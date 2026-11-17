using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;
using System.Web;
using Guades_QCUCram.Helpers;

namespace Guades_QCUCram
{
    public partial class Guades_QCUCram_Availability : Guades_QCUCram_SecurePage
    {
        private const string DateFormat = "yyyy-MM-dd";
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        // Reservations with these statuses do NOT hold a slot
        private const string FreeStatuses = "('Declined','Cancelled','Forfeited')";

        // Text of the Booking Rules pop-up (built from Settings / TimeSlots in Page_PreRender)
        protected string RulesHtml = "";

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // Default = the booking day (tomorrow), or ?date=yyyy-MM-dd from a link
                DateTime d = BookingDay();
                DateTime fromQuery;
                string q = Request.QueryString["date"];
                if (!string.IsNullOrEmpty(q) &&
                    DateTime.TryParseExact(q, DateFormat, Inv, DateTimeStyles.None, out fromQuery))
                {
                    d = fromQuery.Date;
                }
                txtDate.Text = d.ToString(DateFormat, Inv);
            }
        }

        // PreRender runs AFTER the button clicks, so the grid always matches the final date
        protected void Page_PreRender(object sender, EventArgs e)
        {
            RulesHtml = Guades_QCUCram_SettingsHelper.BuildRulesHtml();
            ShowGrid();
        }

        protected void btnShow_Click(object sender, EventArgs e)
        {
            // Nothing to do: the grid is drawn in Page_PreRender from txtDate
        }

        protected void btnPrev_Click(object sender, EventArgs e)
        {
            txtDate.Text = GetSelectedDate().AddDays(-1).ToString(DateFormat, Inv);
        }

        protected void btnNext_Click(object sender, EventArgs e)
        {
            txtDate.Text = GetSelectedDate().AddDays(1).ToString(DateFormat, Inv);
        }

        protected void btnBookingDay_Click(object sender, EventArgs e)
        {
            txtDate.Text = BookingDay().ToString(DateFormat, Inv);
        }

        // The only date students may book = today + AdvanceBookingDays (default: tomorrow)
        private DateTime BookingDay()
        {
            int ahead = Guades_QCUCram_SettingsHelper.GetInt("AdvanceBookingDays", 1);
            return Guades_QCUCram_Clock.Today.AddDays(ahead);
        }

        private DateTime GetSelectedDate()
        {
            DateTime d;
            if (DateTime.TryParseExact((txtDate.Text ?? "").Trim(), DateFormat, Inv, DateTimeStyles.None, out d))
                return d.Date;

            d = BookingDay();
            txtDate.Text = d.ToString(DateFormat, Inv);
            ShowMessage(lblMessage, "That date isn't valid, so the booking day is shown instead.", true);
            return d;
        }

        private void ShowGrid()
        {
            DateTime date = GetSelectedDate();
            string dateText = date.ToString(DateFormat, Inv);
            DateTime bookingDay = BookingDay();
            bool isStudent = CurrentRole == RoleStudent;
            bool isAdmin = CurrentRole == RoleAdmin;
            int myId = CurrentUserId;

            // --- 3 queries total for the whole grid ---
            // Rooms: active ones, plus disabled ones that still have bookings on this date
            DataTable rooms = Guades_QCUCram_Db.Query(
                @"SELECT RoomID, RoomName, IsActive FROM Rooms
                   WHERE IsActive = 1
                      OR RoomID IN (SELECT RoomID FROM Reservations
                                     WHERE ReservationDate = @d AND Status NOT IN " + FreeStatuses + @")
                   ORDER BY RoomID",
                Guades_QCUCram_Db.P("@d", date));

            DataTable slots = Guades_QCUCram_Db.Query(
                @"SELECT TimeSlotID, StartTime, EndTime, IsActive FROM TimeSlots
                   WHERE IsActive = 1
                      OR TimeSlotID IN (SELECT TimeSlotID FROM Reservations
                                         WHERE ReservationDate = @d AND Status NOT IN " + FreeStatuses + @")
                   ORDER BY StartTime",
                Guades_QCUCram_Db.P("@d", date));

            DataTable res = Guades_QCUCram_Db.Query(
                @"SELECT r.ReservationID, r.RoomID, r.TimeSlotID, r.Status, r.StudentID,
                         s.StudentNumber, s.LastName, s.FirstName
                    FROM Reservations r
                    JOIN Students s ON s.StudentID = r.StudentID
                   WHERE r.ReservationDate = @d AND r.Status NOT IN " + FreeStatuses,
                Guades_QCUCram_Db.P("@d", date));

            // Lookup: "roomId|slotId" -> reservation row
            var byCell = new Dictionary<string, DataRow>();
            foreach (DataRow r in res.Rows)
                byCell[Convert.ToString(r["RoomID"]) + "|" + Convert.ToString(r["TimeSlotID"])] = r;

            if (rooms.Rows.Count == 0 || slots.Rows.Count == 0)
            {
                litInfo.Text = "";
                litGrid.Text = "<div class=\"alert alert-warning\">No active rooms or time slots are set up yet.</div>";
                return;
            }

            int open = 0, pending = 0, taken = 0, closed = 0;

            // An EMPTY cell is "Closed" when its date is before the day students may book
            // (today + AdvanceBookingDays). Pending / Taken / "Your booking" cells keep their own labels,
            // even in the past, so history stays readable. Uses Clock.Today (via BookingDay), so the Dev Clock works.
            bool dateClosed = date < bookingDay;
            var sb = new StringBuilder();

            sb.Append("<div class=\"table-responsive\"><table class=\"table table-bordered text-center align-middle\">");
            sb.Append("<thead class=\"table-light\"><tr><th>Time</th>");
            foreach (DataRow room in rooms.Rows)
            {
                bool roomActive = Convert.ToBoolean(room["IsActive"]);
                sb.Append("<th>").Append(HttpUtility.HtmlEncode(Convert.ToString(room["RoomName"])));
                if (!roomActive) sb.Append(" <small class=\"text-muted\">(disabled)</small>");
                sb.Append("</th>");
            }
            sb.Append("</tr></thead><tbody>");

            foreach (DataRow slot in slots.Rows)
            {
                int slotId = Convert.ToInt32(slot["TimeSlotID"]);
                bool slotActive = Convert.ToBoolean(slot["IsActive"]);
                string slotLabel = Guades_QCUCram_ReservationService.FormatSlot(
                    (TimeSpan)slot["StartTime"], (TimeSpan)slot["EndTime"]);

                sb.Append("<tr><th class=\"text-nowrap\">").Append(HttpUtility.HtmlEncode(slotLabel));
                if (!slotActive) sb.Append(" <small class=\"text-muted\">(disabled)</small>");
                sb.Append("</th>");

                foreach (DataRow room in rooms.Rows)
                {
                    int roomId = Convert.ToInt32(room["RoomID"]);
                    bool roomActive = Convert.ToBoolean(room["IsActive"]);

                    DataRow r;
                    if (byCell.TryGetValue(roomId + "|" + slotId, out r))
                    {
                        // ---- Slot is held by a reservation ----
                        int id = Convert.ToInt32(r["ReservationID"]);
                        string status = Convert.ToString(r["Status"]);
                        bool isPending = status == Guades_QCUCram_Status.Pending;
                        bool mine = isStudent && Convert.ToInt32(r["StudentID"]) == myId;

                        string cls, text;
                        if (mine)
                        {
                            cls = "table-primary";
                            text = (isPending ? "Your request" : "Your booking") + " #" + id +
                                   "<br /><small>" + HttpUtility.HtmlEncode(status) + "</small>";
                        }
                        else if (isAdmin)
                        {
                            string who = Convert.ToString(r["LastName"]) + ", " + Convert.ToString(r["FirstName"]) +
                                         " (" + Convert.ToString(r["StudentNumber"]) + ")";
                            cls = isPending ? "table-warning" : "table-danger";
                            text = (isPending ? "Pending" : "Taken") + " #" + id +
                                   "<br /><small>" + HttpUtility.HtmlEncode(who) + "</small>";
                        }
                        else
                        {
                            cls = isPending ? "table-warning" : "table-danger";
                            text = isPending ? "Pending" : "Taken";
                        }

                        if (isPending) pending++; else taken++;
                        sb.Append("<td class=\"").Append(cls).Append("\">").Append(text).Append("</td>");
                    }
                    else if (!roomActive || !slotActive)
                    {
                        sb.Append("<td class=\"table-secondary text-muted\">Unavailable</td>");
                    }
                    else if (dateClosed)
                    {
                        // ---- Closed: empty slot on today or an earlier day ----
                        closed++;
                        sb.Append("<td class=\"qcu-closed\">Closed</td>");
                    }
                    else
                    {
                        // ---- Open ----
                        open++;
                        bool canBook = isStudent && date == bookingDay;
                        if (canBook)
                        {
                            string url = ResolveUrl("~/Guades_QCUCram_Student/Guades_QCUCram_Reserve.aspx") +
                                         "?room=" + roomId + "&slot=" + slotId + "&date=" + dateText;
                            sb.Append("<td class=\"table-success\"><a href=\"")
                              .Append(HttpUtility.HtmlAttributeEncode(url))
                              .Append("\">Open - Book</a></td>");
                        }
                        else
                        {
                            sb.Append("<td class=\"table-success\">Open</td>");
                        }
                    }
                }
                sb.Append("</tr>");
            }
            sb.Append("</tbody></table></div>");
            litGrid.Text = sb.ToString();

            // --- Info above the grid ---
            var info = new StringBuilder();
            info.Append("<p class=\"lead mb-1\">")
                .Append(HttpUtility.HtmlEncode(date.ToString("dddd, MMMM d, yyyy", Inv)))
                .Append("</p>");
            info.Append("<p class=\"mb-2\">Open: <strong>").Append(open)
                .Append("</strong> &middot; Pending: <strong>").Append(pending)
                .Append("</strong> &middot; Taken: <strong>").Append(taken)
                .Append("</strong> &middot; Closed: <strong>").Append(closed).Append("</strong></p>");

            if (isStudent)
            {
                if (date == bookingDay)
                {
                    info.Append("<p class=\"text-success\">You can book open slots on this date. Click an <em>Open - Book</em> cell.</p>");
                }
                else
                {
                    info.Append("<p class=\"text-muted\">Bookings are only accepted for ")
                        .Append(HttpUtility.HtmlEncode(bookingDay.ToString("dddd, MMMM d, yyyy", Inv)))
                        .Append(". Click <em>Booking day</em> to go there.</p>");
                }
            }
            litInfo.Text = info.ToString();
        }
    }
}