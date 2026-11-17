using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Threading;

namespace Guades_QCUCram.Helpers
{
    using S = Guades_QCUCram_Status;

    // The automatic rules (guide section 2.4). Safe to call from several places at once.
    public static class Guades_QCUCram_MaintenanceService
    {
        // ---------------- result object ----------------
        public class SweepResult
        {
            public bool Skipped { get; set; }
            public int Expired { get; set; }
            public int Forfeited { get; set; }
            public int NewlyRestricted { get; set; }
            public int Completed { get; set; }
            public int Lifted { get; set; }
            public int Errors { get; set; }
            public DateTime RanAt { get; set; }

            public bool ChangedAnything
            {
                get { return Expired + Forfeited + Completed + Lifted > 0; }
            }

            public string Summary
            {
                get
                {
                    if (Skipped) return "Another sweep is already running, so this one was skipped.";

                    string s = "Sweep finished at " +
                               RanAt.ToString("h:mm:ss tt", CultureInfo.InvariantCulture) + ": " +
                               Expired + " pending request(s) expired, " +
                               Forfeited + " reservation(s) forfeited" +
                               (NewlyRestricted > 0 ? " (" + NewlyRestricted + " student(s) newly restricted)" : "") + ", " +
                               Completed + " completed, " +
                               Lifted + " restriction(s) lifted.";
                    if (Errors > 0) s += " " + Errors + " error(s) occurred - see the trace log.";
                    return s;
                }
            }
        }

        // Only one sweep at a time inside this web app
        private static readonly object Gate = new object();
        private static long _lastRunTicks;

        // Real (not simulated) time of the last finished sweep; null if none yet
        public static DateTime? LastRun
        {
            get
            {
                long t = Interlocked.Read(ref _lastRunTicks);
                return t == 0 ? (DateTime?)null : new DateTime(t);
            }
        }

        // Slot start/end as a full date-time inside SQL
        private const string StartExpr = "(CAST(r.ReservationDate AS DATETIME) + CAST(ts.StartTime AS DATETIME))";
        private const string EndExpr = "(CAST(r.ReservationDate AS DATETIME) + CAST(ts.EndTime AS DATETIME))";

        // ---------------- the sweep ----------------
        public static SweepResult RunSweep()
        {
            var result = new SweepResult();

            if (!Monitor.TryEnter(Gate))
            {
                result.Skipped = true;
                return result;
            }

            try
            {
                DateTime now = Guades_QCUCram_Clock.Now;
                result.RanAt = now;

                int grace = Guades_QCUCram_SettingsHelper.GetInt("GraceMinutes", 15);
                int threshold = Math.Max(1, Guades_QCUCram_SettingsHelper.GetInt("NoShowThreshold", 3));
                int restrictDays = Guades_QCUCram_SettingsHelper.GetInt("RestrictionDays", 7);

                // Order matters (guide 2.4)
                ExpirePending(now, result);
                ForfeitUnclaimed(now, grace, threshold, restrictDays, result);
                CompleteFinished(now, result);
                LiftExpiredRestrictions(now, result);

                Interlocked.Exchange(ref _lastRunTicks, DateTime.Now.Ticks);
            }
            finally
            {
                Monitor.Exit(Gate);
            }
            return result;
        }

        // ---------------- rule 1: expire pending requests ----------------
        private static void ExpirePending(DateTime now, SweepResult result)
        {
            List<int> ids = FindIds(
                @"SELECT r.ReservationID
                    FROM Reservations r
                    JOIN TimeSlots ts ON ts.TimeSlotID = r.TimeSlotID
                   WHERE r.Status = 'Pending' AND " + StartExpr + @" <= @now
                   ORDER BY r.ReservationID",
                Guades_QCUCram_Db.P("@now", now));

            foreach (int id in ids)
            {
                try
                {
                    Guades_QCUCram_ReservationService.ChangeStatus(
                        id, S.Declined,
                        Guades_QCUCram_ReservationService.ActorSystem, null,
                        "Expired - not reviewed before the slot start time.");
                    result.Expired++;
                }
                catch (InvalidOperationException)
                {
                    // Someone (an admin or the student) changed it first - nothing to do
                }
                catch (Exception ex)
                {
                    result.Errors++;
                    System.Diagnostics.Trace.TraceError("Sweep/expire #" + id + ": " + ex);
                }
            }
        }

