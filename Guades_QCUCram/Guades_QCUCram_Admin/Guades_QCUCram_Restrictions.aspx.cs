using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Web;
using System.Web.UI.WebControls;
using Guades_QCUCram.Helpers;

namespace Guades_QCUCram.Guades_QCUCram_Admin
{
    public partial class Guades_QCUCram_Restrictions : Guades_QCUCram_AdminPage
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        // Lift pop-up: true while the modal should show (plain field, reset each request)
        private bool _liftOpen;
        protected string LiftModalCss { get { return _liftOpen ? " is-open" : ""; } }

        // Student whose "Lift" panel is open (survives postbacks)
        private int LiftId
        {
            get { object v = ViewState["LiftId"]; return v == null ? 0 : (int)v; }
            set { ViewState["LiftId"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack) BindAll();
        }

        // ------------------------------------------------------------------ binding
        private void BindAll()
        {
            int threshold = Math.Max(1, Guades_QCUCram_SettingsHelper.GetInt("NoShowThreshold", 3));
            int days = Guades_QCUCram_SettingsHelper.GetInt("RestrictionDays", 7);
            litRule.Text = "Rule: <strong>" + threshold + "</strong> forfeited booking(s) restrict a student from booking for <strong>" +
                           days + "</strong> day(s). Restrictions end automatically; you can also lift one early.";

            BindRestricted();
            BindWarning(threshold);
        }

        private void BindRestricted()
        {
            DataTable dt = Guades_QCUCram_Db.Query(
                @"SELECT StudentID, StudentNumber, LastName, FirstName, MI, RequesterType, Program,
                         NoShowCount, RestrictionLiftDate
                    FROM Students
                   WHERE IsRestricted = 1
                   ORDER BY CASE WHEN RestrictionLiftDate IS NULL THEN 1 ELSE 0 END, RestrictionLiftDate");

            dt.Columns.Add("StudentHtml", typeof(string));
            dt.Columns.Add("UntilText", typeof(string));
            dt.Columns.Add("RemainingText", typeof(string));

            DateTime now = Guades_QCUCram_Clock.Now;
            foreach (DataRow row in dt.Rows)
            {
                row["StudentHtml"] = StudentHtml(row);

                if (row["RestrictionLiftDate"] == DBNull.Value)
                {
                    row["UntilText"] = "Until lifted manually";
                    row["RemainingText"] = "-";
                }
                else
                {
                    DateTime lift = Convert.ToDateTime(row["RestrictionLiftDate"]);
                    row["UntilText"] = lift.ToString("MMM d, yyyy h:mm tt", Inv);
                    row["RemainingText"] = lift <= now ? "Ending at the next sweep" : RemainingText(lift - now);
                }
            }

            gvRestricted.DataSource = dt;
            gvRestricted.DataBind();
        }

        private void BindWarning(int threshold)
        {
            // "One strike away" only makes sense when the threshold is 2 or more
            int oneAway = threshold - 1;
            int target = oneAway >= 1 ? oneAway : -1;   // -1 matches nobody

            DataTable dt = Guades_QCUCram_Db.Query(
                @"SELECT s.StudentID, s.StudentNumber, s.LastName, s.FirstName, s.MI, s.RequesterType, s.Program,
                         s.NoShowCount,
                         (SELECT MAX(r.ReservationDate) FROM Reservations r
                           WHERE r.StudentID = s.StudentID AND r.Status = 'Forfeited') AS LastForfeited
                    FROM Students s
                   WHERE s.IsRestricted = 0 AND s.NoShowCount = @n
                   ORDER BY s.LastName, s.FirstName",
                Guades_QCUCram_Db.P("@n", target));

            dt.Columns.Add("StudentHtml", typeof(string));
            dt.Columns.Add("StrikesText", typeof(string));
            dt.Columns.Add("LastText", typeof(string));

            foreach (DataRow row in dt.Rows)
            {
                row["StudentHtml"] = StudentHtml(row);
                row["StrikesText"] = Convert.ToInt32(row["NoShowCount"]) + " of " + threshold;
                row["LastText"] = row["LastForfeited"] == DBNull.Value
                    ? "-"
                    : Convert.ToDateTime(row["LastForfeited"]).ToString("MMM d, yyyy", Inv);
            }

            gvWarning.DataSource = dt;
            gvWarning.DataBind();
        }

        private static string StudentHtml(DataRow row)
        {
            string mi = Convert.ToString(row["MI"]);
            string name = Convert.ToString(row["LastName"]) + ", " + Convert.ToString(row["FirstName"]) +
                          (string.IsNullOrWhiteSpace(mi) ? "" : " " + mi);
            return "<strong>" + HttpUtility.HtmlEncode(name) + "</strong><br /><small>" +
                   HttpUtility.HtmlEncode(Convert.ToString(row["StudentNumber"]) + " | " +
                                          Convert.ToString(row["RequesterType"]) + " | " +
                                          Convert.ToString(row["Program"])) + "</small>";
        }

        private static string RemainingText(TimeSpan span)
        {
            if (span.TotalHours < 1) return Math.Max(1, (int)Math.Ceiling(span.TotalMinutes)) + " min";
            if (span.TotalDays < 1) return (int)span.TotalHours + " hr " + span.Minutes + " min";
            return (int)span.TotalDays + " day(s) " + span.Hours + " hr";
        }

        // ------------------------------------------------------------------ lift flow
        protected void gvRestricted_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "LiftRes") return;

            int id;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out id)) return;

            DataTable dt = Guades_QCUCram_Db.Query(
                @"SELECT StudentNumber, LastName, FirstName, MI, NoShowCount, IsRestricted, RestrictionLiftDate
                    FROM Students WHERE StudentID = @id",
                Guades_QCUCram_Db.P("@id", id));

            if (dt.Rows.Count == 0 || !Convert.ToBoolean(dt.Rows[0]["IsRestricted"]))
            {
                ShowMessage(lblMessage, "That student is no longer restricted. The list has been refreshed.", true);
                _liftOpen = false;
                LiftId = 0;
                BindAll();
                return;
            }

            DataRow r = dt.Rows[0];
            string mi = Convert.ToString(r["MI"]);
            string name = Convert.ToString(r["LastName"]) + ", " + Convert.ToString(r["FirstName"]) +
                          (string.IsNullOrWhiteSpace(mi) ? "" : " " + mi);
            string until = r["RestrictionLiftDate"] == DBNull.Value
                ? "until lifted manually"
                : "until " + Convert.ToDateTime(r["RestrictionLiftDate"]).ToString("MMM d, yyyy h:mm tt", Inv);

            litLiftInfo.Text = "<p class=\"mb-0\">You are about to lift the restriction of <strong>" +
                               HttpUtility.HtmlEncode(name) + "</strong> (" +
                               HttpUtility.HtmlEncode(Convert.ToString(r["StudentNumber"])) + "), who has " +
                               Convert.ToInt32(r["NoShowCount"]) + " strike(s) and is restricted " +
                               HttpUtility.HtmlEncode(until) + ".</p>";
            txtNote.Text = "";
            LiftId = id;
            _liftOpen = true;
        }

        protected void btnConfirmLift_Click(object sender, EventArgs e)
        {
            int id = LiftId;
            if (id <= 0) return;

            string note = txtNote.Text.Trim();
            if (note.Length > 150) note = note.Substring(0, 150);

            try
            {
                bool lifted = LiftRestriction(id, note);
                ShowMessage(lblMessage,
                    lifted ? "Restriction lifted. The student has been notified and their strikes were reset."
                           : "That student was no longer restricted.",
                    !lifted);
            }
            catch (Exception)
            {
                ShowMessage(lblMessage, "Something went wrong while lifting the restriction. Please try again.", true);
                return;   // panel stays open
            }

            _liftOpen = false;
            LiftId = 0;
            txtNote.Text = "";
            BindAll();
        }

        protected void btnCancelLift_Click(object sender, EventArgs e)
        {
            _liftOpen = false;
            LiftId = 0;
            txtNote.Text = "";
        }

        // Clears the restriction + strikes and notifies the student, in one transaction.
        private bool LiftRestriction(int studentId, string note)
        {
            using (var con = Guades_QCUCram_Db.GetConnection())
            {
                con.Open();
                using (var tx = con.BeginTransaction())
                {
                    try
                    {
                        int rows;
                        using (var cmd = new SqlCommand(
                            @"UPDATE Students
                                 SET IsRestricted = 0, NoShowCount = 0, RestrictionLiftDate = NULL
                               WHERE StudentID = @s AND IsRestricted = 1", con, tx))
                        {
                            cmd.Parameters.Add(Guades_QCUCram_Db.P("@s", studentId));
                            rows = cmd.ExecuteNonQuery();
                        }

                        if (rows != 1)
                        {
                            tx.Rollback();
                            return false;
                        }

                        string who = string.IsNullOrWhiteSpace(CurrentUserName) ? "a library assistant" : CurrentUserName;
                        string msg = "Your booking restriction was lifted by " + who +
                                     ". Your strike count was reset and you can reserve rooms again." +
                                     (note == "" ? "" : " Note: " + note);
                        if (msg.Length > 400) msg = msg.Substring(0, 397) + "...";

                        using (var cmd = new SqlCommand(
                            @"INSERT INTO Notifications (StudentID, ReservationID, Message, IsRead, CreatedAt)
                              VALUES (@s, NULL, @m, 0, @at)", con, tx))
                        {
                            cmd.Parameters.Add(Guades_QCUCram_Db.P("@s", studentId));
                            cmd.Parameters.Add(Guades_QCUCram_Db.P("@m", msg));
                            cmd.Parameters.Add(Guades_QCUCram_Db.P("@at", Guades_QCUCram_Clock.Now));
                            cmd.ExecuteNonQuery();
                        }

                        tx.Commit();
                        return true;
                    }
                    catch
                    {
                        try { tx.Rollback(); } catch { }
                        throw;
                    }
                }
            }
        }
    }
}