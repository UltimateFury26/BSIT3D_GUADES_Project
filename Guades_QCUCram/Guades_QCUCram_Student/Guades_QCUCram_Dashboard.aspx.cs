using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;
using System.Web;
using System.Web.UI.WebControls;
using Guades_QCUCram.Helpers;

namespace Guades_QCUCram.Guades_QCUCram_Student
{
    public partial class Guades_QCUCram_Dashboard : Guades_QCUCram_StudentPage
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        private const string DateFormat = "yyyy-MM-dd";

        // These reservations do NOT hold a slot
        private const string FreeStatuses = "('Declined','Cancelled','Forfeited')";

        // Calendar data: date -> rank (3 = ongoing, 2 = reserved, 1 = done) and tooltip lines
        private readonly Dictionary<DateTime, int> _dayRank = new Dictionary<DateTime, int>();
        private readonly Dictionary<DateTime, List<string>> _dayTips = new Dictionary<DateTime, List<string>>();

        protected void Page_Load(object sender, EventArgs e)
        {
            DateTime today = Guades_QCUCram_Clock.Today;   // honours the Dev Tools clock

            calBookings.TodaysDate = today;
            if (!IsPostBack) calBookings.VisibleDate = today;

            lblWelcome.Text = "Welcome, " + HttpUtility.HtmlEncode(CurrentUserName) + ".";

            try
            {
                int ahead = Guades_QCUCram_SettingsHelper.GetInt("AdvanceBookingDays", 1);
                DateTime bookingDay = today.AddDays(ahead);

                bool restricted = ShowRestrictionBanner();
                SetupQuickReserve(bookingDay, restricted);

                DataTable mine = LoadMyReservations();
                ShowUpcoming(mine);
                ShowRecent(mine);
                BuildCalendarData(mine);

                ShowGlance(bookingDay, ahead);
            }
            catch (Exception)
            {
                ShowMessage(lblMessage, "Sorry, the dashboard could not be fully loaded. Please refresh the page.", true);
            }
        }

        // ------------------------------------------------------------------ restriction + quick reserve
        // Returns true when the student is currently restricted.
        private bool ShowRestrictionBanner()
        {
            DataTable dt = Guades_QCUCram_Db.Query(
                "SELECT NoShowCount, IsRestricted, RestrictionLiftDate FROM Students WHERE StudentID = @id",
                Guades_QCUCram_Db.P("@id", CurrentUserId));
            if (dt.Rows.Count == 0) return false;

            DataRow me = dt.Rows[0];
            if (!Convert.ToBoolean(me["IsRestricted"])) return false;

            // A restriction whose cooldown has passed is about to be lifted by the sweep; don't show it.
            if (me["RestrictionLiftDate"] != DBNull.Value &&
                Convert.ToDateTime(me["RestrictionLiftDate"]) <= Guades_QCUCram_Clock.Now)
                return false;

            string until = me["RestrictionLiftDate"] == DBNull.Value
                ? "further notice"
                : Convert.ToDateTime(me["RestrictionLiftDate"]).ToString("MMM d, yyyy h:mm tt", Inv);

            litRestricted.Text = "<strong>You are restricted from booking until " + HttpUtility.HtmlEncode(until) +
                                 ".</strong> Reason: " + Convert.ToInt32(me["NoShowCount"]) + " forfeited reservation(s).";
            pnlRestricted.Visible = true;
            return true;
        }

        private void SetupQuickReserve(DateTime bookingDay, bool restricted)
        {
            hlQuickReserve.NavigateUrl = "~/Guades_QCUCram_Student/Guades_QCUCram_Reserve.aspx?date=" +
                                         bookingDay.ToString(DateFormat, Inv);
            if (restricted)
            {
                hlQuickReserve.Enabled = false;
                hlQuickReserve.CssClass += " disabled";
            }
        }

        // ------------------------------------------------------------------ my reservations (one query, used 3 ways)
        private DataTable LoadMyReservations()
        {
            return Guades_QCUCram_Db.Query(
                @"SELECT r.ReservationID, r.ReservationDate, r.Status, r.CreatedAt,
                         ts.StartTime, ts.EndTime, rm.RoomName
                    FROM Reservations r
                    JOIN Rooms rm     ON rm.RoomID = r.RoomID
                    JOIN TimeSlots ts ON ts.TimeSlotID = r.TimeSlotID
                   WHERE r.StudentID = @id
                   ORDER BY r.ReservationDate DESC, ts.StartTime DESC, r.ReservationID DESC",
                Guades_QCUCram_Db.P("@id", CurrentUserId));
        }