        // ---------------- rule 2: forfeit unclaimed bookings ----------------
        private static void ForfeitUnclaimed(DateTime now, int grace, int threshold, int restrictDays, SweepResult result)
        {
            // Strictly AFTER start + grace, matching the check-in window (which allows up to start + grace)
            List<int> ids = FindIds(
                @"SELECT r.ReservationID
                    FROM Reservations r
                    JOIN TimeSlots ts ON ts.TimeSlotID = r.TimeSlotID
                   WHERE r.Status IN ('Approved','Reassigned')
                     AND r.CheckedInAt IS NULL
                     AND DATEADD(MINUTE, @grace, " + StartExpr + @") < @now
                   ORDER BY r.ReservationID",
                Guades_QCUCram_Db.P("@grace", grace),
                Guades_QCUCram_Db.P("@now", now));

            foreach (int id in ids)
            {
                try
                {
                    int outcome = ForfeitOne(id, now, threshold, restrictDays);
                    if (outcome >= 1) result.Forfeited++;
                    if (outcome == 2) result.NewlyRestricted++;
                }
                catch (Exception ex)
                {
                    result.Errors++;
                    System.Diagnostics.Trace.TraceError("Sweep/forfeit #" + id + ": " + ex);
                }
            }
        }

        // Returns 0 = skipped (already handled), 1 = forfeited, 2 = forfeited AND student newly restricted
        private static int ForfeitOne(int id, DateTime now, int threshold, int restrictDays)
        {
            using (var con = Guades_QCUCram_Db.GetConnection())
            {
                con.Open();
                using (var tx = con.BeginTransaction())
                {
                    try
                    {
                        // Lock the reservation and make sure it is still unclaimed
                        object sidObj = ScalarTx(con, tx,
                            @"SELECT StudentID FROM Reservations WITH (UPDLOCK, ROWLOCK)
                               WHERE ReservationID = @id AND Status IN ('Approved','Reassigned') AND CheckedInAt IS NULL",
                            Guades_QCUCram_Db.P("@id", id));
                        if (sidObj == null)
                        {
                            Rollback(tx);
                            return 0;
                        }
                        int studentId = Convert.ToInt32(sidObj);

                        // Add the strike
                        ExecTx(con, tx, "UPDATE Students SET NoShowCount = NoShowCount + 1 WHERE StudentID = @s",
                               Guades_QCUCram_Db.P("@s", studentId));

                        DataTable st = QueryTx(con, tx,
                            "SELECT NoShowCount, IsRestricted FROM Students WHERE StudentID = @s",
                            Guades_QCUCram_Db.P("@s", studentId));
                        int strikes = Convert.ToInt32(st.Rows[0]["NoShowCount"]);
                        bool wasRestricted = Convert.ToBoolean(st.Rows[0]["IsRestricted"]);

                        // Threshold reached -> restrict
                        bool restrictNow = !wasRestricted && strikes >= threshold;
                        DateTime lift = now.AddDays(restrictDays);
                        if (restrictNow)
                        {
                            ExecTx(con, tx,
                                "UPDATE Students SET IsRestricted = 1, RestrictionLiftDate = @lift WHERE StudentID = @s",
                                Guades_QCUCram_Db.P("@lift", lift),
                                Guades_QCUCram_Db.P("@s", studentId));
                        }

                        string extra = strikes <= threshold
                            ? "Strike " + strikes + " of " + threshold + "."
                            : "Total strikes: " + strikes + ".";
                        if (restrictNow)
                            extra += " You are now restricted from booking until " +
                                     lift.ToString("MMM d, yyyy h:mm tt", CultureInfo.InvariantCulture) + ".";

                        // Status change + history + notification (also verifies the transition is allowed)
                        Guades_QCUCram_ReservationService.ChangeStatus(
                            con, tx, id, S.Forfeited,
                            Guades_QCUCram_ReservationService.ActorSystem, null,
                            "Not checked in within the grace period", extra);

                        tx.Commit();
                        return restrictNow ? 2 : 1;
                    }
                    catch (InvalidOperationException)
                    {
                        Rollback(tx);   // state changed under us: undo the strike too
                        return 0;
                    }
                    catch
                    {
                        Rollback(tx);
                        throw;
                    }
                }
            }
        }

        // ---------------- rule 3: complete finished sessions ----------------
        private static void CompleteFinished(DateTime now, SweepResult result)
        {
            List<int> ids = FindIds(
                @"SELECT r.ReservationID
                    FROM Reservations r
                    JOIN TimeSlots ts ON ts.TimeSlotID = r.TimeSlotID
                   WHERE r.Status = 'CheckedIn' AND " + EndExpr + @" <= @now
                   ORDER BY r.ReservationID",
                Guades_QCUCram_Db.P("@now", now));

            foreach (int id in ids)
            {
                try
                {
                    Guades_QCUCram_ReservationService.ChangeStatus(
                        id, S.Completed,
                        Guades_QCUCram_ReservationService.ActorSystem, null,
                        "Slot ended");
                    result.Completed++;
                }
                catch (InvalidOperationException)
                {
                    // already handled elsewhere
                }
                catch (Exception ex)
                {
                    result.Errors++;
                    System.Diagnostics.Trace.TraceError("Sweep/complete #" + id + ": " + ex);
                }
            }
        }

