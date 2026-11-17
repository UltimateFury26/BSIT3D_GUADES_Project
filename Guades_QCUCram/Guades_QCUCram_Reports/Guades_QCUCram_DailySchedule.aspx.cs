using System;
using System.Data;
using System.Globalization;
using System.Web;
using Guades_QCUCram.Helpers;

namespace Guades_QCUCram.Guades_QCUCram_Reports
{
    // Printable list of all confirmed bookings for one date (for the library desk).
    public partial class Guades_QCUCram_DailySchedule : Guades_QCUCram_AdminPage
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        private const string DateFmt = "yyyy-MM-dd";

        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) return;

            // Optional: ...DailySchedule.aspx?date=2026-10-07 opens a specific day. Default = today.
            DateTime d;
            if (!DateTime.TryParseExact((Request.QueryString["date"] ?? "").Trim(), DateFmt, Inv,
                                        DateTimeStyles.None, out d))
                d = Guades_QCUCram_Clock.Today;

            txtDate.Text = d.ToString(DateFmt, Inv);
            LoadSchedule();
        }

        // ------------------------------------------------------------------ buttons
        protected void btnShow_Click(object sender, EventArgs e) { LoadSchedule(); }

        protected void btnPrev_Click(object sender, EventArgs e) { ShiftDay(-1); }

        protected void btnNext_Click(object sender, EventArgs e) { ShiftDay(1); }

        protected void btnToday_Click(object sender, EventArgs e)
        {
            txtDate.Text = Guades_QCUCram_Clock.Today.ToString(DateFmt, Inv);
            LoadSchedule();
        }

        protected void btnTomorrow_Click(object sender, EventArgs e)
        {
            txtDate.Text = Guades_QCUCram_Clock.Today.AddDays(1).ToString(DateFmt, Inv);
            LoadSchedule();
        }

        private void ShiftDay(int days)
        {
            DateTime d;
            if (!TryReadDate(out d)) return;
            txtDate.Text = d.AddDays(days).ToString(DateFmt, Inv);
            LoadSchedule();
        }

        // ------------------------------------------------------------------ helpers
        private bool TryReadDate(out DateTime date)
        {
            if (!DateTime.TryParseExact((txtDate.Text ?? "").Trim(), DateFmt, Inv, DateTimeStyles.None, out date))
            {
                ShowError("Please choose a valid date.");
                return false;
            }
            return true;
        }

        private void ShowError(string text)
        {
            pnlResults.Visible = false;
            ShowMessage(lblMessage, text, true);
        }

        // ------------------------------------------------------------------ load
        private void LoadSchedule()
        {
            lblMessage.Visible = false;

            DateTime date;
            if (!TryReadDate(out date)) return;

            try
            {
                // "Confirmed" = Approved, Reassigned, CheckedIn, Completed
                DataTable dt = Guades_QCUCram_Db.Query(
                    @"SELECT r.ReservationID, ts.StartTime, ts.EndTime, rm.RoomName,
                             s.StudentNumber, s.LastName, s.FirstName, s.MI, s.RequesterType, s.Program,
                             r.NumberOfMembers, r.Purpose, r.Status
                        FROM Reservations r
                        JOIN Students s   ON s.StudentID = r.StudentID
                        JOIN Rooms rm     ON rm.RoomID = r.RoomID
                        JOIN TimeSlots ts ON ts.TimeSlotID = r.TimeSlotID
                       WHERE r.ReservationDate = @d
                         AND r.Status IN ('Approved','Reassigned','CheckedIn','Completed')
                       ORDER BY ts.StartTime, rm.RoomName",
                    Guades_QCUCram_Db.P("@d", date.Date));

                dt.Columns.Add("TimeText", typeof(string));
                dt.Columns.Add("RequesterHtml", typeof(string));

                foreach (DataRow row in dt.Rows)
                {
                    row["TimeText"] = Guades_QCUCram_ReservationService.FormatSlot(
                        (TimeSpan)row["StartTime"], (TimeSpan)row["EndTime"]);

                    string mi = Convert.ToString(row["MI"]);
                    string name = Convert.ToString(row["LastName"]) + ", " + Convert.ToString(row["FirstName"]) +
                                  (string.IsNullOrWhiteSpace(mi) ? "" : " " + mi + ".");
                    row["RequesterHtml"] = "<strong>" + HttpUtility.HtmlEncode(name) + "</strong><br /><small>" +
                                           HttpUtility.HtmlEncode(Convert.ToString(row["StudentNumber"]) + " | " +
                                                                  Convert.ToString(row["RequesterType"])) + "</small>";
                }

                gvSchedule.DataSource = dt;
                gvSchedule.DataBind();

                lblDateTitle.Text = HttpUtility.HtmlEncode(date.ToString("dddd, MMMM d, yyyy", Inv));
                lblCount.Text = dt.Rows.Count.ToString(Inv) + " confirmed booking(s).";

                int pending = Convert.ToInt32(Guades_QCUCram_Db.Scalar(
                    "SELECT COUNT(*) FROM Reservations WHERE ReservationDate = @d AND Status = 'Pending'",
                    Guades_QCUCram_Db.P("@d", date.Date)));
                lblPending.Text = pending > 0
                    ? pending.ToString(Inv) + " pending request(s) for this date are not listed."
                    : "";

                litPrintMeta.Text = "Printed " +
                    HttpUtility.HtmlEncode(Guades_QCUCram_Clock.Now.ToString("MMM d, yyyy h:mm tt", Inv)) +
                    " by " + HttpUtility.HtmlEncode(CurrentUserName);

                pnlResults.Visible = true;
            }
            catch (Exception)
            {
                ShowError("Sorry, the schedule could not be loaded. Please try again.");
            }
        }

        // ------------------------------------------------------------------ display
        protected string StatusBadge(object status)
        {
            string s = Convert.ToString(status);
            string css;

            switch (s)
            {
                case Guades_QCUCram_Status.Approved: css = "bg-success"; break;
                case Guades_QCUCram_Status.Reassigned: css = "bg-info text-dark"; break;
                case Guades_QCUCram_Status.CheckedIn: css = "bg-primary"; break;
                case Guades_QCUCram_Status.Completed: css = "bg-dark"; break;
                default: css = "bg-light text-dark"; break;
            }

            string label = (s == Guades_QCUCram_Status.CheckedIn) ? "Checked In" : s;
            return "<span class=\"badge " + css + "\">" + HttpUtility.HtmlEncode(label) + "</span>";
        }
    }
}