        private static DateTime SlotStart(DataRow r)
        {
            return Convert.ToDateTime(r["ReservationDate"]).Date.Add((TimeSpan)r["StartTime"]);
        }

        private static DateTime SlotEnd(DataRow r)
        {
            return Convert.ToDateTime(r["ReservationDate"]).Date.Add((TimeSpan)r["EndTime"]);
        }

        private void ShowUpcoming(DataTable mine)
        {
            DateTime now = Guades_QCUCram_Clock.Now;
            var upcoming = new List<DataRow>();

            foreach (DataRow r in mine.Rows)
            {
                string s = Convert.ToString(r["Status"]);
                bool live = s == Guades_QCUCram_Status.Pending || s == Guades_QCUCram_Status.Approved ||
                            s == Guades_QCUCram_Status.Reassigned || s == Guades_QCUCram_Status.CheckedIn;
                if (live && SlotEnd(r) > now) upcoming.Add(r);
            }

            upcoming.Sort((a, b) => SlotStart(a).CompareTo(SlotStart(b)));
            litUpcoming.Text = RenderTable(upcoming, "You have no upcoming reservations.");
        }

        private void ShowRecent(DataTable mine)
        {
            var view = new DataView(mine) { Sort = "CreatedAt DESC, ReservationID DESC" };
            var recent = new List<DataRow>();
            for (int i = 0; i < view.Count && i < 5; i++)
                recent.Add(view[i].Row);

            litRecent.Text = RenderTable(recent, "You have not made any reservations yet.");
        }