        // ---------------- rule 4: lift expired restrictions ----------------
        private static void LiftExpiredRestrictions(DateTime now, SweepResult result)
        {
            List<int> ids = FindIds(
                @"SELECT StudentID FROM Students
                   WHERE IsRestricted = 1 AND RestrictionLiftDate IS NOT NULL AND RestrictionLiftDate <= @now",
                Guades_QCUCram_Db.P("@now", now));

            foreach (int sid in ids)
            {
                try
                {
                    if (LiftOne(sid, now)) result.Lifted++;
                }
                catch (Exception ex)
                {
                    result.Errors++;
                    System.Diagnostics.Trace.TraceError("Sweep/lift student " + sid + ": " + ex);
                }
            }
        }

        private static bool LiftOne(int studentId, DateTime now)
        {
            using (var con = Guades_QCUCram_Db.GetConnection())
            {
                con.Open();
                using (var tx = con.BeginTransaction())
                {
                    try
                    {
                        int rows = ExecTx(con, tx,
                            @"UPDATE Students
                                 SET IsRestricted = 0, NoShowCount = 0, RestrictionLiftDate = NULL
                               WHERE StudentID = @s AND IsRestricted = 1
                                 AND RestrictionLiftDate IS NOT NULL AND RestrictionLiftDate <= @now",
                            Guades_QCUCram_Db.P("@s", studentId),
                            Guades_QCUCram_Db.P("@now", now));

                        if (rows != 1)
                        {
                            Rollback(tx);
                            return false;
                        }

                        ExecTx(con, tx,
                            @"INSERT INTO Notifications (StudentID, ReservationID, Message, IsRead, CreatedAt)
                              VALUES (@s, NULL, @msg, 0, @now)",
                            Guades_QCUCram_Db.P("@s", studentId),
                            Guades_QCUCram_Db.P("@msg", "Your booking restriction has ended and your strike count was reset. You can reserve rooms again."),
                            Guades_QCUCram_Db.P("@now", now));

                        tx.Commit();
                        return true;
                    }
                    catch
                    {
                        Rollback(tx);
                        throw;
                    }
                }
            }
        }

        // =====================================================================
        //  STEP 25: automatic triggers
        // =====================================================================
        private static System.Threading.Timer _timer;
        private static readonly object TimerLock = new object();

        // Runs the sweep only if the last one finished more than minSeconds ago (real time).
        // Returns null when it was not due yet.
        public static SweepResult RunIfDue(int minSeconds)
        {
            DateTime? last = LastRun;
            if (last.HasValue && (DateTime.Now - last.Value).TotalSeconds < minSeconds)
                return null;
            return RunSweep();
        }

        // Same, but NEVER throws: a sweep problem must not break the page the user asked for.
        public static void TryRunIfDue(int minSeconds)
        {
            try
            {
                RunIfDue(minSeconds);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("Automatic sweep failed: " + ex);
            }
        }

        // Background timer: first run after 5 seconds, then every 'everySeconds'.
        public static void StartBackgroundTimer(int everySeconds)
        {
            lock (TimerLock)
            {
                if (_timer != null) return;   // already running

                int period = Math.Max(10, everySeconds) * 1000;
                int minGap = Math.Max(5, everySeconds / 2);

                _timer = new System.Threading.Timer(
                    state => TryRunIfDue(minGap), null, 5000, period);
            }
        }

        public static void StopBackgroundTimer()
        {
            lock (TimerLock)
            {
                if (_timer == null) return;
                _timer.Dispose();
                _timer = null;
            }
        }

        // ---------------- small private helpers ----------------
        private static List<int> FindIds(string sql, params SqlParameter[] parameters)
        {
            var list = new List<int>();
            DataTable dt = Guades_QCUCram_Db.Query(sql, parameters);
            foreach (DataRow r in dt.Rows) list.Add(Convert.ToInt32(r[0]));
            return list;
        }

        private static int ExecTx(SqlConnection con, SqlTransaction tx, string sql, params SqlParameter[] parameters)
        {
            using (var cmd = new SqlCommand(sql, con, tx))
            {
                if (parameters != null) cmd.Parameters.AddRange(parameters);
                return cmd.ExecuteNonQuery();
            }
        }

        private static object ScalarTx(SqlConnection con, SqlTransaction tx, string sql, params SqlParameter[] parameters)
        {
            using (var cmd = new SqlCommand(sql, con, tx))
            {
                if (parameters != null) cmd.Parameters.AddRange(parameters);
                return cmd.ExecuteScalar();
            }
        }

        private static DataTable QueryTx(SqlConnection con, SqlTransaction tx, string sql, params SqlParameter[] parameters)
        {
            using (var cmd = new SqlCommand(sql, con, tx))
            using (var da = new SqlDataAdapter(cmd))
            {
                if (parameters != null) cmd.Parameters.AddRange(parameters);
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        private static void Rollback(SqlTransaction tx)
        {
            try { tx.Rollback(); } catch { /* already finished */ }
        }
    }
}