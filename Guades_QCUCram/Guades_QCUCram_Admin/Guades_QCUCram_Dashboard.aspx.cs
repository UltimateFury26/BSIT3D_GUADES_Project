using System;
using System.Data;
using System.Globalization;
using System.Text;
using System.Web;
using Guades_QCUCram.Helpers;

namespace Guades_QCUCram.Guades_QCUCram_Admin
{
    public partial class Guades_QCUCram_Dashboard : Guades_QCUCram_AdminPage
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        protected void Page_Load(object sender, EventArgs e)
        {
            // The automatic sweep already ran in SecurePage.OnLoad, so these numbers are fresh.
            DateTime now = Guades_QCUCram_Clock.Now;   // honours the Dev Tools clock
            DateTime today = now.Date;

            try
            {
                lblWelcome.Text = "Welcome, " + HttpUtility.HtmlEncode(CurrentUserName) + ".";

                int offset = Guades_QCUCram_Clock.OffsetMinutes;
                lblClock.Text = "System time: <strong>" +
                                HttpUtility.HtmlEncode(now.ToString("ddd, MMM d, yyyy h:mm tt", Inv)) + "</strong>" +
                                (offset != 0 ? " &middot; Dev Tools clock is shifted by " + offset + " minute(s)." : "");

                BuildCards(today);
                BuildUpcoming(now, today);
            }
            catch (Exception)
            {
                ShowMessage(lblMessage, "Sorry, the dashboard could not be fully loaded. Please refresh the page.", true);
            }
        }

        // ------------------------------------------------------------------ count cards
        private void BuildCards(DateTime today)
        {
            // One round trip, six counts. The definitions match the pages the cards link to:
            //  - Awaiting / Checked in  = the same lists the Check-In page shows (today's date)
            //  - Restricted             = the same list the Restrictions page shows
            //  - Forfeited today        = forfeited bookings whose slot day is today
            DataTable dt = Guades_QCUCram_Db.Query(
                @"SELECT
                    (SELECT COUNT(*) FROM Reservations WHERE Status = 'Pending') AS CntPending,
                    (SELECT COUNT(*) FROM Reservations
                      WHERE ReservationDate = @today AND Status IN ('Approved','Reassigned')) AS CntAwaiting,
                    (SELECT COUNT(*) FROM Reservations
                      WHERE ReservationDate = @today AND Status = 'CheckedIn') AS CntCheckedIn,
                    (SELECT COUNT(*) FROM Reservations
                      WHERE ReservationDate = @today AND Status = 'Forfeited') AS CntForfeited,
                    (SELECT COUNT(*) FROM Students WHERE IsRestricted = 1) AS CntRestricted,
                    (SELECT COUNT(*) FROM Reservations) AS CntTotal",
                Guades_QCUCram_Db.P("@today", today));

            DataRow c = dt.Rows[0];
            int pending = Convert.ToInt32(c["CntPending"]);
            int awaiting = Convert.ToInt32(c["CntAwaiting"]);
            int checkedIn = Convert.ToInt32(c["CntCheckedIn"]);
            int forfeited = Convert.ToInt32(c["CntForfeited"]);
            int restricted = Convert.ToInt32(c["CntRestricted"]);
            int total = Convert.ToInt32(c["CntTotal"]);

            string day = today.ToString("yyyy-MM-dd", Inv);
            string queue = ResolveUrl("~/Guades_QCUCram_Admin/Guades_QCUCram_Queue.aspx");
            string checkIn = ResolveUrl("~/Guades_QCUCram_Admin/Guades_QCUCram_CheckIn.aspx");
            string records = ResolveUrl("~/Guades_QCUCram_Admin/Guades_QCUCram_Records.aspx");
            string restrictions = ResolveUrl("~/Guades_QCUCram_Admin/Guades_QCUCram_Restrictions.aspx");

            var sb = new StringBuilder();
            sb.Append(Card(queue, "Pending requests", pending,
                           "Waiting for approval or decline", pending > 0 ? "warning" : "secondary"));
            sb.Append(Card(checkIn, "Awaiting check-in today", awaiting,
                           "Approved or reassigned for today", "primary"));
            sb.Append(Card(checkIn, "Checked in now", checkedIn,
                           "Today's groups currently in a room", "success"));
            sb.Append(Card(records + "?status=Forfeited&from=" + day + "&to=" + day, "Forfeited today", forfeited,
                           "No-shows from today's slots", forfeited > 0 ? "danger" : "secondary"));
            sb.Append(Card(restrictions, "Currently restricted", restricted,
                           "Students barred from booking", restricted > 0 ? "danger" : "secondary"));
            sb.Append(Card(records, "Total reservations", total,
                           "All records, any status", "secondary"));

            litCards.Text = sb.ToString();
        }

        private static string Card(string href, string title, int count, string hint, string accent)
        {
            return "<div class=\"col-sm-6 col-lg-4\">" +
                   "<a href=\"" + HttpUtility.HtmlAttributeEncode(href) + "\" class=\"card h-100 text-decoration-none border-" + accent + "\">" +
                   "<div class=\"card-body\">" +
                   "<div class=\"text-muted small text-uppercase\">" + HttpUtility.HtmlEncode(title) + "</div>" +
                   "<div class=\"display-5 fw-bold text-body\">" + count + "</div>" +
                   "<div class=\"small text-muted\">" + HttpUtility.HtmlEncode(hint) + "</div>" +
                   "</div></a></div>";
        }

