using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;
using System.Web;
using System.Web.UI.WebControls;
using Guades_QCUCram.Helpers;

namespace Guades_QCUCram.Guades_QCUCram_Admin
{
    public partial class Guades_QCUCram_CheckIn : Guades_QCUCram_AdminPage
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        // Shown as "Grace expiring" when this many minutes (or fewer) are left
        private const int ExpiringMinutes = 5;

        private const string BaseSql =
            @"SELECT r.ReservationID, r.ReservationDate, r.Status, r.CheckedInAt,
                     ts.StartTime, ts.EndTime, rm.RoomName, r.NumberOfMembers,
                     s.StudentNumber, s.LastName, s.FirstName, s.MI, s.RequesterType, s.Program
                FROM Reservations r
                JOIN Students s   ON s.StudentID = r.StudentID
                JOIN Rooms rm     ON rm.RoomID = r.RoomID
                JOIN TimeSlots ts ON ts.TimeSlotID = r.TimeSlotID ";

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack) BindAll();
        }

        // ------------------------------------------------------------------ binding
        private void BindAll()
        {
            DateTime now = Guades_QCUCram_Clock.Now;
            litNow.Text = "System time: <strong>" +
                          HttpUtility.HtmlEncode(now.ToString("ddd, MMM d, yyyy h:mm:ss tt", Inv)) +
                          "</strong> &middot; Grace period: <strong>" +
                          Guades_QCUCram_SettingsHelper.GetInt("GraceMinutes", 15) + " minutes</strong>";

            BindWaiting();
            BindCheckedIn();
        }

        private void BindWaiting()
        {
            string q = txtSearch.Text.Trim();

            string sql = BaseSql +
                "WHERE r.ReservationDate = @today AND r.Status IN ('Approved','Reassigned')";
            var ps = new List<SqlParameter> { Guades_QCUCram_Db.P("@today", Guades_QCUCram_Clock.Today) };

            if (q != "")
            {
                sql += " AND (CAST(r.ReservationID AS NVARCHAR(20)) = @q" +
                       " OR s.StudentNumber LIKE @like OR s.LastName LIKE @like)";
                ps.Add(Guades_QCUCram_Db.P("@q", q));
                ps.Add(Guades_QCUCram_Db.P("@like", "%" + q + "%"));
            }
            sql += " ORDER BY ts.StartTime, rm.RoomName";

            DataTable dt = Guades_QCUCram_Db.Query(sql, ps.ToArray());

            // Member names for today's bookings (one query for all rows)
            Dictionary<int, List<string>> memberMap = LoadMembers();

            dt.Columns.Add("WhenHtml", typeof(string));
            dt.Columns.Add("RequesterHtml", typeof(string));
            dt.Columns.Add("MembersHtml", typeof(string));
            dt.Columns.Add("StateHtml", typeof(string));
            dt.Columns.Add("CanCheckIn", typeof(bool));

            int grace = Guades_QCUCram_SettingsHelper.GetInt("GraceMinutes", 15);
            DateTime now = Guades_QCUCram_Clock.Now;

            foreach (DataRow row in dt.Rows)
            {
                int id = Convert.ToInt32(row["ReservationID"]);
                DateTime date = Convert.ToDateTime(row["ReservationDate"]).Date;
                DateTime start = date.Add((TimeSpan)row["StartTime"]);
                DateTime closes = start.AddMinutes(grace);

                row["WhenHtml"] = WhenHtml(row);
                row["RequesterHtml"] = RequesterHtml(row);

                // Members (expandable, so the assistant can verify QCIDs)
                var sb = new StringBuilder();
                List<string> names;
                int count = Convert.ToInt32(row["NumberOfMembers"]);
                sb.Append("<details><summary>").Append(count).Append(" members</summary><ol class=\"mb-0 small\">");
                if (memberMap.TryGetValue(id, out names))
                {
                    foreach (string n in names)
                        sb.Append("<li>").Append(HttpUtility.HtmlEncode(n)).Append("</li>");
                }
                sb.Append("</ol></details>");
                row["MembersHtml"] = sb.ToString();

                // State relative to the check-in window [start, start + grace]
                string badge, text;
                bool can;
                if (now < start)
                {
                    badge = "text-bg-secondary";
                    text = "Not yet open";
                    can = false;
                    row["StateHtml"] = "<span class=\"badge " + badge + "\">" + text + "</span><br /><small>Opens at " +
                                       start.ToString("h:mm tt", Inv) + "</small>";
                }
                else if (now <= closes)
                {
                    int left = (int)Math.Ceiling((closes - now).TotalMinutes);
                    bool expiring = left <= ExpiringMinutes;
                    badge = expiring ? "text-bg-warning" : "text-bg-success";
                    text = expiring ? "Grace expiring" : "Open for check-in";
                    can = true;
                    row["StateHtml"] = "<span class=\"badge " + badge + "\">" + text + "</span><br /><small>Closes in " +
                                       left + " min (" + closes.ToString("h:mm tt", Inv) + ")</small>";
                }
                else
                {
                    badge = "text-bg-danger";
                    text = "Past grace";
                    can = false;
                    row["StateHtml"] = "<span class=\"badge " + badge + "\">" + text + "</span><br /><small>" +
                                       "Closed at " + closes.ToString("h:mm tt", Inv) + " - will be forfeited</small>";
                }
                row["CanCheckIn"] = can;
            }

            gvCheckIn.DataSource = dt;
            gvCheckIn.DataBind();
        }

        private void BindCheckedIn()
        {
            DataTable dt = Guades_QCUCram_Db.Query(
                BaseSql + "WHERE r.ReservationDate = @today AND r.Status = 'CheckedIn' ORDER BY ts.StartTime, rm.RoomName",
                Guades_QCUCram_Db.P("@today", Guades_QCUCram_Clock.Today));

            dt.Columns.Add("WhenHtml", typeof(string));
            dt.Columns.Add("RequesterHtml", typeof(string));
            dt.Columns.Add("CheckedText", typeof(string));

            foreach (DataRow row in dt.Rows)
            {
                row["WhenHtml"] = WhenHtml(row);
                row["RequesterHtml"] = RequesterHtml(row);
                row["CheckedText"] = row["CheckedInAt"] == DBNull.Value
                    ? ""
                    : Convert.ToDateTime(row["CheckedInAt"]).ToString("h:mm tt", Inv);
            }

            gvCheckedIn.DataSource = dt;
            gvCheckedIn.DataBind();
        }

        private Dictionary<int, List<string>> LoadMembers()
        {
            var map = new Dictionary<int, List<string>>();
            DataTable dt = Guades_QCUCram_Db.Query(
                @"SELECT m.ReservationID, m.LastName, m.FirstName, m.MI
                    FROM ReservationMembers m
                    JOIN Reservations r ON r.ReservationID = m.ReservationID
                   WHERE r.ReservationDate = @today AND r.Status IN ('Approved','Reassigned')
                   ORDER BY m.ReservationID, m.MemberID",
                Guades_QCUCram_Db.P("@today", Guades_QCUCram_Clock.Today));

            foreach (DataRow m in dt.Rows)
            {
                int rid = Convert.ToInt32(m["ReservationID"]);
                string mi = Convert.ToString(m["MI"]);
                string name = Convert.ToString(m["LastName"]) + ", " + Convert.ToString(m["FirstName"]) +
                              (string.IsNullOrWhiteSpace(mi) ? "" : " " + mi);
                List<string> list;
                if (!map.TryGetValue(rid, out list))
                {
                    list = new List<string>();
                    map[rid] = list;
                }
                list.Add(name);
            }
            return map;
        }

        // ------------------------------------------------------------------ display helpers
        private static string WhenHtml(DataRow row)
        {
            string time = Guades_QCUCram_ReservationService.FormatSlot(
                (TimeSpan)row["StartTime"], (TimeSpan)row["EndTime"]);
            return "<strong>" + HttpUtility.HtmlEncode(time) + "</strong><br />" +
                   HttpUtility.HtmlEncode(Convert.ToString(row["RoomName"]));
        }

        private static string RequesterHtml(DataRow row)
        {
            string mi = Convert.ToString(row["MI"]);
            string name = Convert.ToString(row["LastName"]) + ", " + Convert.ToString(row["FirstName"]) +
                          (string.IsNullOrWhiteSpace(mi) ? "" : " " + mi);
            return "<strong>" + HttpUtility.HtmlEncode(name) + "</strong><br /><small>" +
                   HttpUtility.HtmlEncode(Convert.ToString(row["StudentNumber"]) + " | " +
                                          Convert.ToString(row["RequesterType"]) + " | " +
                                          Convert.ToString(row["Program"])) + "</small>";
        }

        // ------------------------------------------------------------------ buttons
        protected void btnSearch_Click(object sender, EventArgs e)
        {
            BindAll();
        }

        protected void btnClear_Click(object sender, EventArgs e)
        {
            txtSearch.Text = "";
            BindAll();
        }

        protected void btnRefresh_Click(object sender, EventArgs e)
        {
            BindAll();
        }

        protected void gvCheckIn_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "CheckInRes") return;

            int id;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out id)) return;

            DoCheckIn(id);
            BindAll();
        }

        // ------------------------------------------------------------------ check-in (window enforced here)
        private void DoCheckIn(int id)
        {
            var info = Guades_QCUCram_ReservationService.GetInfo(id);
            if (info == null)
            {
                ShowMessage(lblMessage, "Reservation #" + id + " was not found.", true);
                return;
            }
            if (info.Status != Guades_QCUCram_Status.Approved && info.Status != Guades_QCUCram_Status.Reassigned)
            {
                ShowMessage(lblMessage, "Reservation #" + id + " is " + info.Status + " and can't be checked in.", true);
                return;
            }

            int grace = Guades_QCUCram_SettingsHelper.GetInt("GraceMinutes", 15);
            DateTime now = Guades_QCUCram_Clock.Now;
            DateTime start = info.SlotStart;
            DateTime closes = start.AddMinutes(grace);

            if (now < start)
            {
                ShowMessage(lblMessage,
                    "Too early: check-in for reservation #" + id + " opens at " +
                    start.ToString("h:mm tt", Inv) + ".", true);
                return;
            }
            if (now > closes)
            {
                ShowMessage(lblMessage,
                    "The grace period for reservation #" + id + " ended at " +
                    closes.ToString("h:mm tt", Inv) + ". It will be forfeited.", true);
                return;
            }

            try
            {
                Guades_QCUCram_ReservationService.ChangeStatus(
                    id,
                    Guades_QCUCram_Status.CheckedIn,
                    Guades_QCUCram_ReservationService.ActorAdmin,
                    CurrentUserId,
                    null);

                ShowMessage(lblMessage,
                    "Reservation #" + id + " is checked in.", false);
            }
            catch (InvalidOperationException ex)
            {
                ShowMessage(lblMessage, ex.Message, true);
            }
            catch (Exception)
            {
                ShowMessage(lblMessage, "Something went wrong while checking in. Please try again.", true);
            }
        }
    }
}