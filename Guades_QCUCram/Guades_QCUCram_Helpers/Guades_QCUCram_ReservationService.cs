using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;

namespace Guades_QCUCram.Helpers
{
    using S = Guades_QCUCram_Status;

    // The ONE place where reservation statuses change.
    // Every change = update Reservations + insert history row + insert notification.
    public static class Guades_QCUCram_ReservationService
    {
        // Who made the change (stored in ReservationStatusHistory.ChangedByRole)
        public const string ActorAdmin = "Admin";
        public const string ActorStudent = "Student";
        public const string ActorSystem = "System";

        // ---------------- Allowed transitions (guide section 2.3) ----------------
        private static readonly Dictionary<string, string[]> Allowed = new Dictionary<string, string[]>
        {
            { S.Pending,    new[] { S.Approved, S.Declined, S.Reassigned, S.Cancelled } },
            { S.Approved,   new[] { S.CheckedIn, S.Forfeited, S.Cancelled, S.Reassigned } },
            { S.Reassigned, new[] { S.CheckedIn, S.Forfeited, S.Cancelled, S.Reassigned } },
            { S.CheckedIn,  new[] { S.Completed } }
            // Declined, Cancelled, Forfeited, Completed are terminal (no entry)
        };

        public static bool CanTransition(string from, string to)
        {
            string[] targets;
            if (from == null || to == null) return false;
            if (!Allowed.TryGetValue(from, out targets)) return false;
            return Array.IndexOf(targets, to) >= 0;
        }

        // ---------------- Reservation details (room, date, time) ----------------
        public class ReservationInfo
        {
            public int ReservationId { get; set; }
            public int StudentId { get; set; }
            public string Status { get; set; }
            public int RoomId { get; set; }
            public int TimeSlotId { get; set; }
            public string RoomName { get; set; }
            public DateTime Date { get; set; }
            public TimeSpan StartTime { get; set; }
            public TimeSpan EndTime { get; set; }

            // Exact start/end moment of the booked slot (used later by check-in and the sweep)
            public DateTime SlotStart { get { return Date.Date.Add(StartTime); } }
            public DateTime SlotEnd { get { return Date.Date.Add(EndTime); } }

            public string SlotLabel { get { return FormatSlot(StartTime, EndTime); } }

            public string Summary
            {
                get
                {
                    return RoomName + " on " +
                           Date.ToString("MMM d, yyyy", CultureInfo.InvariantCulture) +
                           " (" + SlotLabel + ")";
                }
            }
        }

        // "9:00 AM - 10:00 AM"
        public static string FormatSlot(TimeSpan start, TimeSpan end)
        {
            DateTime d = DateTime.Today;
            return d.Add(start).ToString("h:mm tt", CultureInfo.InvariantCulture) + " - " +
                   d.Add(end).ToString("h:mm tt", CultureInfo.InvariantCulture);
        }

        public static string GetSlotLabel(int timeSlotId)
        {
            var dt = Guades_QCUCram_Db.Query(
                "SELECT StartTime, EndTime FROM TimeSlots WHERE TimeSlotID = @id",
                Guades_QCUCram_Db.P("@id", timeSlotId));
            if (dt.Rows.Count == 0) return "";
            return FormatSlot((TimeSpan)dt.Rows[0]["StartTime"], (TimeSpan)dt.Rows[0]["EndTime"]);
        }

        // Returns null if the reservation does not exist
        public static ReservationInfo GetInfo(int reservationId)
        {
            using (var con = Guades_QCUCram_Db.GetConnection())
            {
                con.Open();
                return ReadInfo(con, null, reservationId, false);
            }
        }

        // e.g. "Collab Room 3 on Oct 7, 2026 (9:00 AM - 10:00 AM)"
        public static string GetReservationSummary(int reservationId)
        {
            ReservationInfo info = GetInfo(reservationId);
            return info == null ? "" : info.Summary;
        }