        private string RenderTable(List<DataRow> rows, string emptyText)
        {
            if (rows.Count == 0)
                return "<p class=\"text-muted mb-0\">" + HttpUtility.HtmlEncode(emptyText) + "</p>";

            var sb = new StringBuilder();
            sb.Append("<div class=\"table-responsive\">");
            sb.Append("<table class=\"table table-sm table-bordered table-striped align-middle mb-0\">");
            sb.Append("<thead class=\"table-light\"><tr><th>#</th><th>Date</th><th>Time</th><th>Room</th><th>Status</th></tr></thead><tbody>");

            foreach (DataRow r in rows)
            {
                sb.Append("<tr>");
                sb.Append("<td>").Append(Convert.ToInt32(r["ReservationID"])).Append("</td>");
                sb.Append("<td class=\"text-nowrap\">")
                  .Append(HttpUtility.HtmlEncode(Convert.ToDateTime(r["ReservationDate"]).ToString("ddd, MMM d, yyyy", Inv)))
                  .Append("</td>");
                sb.Append("<td class=\"text-nowrap\">")
                  .Append(HttpUtility.HtmlEncode(Guades_QCUCram_ReservationService.FormatSlot(
                      (TimeSpan)r["StartTime"], (TimeSpan)r["EndTime"])))
                  .Append("</td>");
                sb.Append("<td>").Append(HttpUtility.HtmlEncode(Convert.ToString(r["RoomName"]))).Append("</td>");
                sb.Append("<td>").Append(StatusBadge(Convert.ToString(r["Status"]))).Append("</td>");
                sb.Append("</tr>");
            }

            sb.Append("</tbody></table></div>");
            return sb.ToString();
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

        // ------------------------------------------------------------------ availability at a glance
        private void ShowGlance(DateTime bookingDay, int ahead)
        {
            string when = ahead == 1 ? "tomorrow" : ahead == 0 ? "today" : "in " + ahead + " days";
            lblGlanceTitle.Text = HttpUtility.HtmlEncode("Availability " + when + " (" +
                                  bookingDay.ToString("ddd, MMM d", Inv) + ")");

            DataTable rooms = Guades_QCUCram_Db.Query(
                @"SELECT rm.RoomID, rm.RoomName,
                         (SELECT COUNT(*) FROM TimeSlots WHERE IsActive = 1) AS TotalSlots,
                         (SELECT COUNT(*)
                            FROM Reservations r
                            JOIN TimeSlots t ON t.TimeSlotID = r.TimeSlotID
                           WHERE r.RoomID = rm.RoomID AND r.ReservationDate = @d
                             AND t.IsActive = 1 AND r.Status NOT IN " + FreeStatuses + @") AS Held
                    FROM Rooms rm
                   WHERE rm.IsActive = 1
                   ORDER BY rm.RoomID",
                Guades_QCUCram_Db.P("@d", bookingDay));

            if (rooms.Rows.Count == 0)
            {
                litGlance.Text = "<p class=\"text-muted mb-0\">No rooms are open for booking right now.</p>";
                return;
            }

            var sb = new StringBuilder();
            sb.Append("<ul class=\"list-group mb-3\">");
            foreach (DataRow r in rooms.Rows)
            {
                int total = Convert.ToInt32(r["TotalSlots"]);
                int open = Math.Max(0, total - Convert.ToInt32(r["Held"]));

                string css = open == 0 ? "bg-danger" : (open <= 2 ? "bg-warning text-dark" : "bg-success");
                string text = open == 0 ? "Full" : open + "/" + total + " open";

                sb.Append("<li class=\"list-group-item d-flex justify-content-between align-items-center\">");
                sb.Append("<span>").Append(HttpUtility.HtmlEncode(Convert.ToString(r["RoomName"]))).Append("</span>");
                sb.Append("<span class=\"badge ").Append(css).Append("\">").Append(HttpUtility.HtmlEncode(text)).Append("</span>");
                sb.Append("</li>");
            }
            sb.Append("</ul>");

            sb.Append("<a href=\"").Append(ResolveUrl("~/Guades_QCUCram_Availability.aspx"))
              .Append("?date=").Append(bookingDay.ToString(DateFormat, Inv))
              .Append("\">See the full grid</a>");

            litGlance.Text = sb.ToString();
        }

        // ------------------------------------------------------------------ calendar
        private void BuildCalendarData(DataTable mine)
        {
            _dayRank.Clear();
            _dayTips.Clear();

            foreach (DataRow r in mine.Rows)
            {
                string status = Convert.ToString(r["Status"]);
                int rank;

                if (status == Guades_QCUCram_Status.CheckedIn) rank = 3;            // ongoing
                else if (status == Guades_QCUCram_Status.Pending ||
                         status == Guades_QCUCram_Status.Approved ||
                         status == Guades_QCUCram_Status.Reassigned) rank = 2;      // reserved
                else if (status == Guades_QCUCram_Status.Completed) rank = 1;       // done
                else continue;                                                      // declined / cancelled / forfeited: no color

                DateTime day = Convert.ToDateTime(r["ReservationDate"]).Date;

                int existing;
                if (!_dayRank.TryGetValue(day, out existing) || rank > existing)
                    _dayRank[day] = rank;

                List<string> tips;
                if (!_dayTips.TryGetValue(day, out tips))
                {
                    tips = new List<string>();
                    _dayTips[day] = tips;
                }
                tips.Add("#" + Convert.ToInt32(r["ReservationID"]) + " " + Convert.ToString(r["RoomName"]) + ", " +
                         Guades_QCUCram_ReservationService.FormatSlot((TimeSpan)r["StartTime"], (TimeSpan)r["EndTime"]) +
                         " (" + (status == Guades_QCUCram_Status.CheckedIn ? "Checked In" : status) + ")");
            }
        }

        protected void calBookings_DayRender(object sender, DayRenderEventArgs e)
        {
            DateTime day = e.Day.Date.Date;

            int rank;
            if (!_dayRank.TryGetValue(day, out rank)) return;

            string label;
            switch (rank)
            {
                case 3: e.Cell.CssClass = "bg-primary text-white fw-bold"; label = "Ongoing"; break;
                case 2: e.Cell.CssClass = "bg-success text-white fw-bold"; label = "Reserved"; break;
                default: e.Cell.CssClass = "bg-secondary text-white fw-bold"; label = "Done"; break;
            }

            List<string> tips;
            if (_dayTips.TryGetValue(day, out tips))
                e.Cell.ToolTip = label + ": " + string.Join("; ", tips.ToArray());
        }
    }
}