        // ------------------------------------------------------------------ next 5 upcoming bookings
        private void BuildUpcoming(DateTime now, DateTime today)
        {
            // Upcoming = still holding a slot that has not ended yet and is not already checked in.
            // Pending requests are included because they hold the slot too (the badge tells them apart).
            DataTable dt = Guades_QCUCram_Db.Query(
                @"SELECT TOP 5 r.ReservationID, r.ReservationDate, r.Status, r.NumberOfMembers,
                         ts.StartTime, ts.EndTime, rm.RoomName,
                         s.StudentNumber, s.LastName, s.FirstName, s.MI
                    FROM Reservations r
                    JOIN Students s   ON s.StudentID = r.StudentID
                    JOIN Rooms rm     ON rm.RoomID = r.RoomID
                    JOIN TimeSlots ts ON ts.TimeSlotID = r.TimeSlotID
                   WHERE r.Status IN ('Pending','Approved','Reassigned')
                     AND CAST(r.ReservationDate AS DATETIME) + CAST(ts.EndTime AS DATETIME) > @now
                   ORDER BY r.ReservationDate, ts.StartTime, r.ReservationID",
                Guades_QCUCram_Db.P("@now", now));

            if (dt.Rows.Count == 0)
            {
                litUpcoming.Text = "<p class=\"text-muted mb-0\">No upcoming bookings.</p>";
                return;
            }

            var sb = new StringBuilder();
            sb.Append("<div class=\"table-responsive\">");
            sb.Append("<table class=\"table table-bordered table-striped align-middle mb-0\">");
            sb.Append("<thead class=\"table-light\"><tr><th>#</th><th>Date</th><th>Time</th><th>Room</th><th>Requester</th><th>Members</th><th>Status</th></tr></thead><tbody>");

            foreach (DataRow r in dt.Rows)
            {
                DateTime date = Convert.ToDateTime(r["ReservationDate"]).Date;
                string dateHtml = HttpUtility.HtmlEncode(date.ToString("ddd, MMM d, yyyy", Inv));
                if (date == today) dateHtml += " <span class=\"badge text-bg-primary\">Today</span>";
                else if (date == today.AddDays(1)) dateHtml += " <span class=\"badge text-bg-secondary\">Tomorrow</span>";

                string mi = Convert.ToString(r["MI"]);
                string name = Convert.ToString(r["LastName"]) + ", " + Convert.ToString(r["FirstName"]) +
                              (string.IsNullOrWhiteSpace(mi) ? "" : " " + mi.TrimEnd('.') + ".");

                sb.Append("<tr>");
                sb.Append("<td>").Append(Convert.ToInt32(r["ReservationID"])).Append("</td>");
                sb.Append("<td class=\"text-nowrap\">").Append(dateHtml).Append("</td>");
                sb.Append("<td class=\"text-nowrap\">")
                  .Append(HttpUtility.HtmlEncode(Guades_QCUCram_ReservationService.FormatSlot(
                      (TimeSpan)r["StartTime"], (TimeSpan)r["EndTime"])))
                  .Append("</td>");
                sb.Append("<td>").Append(HttpUtility.HtmlEncode(Convert.ToString(r["RoomName"]))).Append("</td>");
                sb.Append("<td>").Append(HttpUtility.HtmlEncode(name))
                  .Append("<br /><small class=\"text-muted\">").Append(HttpUtility.HtmlEncode(Convert.ToString(r["StudentNumber"])))
                  .Append("</small></td>");
                sb.Append("<td>").Append(Convert.ToInt32(r["NumberOfMembers"])).Append("</td>");
                sb.Append("<td>").Append(StatusBadge(Convert.ToString(r["Status"]))).Append("</td>");
                sb.Append("</tr>");
            }

            sb.Append("</tbody></table></div>");
            litUpcoming.Text = sb.ToString();
        }

        private static string StatusBadge(string status)
        {
            string css;
            switch (status)
            {
                case Guades_QCUCram_Status.Pending: css = "bg-warning text-dark"; break;
                case Guades_QCUCram_Status.Approved: css = "bg-success"; break;
                case Guades_QCUCram_Status.Reassigned: css = "bg-info text-dark"; break;
                case Guades_QCUCram_Status.Declined: css = "bg-danger"; break;
                case Guades_QCUCram_Status.Cancelled: css = "bg-secondary"; break;
                case Guades_QCUCram_Status.CheckedIn: css = "bg-primary"; break;
                case Guades_QCUCram_Status.Completed: css = "bg-dark"; break;
                case Guades_QCUCram_Status.Forfeited: css = "bg-danger"; break;
                default: css = "bg-light text-dark"; break;
            }
            string label = status == Guades_QCUCram_Status.CheckedIn ? "Checked In" : status;
            return "<span class=\"badge " + css + "\">" + HttpUtility.HtmlEncode(label) + "</span>";
        }
    }
}