        // ---------------- ChangeStatus (use inside YOUR transaction) ----------------
        // Throws InvalidOperationException (with a user-friendly message) when the change
        // is not allowed, so the caller's transaction can be rolled back.
        public static void ChangeStatus(SqlConnection con, SqlTransaction tx, int reservationId,
                                        string newStatus, string actorRole, int? actorId,
                                        string notes = null, string extraMessage = null)
        {
            if (actorRole != ActorAdmin && actorRole != ActorStudent && actorRole != ActorSystem)
                throw new ArgumentException("Unknown actor role.", "actorRole");
            if (actorRole != ActorSystem && !actorId.HasValue)
                throw new ArgumentException("actorId is required for Admin and Student changes.", "actorId");

            // Read + lock the row so two people can't change it at the same moment
            ReservationInfo info = ReadInfo(con, tx, reservationId, true);
            if (info == null)
                throw new InvalidOperationException("Reservation #" + reservationId + " was not found.");

            string oldStatus = info.Status;
            if (!CanTransition(oldStatus, newStatus))
                throw new InvalidOperationException(
                    "Reservation #" + reservationId + " is " + oldStatus +
                    " and can't be changed to " + newStatus + ".");

            // 1) Update the reservation
            DateTime now = Guades_QCUCram_Clock.Now;
            int rows;
            if (newStatus == S.CheckedIn)
            {
                rows = Exec(con, tx,
                    "UPDATE Reservations SET Status = @s, CheckedInAt = @now WHERE ReservationID = @id AND Status = @old",
                    Guades_QCUCram_Db.P("@s", newStatus),
                    Guades_QCUCram_Db.P("@now", now),
                    Guades_QCUCram_Db.P("@id", reservationId),
                    Guades_QCUCram_Db.P("@old", oldStatus));
            }
            else
            {
                rows = Exec(con, tx,
                    "UPDATE Reservations SET Status = @s WHERE ReservationID = @id AND Status = @old",
                    Guades_QCUCram_Db.P("@s", newStatus),
                    Guades_QCUCram_Db.P("@id", reservationId),
                    Guades_QCUCram_Db.P("@old", oldStatus));
            }
            if (rows != 1)
                throw new InvalidOperationException(
                    "Reservation #" + reservationId + " was just changed by someone else. Please refresh and try again.");

            // Reflect the new status in the info used for the message
            info.Status = newStatus;

            // 2) History row (audit trail)
            InsertHistory(con, tx, reservationId, oldStatus, newStatus, actorRole, actorId, notes);

            // 3) Notification for the requester
            string message = BuildMessage(newStatus, info, actorRole, notes, extraMessage);
            InsertNotification(con, tx, info.StudentId, reservationId, message);
        }

        // Same thing, but opens its own connection + transaction (handy for one-off changes)
        public static void ChangeStatus(int reservationId, string newStatus, string actorRole,
                                        int? actorId, string notes = null, string extraMessage = null)
        {
            using (var con = Guades_QCUCram_Db.GetConnection())
            {
                con.Open();
                using (var tx = con.BeginTransaction())
                {
                    try
                    {
                        ChangeStatus(con, tx, reservationId, newStatus, actorRole, actorId, notes, extraMessage);
                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }

        // ---------------- First history row + "received" notification (Step 18 calls this) ----------------
        // Call right after inserting a new Pending reservation, inside the same transaction.
        public static void RecordCreation(SqlConnection con, SqlTransaction tx, int reservationId)
        {
            ReservationInfo info = ReadInfo(con, tx, reservationId, false);
            if (info == null)
                throw new InvalidOperationException("Reservation #" + reservationId + " was not found.");

            InsertHistory(con, tx, reservationId, null, S.Pending, ActorStudent, info.StudentId, "Request submitted");

            string message = "Your request #" + reservationId + " (" + info.Summary +
                             ") was received and is awaiting review by the library assistant.";
            InsertNotification(con, tx, info.StudentId, reservationId, message);
        }

        // ---------------- Notification text per status ----------------
        private static string BuildMessage(string newStatus, ReservationInfo info, string actorRole,
                                           string notes, string extra)
        {
            string head = "Reservation #" + info.ReservationId + " (" + info.Summary + ")";
            string note = string.IsNullOrWhiteSpace(notes) ? "" : notes.Trim();
            string msg;

            switch (newStatus)
            {
                case S.Approved:
                    msg = head + " was approved. Bring the physical QCID of all members and arrive on time; " +
                          "unclaimed rooms are forfeited after " + GraceMinutes() + " minutes.";
                    break;

                case S.Reassigned:
                    msg = head + " was reassigned to this new room/time." +
                          (note == "" ? "" : " Details: " + note);
                    break;

                case S.Declined:
                    msg = head + " was declined." + (note == "" ? "" : " Reason: " + note);
                    break;

                case S.Cancelled:
                    msg = (actorRole == ActorAdmin)
                        ? head + " was cancelled by the library." + (note == "" ? "" : " Reason: " + note)
                        : "You cancelled " + head + ".";
                    break;

                case S.CheckedIn:
                    msg = "Check-in confirmed for " + head + ". Please keep the room tidy, no eating or drinking.";
                    break;

                case S.Completed:
                    msg = head + " is completed. Thank you for using the collaboration rooms.";
                    break;

                case S.Forfeited:
                    msg = head + " was forfeited because it was not claimed within " +
                          GraceMinutes() + " minutes of the start time.";
                    break;

                default:
                    msg = head + " is now " + newStatus + ".";
                    break;
            }

            if (!string.IsNullOrWhiteSpace(extra)) msg += " " + extra.Trim();

            // Notifications.Message is NVARCHAR(400)
            if (msg.Length > 400) msg = msg.Substring(0, 397) + "...";
            return msg;
        }

        private static int GraceMinutes()
        {
            return Guades_QCUCram_SettingsHelper.GetInt("GraceMinutes", 15);
        }

        // =====================================================================
        //  STEP 18: validate + create a reservation request
        // =====================================================================

        private const string FreeList = "('Declined','Cancelled','Forfeited')";

        public class MemberInput
        {
            public string LastName { get; set; }
            public string FirstName { get; set; }
            public string MI { get; set; }
        }

        public class CreateRequest
        {
            public int StudentId { get; set; }
            public DateTime Date { get; set; }      // DateTime.MinValue = missing/invalid
            public int RoomId { get; set; }
            public int TimeSlotId { get; set; }
            public string Purpose { get; set; }
            public int NumberOfMembers { get; set; }
            public List<MemberInput> Members { get; set; }
        }

        public class CreateResult
        {
            public List<string> Errors { get; private set; }
            public int ReservationId { get; set; }
            public bool Success { get { return Errors.Count == 0 && ReservationId > 0; } }
            public CreateResult() { Errors = new List<string>(); }
        }

        // Checks ALL the rules (guide section 2.5), collects every error,
        // and only saves when there are none.
        public static CreateResult ValidateAndCreate(CreateRequest req)
        {
            var result = new CreateResult();
            List<string> errors = result.Errors;
            DateTime now = Guades_QCUCram_Clock.Now;
            CultureInfo inv = CultureInfo.InvariantCulture;

            // ---- Rule 1: requester exists and is not restricted ----
            var st = Guades_QCUCram_Db.Query(
                "SELECT IsRestricted, RestrictionLiftDate FROM Students WHERE StudentID = @id",
                Guades_QCUCram_Db.P("@id", req.StudentId));
            if (st.Rows.Count == 0)
            {
                errors.Add("Your account could not be found. Please log in again.");
                return result;
            }
            if (Convert.ToBoolean(st.Rows[0]["IsRestricted"]))
            {
                object lift = st.Rows[0]["RestrictionLiftDate"];
                bool stillRestricted = lift == DBNull.Value || Convert.ToDateTime(lift) > now;
                if (stillRestricted)
                {
                    string until = lift == DBNull.Value
                        ? "further notice"
                        : Convert.ToDateTime(lift).ToString("MMM d, yyyy h:mm tt", inv);
                    errors.Add("You are restricted from booking until " + until + ".");
                    return result;
                }
            }

            // ---- Rule 2: date must be exactly today + AdvanceBookingDays ----
            bool dateOk = req.Date != DateTime.MinValue;
            DateTime date = req.Date.Date;
            if (!dateOk)
            {
                errors.Add("Please choose a valid date.");
            }
            else
            {
                int ahead = Guades_QCUCram_SettingsHelper.GetInt("AdvanceBookingDays", 1);
                DateTime required = Guades_QCUCram_Clock.Today.AddDays(ahead);
                if (date != required)
                {
                    string aheadText = ahead == 0 ? "for today only"
                                     : ahead == 1 ? "exactly one day in advance"
                                     : "exactly " + ahead + " days in advance";
                    errors.Add("Reservations must be made " + aheadText + ". Please choose " +
                               required.ToString("dddd, MMM d, yyyy", inv) + ".");
                }
            }

            // ---- Rule 3: room and time slot exist and are active ----
            string roomName = null;
            int minCap = 5, maxCap = 10;
            if (req.RoomId <= 0)
            {
                errors.Add("Please select a room.");
            }
            else
            {
                var rm = Guades_QCUCram_Db.Query(
                    "SELECT RoomName, MinCapacity, MaxCapacity, IsActive FROM Rooms WHERE RoomID = @id",
                    Guades_QCUCram_Db.P("@id", req.RoomId));
                if (rm.Rows.Count == 0 || !Convert.ToBoolean(rm.Rows[0]["IsActive"]))
                {
                    errors.Add("The selected room is not available for booking.");
                }
                else
                {
                    roomName = Convert.ToString(rm.Rows[0]["RoomName"]);
                    minCap = Convert.ToInt32(rm.Rows[0]["MinCapacity"]);
                    maxCap = Convert.ToInt32(rm.Rows[0]["MaxCapacity"]);
                }
            }

            bool slotOk = false;
            TimeSpan slotStart = TimeSpan.Zero;
            if (req.TimeSlotId <= 0)
            {
                errors.Add("Please select a time slot.");
            }
            else
            {
                var ts = Guades_QCUCram_Db.Query(
                    "SELECT StartTime, IsActive FROM TimeSlots WHERE TimeSlotID = @id",
                    Guades_QCUCram_Db.P("@id", req.TimeSlotId));
                if (ts.Rows.Count == 0 || !Convert.ToBoolean(ts.Rows[0]["IsActive"]))
                {
                    errors.Add("The selected time slot is not available for booking.");
                }
                else
                {
                    slotOk = true;
                    slotStart = (TimeSpan)ts.Rows[0]["StartTime"];
                }
            }
            if (dateOk && slotOk && date.Add(slotStart) <= now)
                errors.Add("That time slot has already started. Please choose a later slot.");

            // ---- Rule 4: group size, member rows, room capacity ----
            bool sizeOk = req.NumberOfMembers >= 5 && req.NumberOfMembers <= 10;
            if (!sizeOk)
            {
                errors.Add("Group size must be between 5 and 10 members.");
            }
            else if (roomName != null && (req.NumberOfMembers < minCap || req.NumberOfMembers > maxCap))
            {
                errors.Add(roomName + " accommodates " + minCap + " to " + maxCap + " persons only.");
            }

            List<MemberInput> members = req.Members ?? new List<MemberInput>();
            if (members.Count != req.NumberOfMembers)
            {
                errors.Add("Please fill in exactly " + req.NumberOfMembers + " member rows.");
            }
            else
            {
                for (int i = 0; i < members.Count; i++)
                {
                    MemberInput m = members[i];
                    string last = (m.LastName ?? "").Trim();
                    string first = (m.FirstName ?? "").Trim();
                    string mi = (m.MI ?? "").Trim();
                    if (last == "" || first == "")
                        errors.Add("Member #" + (i + 1) + ": last name and first name are required.");
                    else if (last.Length > 60 || first.Length > 60 || mi.Length > 5)
                        errors.Add("Member #" + (i + 1) + ": a name is too long.");
                }
            }

            // ---- Rule 7: required fields ----
            string purpose = (req.Purpose ?? "").Trim();
            if (purpose == "") errors.Add("Please enter the purpose of your reservation.");
            else if (purpose.Length > 300) errors.Add("Purpose must be 300 characters or fewer.");

            // ---- Rules 5 + 6 (quick, non-locking check so ALL errors show together) ----
            if (dateOk)
            {
                if (roomName != null && slotOk && SlotIsHeld(null, null, req.RoomId, date, req.TimeSlotId, false))
                    errors.Add("That room and time slot is already taken or has a pending request. " +
                               "Please choose another slot (see the availability grid).");

                if (ActiveCountForDay(null, null, req.StudentId, date, false) > 0)
                    errors.Add("You already have an active reservation on " + date.ToString("MMM d, yyyy", inv) +
                               " (maximum 1 hour per day). Cancel it first if you want a different slot.");
            }

            if (errors.Count > 0) return result;

            // ---- All rules passed: save everything in ONE transaction ----
            using (var con = Guades_QCUCram_Db.GetConnection())
            {
                con.Open();
                using (var tx = con.BeginTransaction(System.Data.IsolationLevel.Serializable))
                {
                    try
                    {
                        // Authoritative re-check WITH locks (protects against double-click / two browsers)
                        if (SlotIsHeld(con, tx, req.RoomId, date, req.TimeSlotId, true))
                        {
                            SafeRollback(tx);
                            errors.Add("That room and time slot was just taken by someone else. Please choose another slot.");
                            return result;
                        }
                        if (ActiveCountForDay(con, tx, req.StudentId, date, true) > 0)
                        {
                            SafeRollback(tx);
                            errors.Add("You already have an active reservation on " +
                                       date.ToString("MMM d, yyyy", inv) + " (maximum 1 hour per day).");
                            return result;
                        }

                        object idObj = ScalarTx(con, tx,
                            @"INSERT INTO Reservations
                                  (StudentID, RoomID, TimeSlotID, ReservationDate, Purpose, NumberOfMembers, Status, CreatedAt)
                              OUTPUT INSERTED.ReservationID
                              VALUES (@s, @r, @t, CAST(@d AS DATE), @p, @n, @st, @at)",
                            Guades_QCUCram_Db.P("@s", req.StudentId),
                            Guades_QCUCram_Db.P("@r", req.RoomId),
                            Guades_QCUCram_Db.P("@t", req.TimeSlotId),
                            Guades_QCUCram_Db.P("@d", date),
                            Guades_QCUCram_Db.P("@p", purpose),
                            Guades_QCUCram_Db.P("@n", req.NumberOfMembers),
                            Guades_QCUCram_Db.P("@st", Guades_QCUCram_Status.Pending),
                            Guades_QCUCram_Db.P("@at", now));
                        int id = Convert.ToInt32(idObj);

                        foreach (MemberInput m in members)
                        {
                            Exec(con, tx,
                                @"INSERT INTO ReservationMembers (ReservationID, LastName, FirstName, MI)
                                  VALUES (@id, @l, @f, @mi)",
                                Guades_QCUCram_Db.P("@id", id),
                                Guades_QCUCram_Db.P("@l", m.LastName.Trim()),
                                Guades_QCUCram_Db.P("@f", m.FirstName.Trim()),
                                Guades_QCUCram_Db.P("@mi", string.IsNullOrWhiteSpace(m.MI) ? null : m.MI.Trim()));
                        }

                        // First history row (NULL -> Pending) + "received" notification
                        RecordCreation(con, tx, id);

                        tx.Commit();
                        result.ReservationId = id;
                    }
                    catch (SqlException ex)
                    {
                        SafeRollback(tx);
                        if (ex.Number == 2601 || ex.Number == 2627)
                            errors.Add("That slot was just taken - please choose another.");
                        else if (ex.Number == 1205)
                            errors.Add("The system was busy. Please press Submit again.");
                        else
                            throw;
                    }
                    catch
                    {
                        SafeRollback(tx);
                        throw;
                    }
                }
            }
            return result;
        }

        // Is the room/date/slot held by an active (not declined/cancelled/forfeited) reservation?
        // con/tx = null -> quick check on its own connection; otherwise runs inside the transaction.
        private static bool SlotIsHeld(SqlConnection con, SqlTransaction tx, int roomId,
                                       DateTime date, int slotId, bool lockRows)
        {
            object n = ScalarAny(con, tx,
                "SELECT COUNT(*) FROM Reservations" + (lockRows ? " WITH (UPDLOCK, HOLDLOCK)" : "") +
                " WHERE RoomID = @r AND ReservationDate = CAST(@d AS DATE) AND TimeSlotID = @t" +
                " AND Status NOT IN " + FreeList,
                Guades_QCUCram_Db.P("@r", roomId),
                Guades_QCUCram_Db.P("@d", date),
                Guades_QCUCram_Db.P("@t", slotId));
            return Convert.ToInt32(n) > 0;
        }

        // How many active reservations does this person already have on that date?
        private static int ActiveCountForDay(SqlConnection con, SqlTransaction tx, int studentId,
                                             DateTime date, bool lockRows)
        {
            object n = ScalarAny(con, tx,
                "SELECT COUNT(*) FROM Reservations" + (lockRows ? " WITH (UPDLOCK, HOLDLOCK)" : "") +
                " WHERE StudentID = @s AND ReservationDate = CAST(@d AS DATE)" +
                " AND Status NOT IN " + FreeList,
                Guades_QCUCram_Db.P("@s", studentId),
                Guades_QCUCram_Db.P("@d", date));
            return Convert.ToInt32(n);
        }

        private static object ScalarAny(SqlConnection con, SqlTransaction tx, string sql, params SqlParameter[] parameters)
        {
            if (con == null) return Guades_QCUCram_Db.Scalar(sql, parameters);
            return ScalarTx(con, tx, sql, parameters);
        }

        private static object ScalarTx(SqlConnection con, SqlTransaction tx, string sql, params SqlParameter[] parameters)
        {
            using (var cmd = new SqlCommand(sql, con, tx))
            {
                if (parameters != null) cmd.Parameters.AddRange(parameters);
                return cmd.ExecuteScalar();
            }
        }

        private static void SafeRollback(SqlTransaction tx)
        {
            try { tx.Rollback(); } catch { /* already finished */ }
        }

        // =====================================================================
        //  STEP 21: reassign (move a booking to another room and/or slot, same date)
        // =====================================================================
        // Throws InvalidOperationException with a friendly message when it can't be done.
        public static void Reassign(int reservationId, int newRoomId, int newSlotId, string reason, int adminId)
        {
            string why = (reason ?? "").Trim();
            if (why == "")
                throw new InvalidOperationException("Please enter a reason for the reassignment.");
            if (why.Length > 200)
                throw new InvalidOperationException("The reason must be 200 characters or fewer.");

            using (var con = Guades_QCUCram_Db.GetConnection())
            {
                con.Open();
                using (var tx = con.BeginTransaction(System.Data.IsolationLevel.Serializable))
                {
                    try
                    {
                        ReservationInfo info = ReadInfo(con, tx, reservationId, true);
                        if (info == null)
                            throw new InvalidOperationException("Reservation #" + reservationId + " was not found.");
                        if (!CanTransition(info.Status, S.Reassigned))
                            throw new InvalidOperationException("Reservation #" + reservationId + " is " +
                                info.Status + " and can't be reassigned.");

                        DateTime now = Guades_QCUCram_Clock.Now;
                        if (info.SlotStart <= now)
                            throw new InvalidOperationException("Reservation #" + reservationId +
                                " has already started and can't be reassigned.");
                        if (newRoomId == info.RoomId && newSlotId == info.TimeSlotId)
                            throw new InvalidOperationException("Please choose a different room or time slot.");

                        // New room: must exist, be active, and fit the group
                        System.Data.DataTable rm = QueryTx(con, tx,
                            "SELECT RoomName, MinCapacity, MaxCapacity, IsActive FROM Rooms WHERE RoomID = @id",
                            Guades_QCUCram_Db.P("@id", newRoomId));
                        if (rm.Rows.Count == 0 || !Convert.ToBoolean(rm.Rows[0]["IsActive"]))
                            throw new InvalidOperationException("The selected room is not available.");
                        string newRoomName = Convert.ToString(rm.Rows[0]["RoomName"]);

                        int members = Convert.ToInt32(ScalarTx(con, tx,
                            "SELECT NumberOfMembers FROM Reservations WHERE ReservationID = @id",
                            Guades_QCUCram_Db.P("@id", reservationId)));
                        int minCap = Convert.ToInt32(rm.Rows[0]["MinCapacity"]);
                        int maxCap = Convert.ToInt32(rm.Rows[0]["MaxCapacity"]);
                        if (members < minCap || members > maxCap)
                            throw new InvalidOperationException(newRoomName + " accommodates " + minCap + " to " +
                                maxCap + " persons, but this group has " + members + ".");

                        // New slot: must exist, be active, and still be in the future
                        System.Data.DataTable ts = QueryTx(con, tx,
                            "SELECT StartTime, EndTime, IsActive FROM TimeSlots WHERE TimeSlotID = @id",
                            Guades_QCUCram_Db.P("@id", newSlotId));
                        if (ts.Rows.Count == 0 || !Convert.ToBoolean(ts.Rows[0]["IsActive"]))
                            throw new InvalidOperationException("The selected time slot is not available.");
                        TimeSpan newStart = (TimeSpan)ts.Rows[0]["StartTime"];
                        TimeSpan newEnd = (TimeSpan)ts.Rows[0]["EndTime"];
                        if (info.Date.Date.Add(newStart) <= now)
                            throw new InvalidOperationException("That time slot has already started.");

                        // Is the target free? (locks the range so nobody can grab it meanwhile)
                        if (SlotIsHeld(con, tx, newRoomId, info.Date.Date, newSlotId, true))
                            throw new InvalidOperationException(
                                "That room and time slot is already taken or has a pending request.");

                        string note = "Moved from " + info.RoomName + ", " + info.SlotLabel +
                                      " to " + newRoomName + ", " + FormatSlot(newStart, newEnd) +
                                      ". Reason: " + why;

                        Exec(con, tx,
                            "UPDATE Reservations SET RoomID = @r, TimeSlotID = @t WHERE ReservationID = @id",
                            Guades_QCUCram_Db.P("@r", newRoomId),
                            Guades_QCUCram_Db.P("@t", newSlotId),
                            Guades_QCUCram_Db.P("@id", reservationId));

                        // Status -> Reassigned (+ history row + notification showing the NEW room/time)
                        ChangeStatus(con, tx, reservationId, S.Reassigned, ActorAdmin, adminId, note);

                        tx.Commit();
                    }
                    catch (SqlException ex)
                    {
                        SafeRollback(tx);
                        if (ex.Number == 2601 || ex.Number == 2627)
                            throw new InvalidOperationException("That room and time slot was just taken by someone else.");
                        if (ex.Number == 1205)
                            throw new InvalidOperationException("The system was busy. Please try again.");
                        throw;
                    }
                    catch
                    {
                        SafeRollback(tx);
                        throw;
                    }
                }
            }
        }

        // SELECT inside a transaction -> DataTable
        private static System.Data.DataTable QueryTx(SqlConnection con, SqlTransaction tx, string sql,
                                                     params SqlParameter[] parameters)
        {
            using (var cmd = new SqlCommand(sql, con, tx))
            using (var da = new SqlDataAdapter(cmd))
            {
                if (parameters != null) cmd.Parameters.AddRange(parameters);
                var dt = new System.Data.DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        // ---------------- Private data helpers ----------------
        private static ReservationInfo ReadInfo(SqlConnection con, SqlTransaction tx, int reservationId, bool lockRow)
        {
            string sql =
                @"SELECT r.ReservationID, r.StudentID, r.Status, r.RoomID, r.TimeSlotID, r.ReservationDate,
                         rm.RoomName, ts.StartTime, ts.EndTime
                    FROM Reservations r " + (lockRow ? "WITH (UPDLOCK, ROWLOCK) " : "") + @"
                    JOIN Rooms rm     ON rm.RoomID = r.RoomID
                    JOIN TimeSlots ts ON ts.TimeSlotID = r.TimeSlotID
                   WHERE r.ReservationID = @id";

            using (var cmd = new SqlCommand(sql, con, tx))
            {
                cmd.Parameters.Add(Guades_QCUCram_Db.P("@id", reservationId));
                using (var rd = cmd.ExecuteReader())
                {
                    if (!rd.Read()) return null;

                    return new ReservationInfo
                    {
                        ReservationId = Convert.ToInt32(rd["ReservationID"]),
                        StudentId = Convert.ToInt32(rd["StudentID"]),
                        Status = Convert.ToString(rd["Status"]),
                        RoomId = Convert.ToInt32(rd["RoomID"]),
                        TimeSlotId = Convert.ToInt32(rd["TimeSlotID"]),
                        RoomName = Convert.ToString(rd["RoomName"]),
                        Date = Convert.ToDateTime(rd["ReservationDate"]),
                        StartTime = (TimeSpan)rd["StartTime"],
                        EndTime = (TimeSpan)rd["EndTime"]
                    };
                }
            }
        }

        private static void InsertHistory(SqlConnection con, SqlTransaction tx, int reservationId,
                                          string oldStatus, string newStatus, string actorRole,
                                          int? actorId, string notes)
        {
            if (notes != null && notes.Length > 300) notes = notes.Substring(0, 300);

            Exec(con, tx,
                @"INSERT INTO ReservationStatusHistory
                      (ReservationID, OldStatus, NewStatus, ChangedByRole, ChangedBy, ChangedAt, Notes)
                  VALUES (@rid, @old, @new, @role, @by, @at, @notes)",
                Guades_QCUCram_Db.P("@rid", reservationId),
                Guades_QCUCram_Db.P("@old", oldStatus),
                Guades_QCUCram_Db.P("@new", newStatus),
                Guades_QCUCram_Db.P("@role", actorRole),
                Guades_QCUCram_Db.P("@by", actorId),
                Guades_QCUCram_Db.P("@at", Guades_QCUCram_Clock.Now),
                Guades_QCUCram_Db.P("@notes", string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()));
        }

        private static void InsertNotification(SqlConnection con, SqlTransaction tx, int studentId,
                                               int reservationId, string message)
        {
            Exec(con, tx,
                @"INSERT INTO Notifications (StudentID, ReservationID, Message, IsRead, CreatedAt)
                  VALUES (@sid, @rid, @msg, 0, @at)",
                Guades_QCUCram_Db.P("@sid", studentId),
                Guades_QCUCram_Db.P("@rid", reservationId),
                Guades_QCUCram_Db.P("@msg", message),
                Guades_QCUCram_Db.P("@at", Guades_QCUCram_Clock.Now));
        }

        private static int Exec(SqlConnection con, SqlTransaction tx, string sql, params SqlParameter[] parameters)
        {
            using (var cmd = new SqlCommand(sql, con, tx))
            {
                if (parameters != null) cmd.Parameters.AddRange(parameters);
                return cmd.ExecuteNonQuery();
            }
        }
    }
}