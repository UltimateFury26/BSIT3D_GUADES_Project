using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;
using System.Web;
using System.Web.UI.WebControls;
using Guades_QCUCram.Helpers;

namespace Guades_QCUCram.Guades_QCUCram_Admin
{
    public partial class Guades_QCUCram_Queue : Guades_QCUCram_AdminPage
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        // Pop-up modals: true while each should show (plain fields, reset each request)
        private bool _reviewOpen;
        private bool _reassignOpen;
        protected string ReviewModalCss { get { return _reviewOpen ? " is-open" : ""; } }
        protected string ReassignModalCss { get { return _reassignOpen ? " is-open" : ""; } }

        private const string BaseSql =
            @"SELECT r.ReservationID, r.CreatedAt, r.ReservationDate, ts.StartTime, ts.EndTime,
                     rm.RoomName, r.NumberOfMembers, r.Purpose, r.Status,
                     s.StudentNumber, s.LastName, s.FirstName, s.MI, s.RequesterType, s.Program, s.NoShowCount
                FROM Reservations r
                JOIN Students s   ON s.StudentID = r.StudentID
                JOIN Rooms rm     ON rm.RoomID = r.RoomID
                JOIN TimeSlots ts ON ts.TimeSlotID = r.TimeSlotID ";

        // Request open in the review panel / reassign panel (survive postbacks)
        private int SelectedId
        {
            get { object v = ViewState["SelectedId"]; return v == null ? 0 : (int)v; }
            set { ViewState["SelectedId"] = value; }
        }

        private int ReassignId
        {
            get { object v = ViewState["ReassignId"]; return v == null ? 0 : (int)v; }
            set { ViewState["ReassignId"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack) BindAll();
        }

        // ------------------------------------------------------------------ grids
        private void BindAll()
        {
            BindPending();
            BindUpcoming();
        }

        private void BindPending()
        {
            DataTable dt = Guades_QCUCram_Db.Query(
                BaseSql + "WHERE r.Status = 'Pending' ORDER BY r.CreatedAt, r.ReservationID");
            AddDisplayColumns(dt);

            gvQueue.DataSource = dt;
            gvQueue.DataBind();

            litCount.Text = dt.Rows.Count == 0
                ? ""
                : "<strong>" + dt.Rows.Count + "</strong> pending request(s), oldest first.";
        }

        private void BindUpcoming()
        {
            // Approved/Reassigned bookings that have not started yet
            DataTable dt = Guades_QCUCram_Db.Query(
                BaseSql +
                @"WHERE r.Status IN ('Approved','Reassigned')
                    AND (r.ReservationDate > @today OR (r.ReservationDate = @today AND ts.StartTime > @nowTime))
                  ORDER BY r.ReservationDate, ts.StartTime, rm.RoomName",
                Guades_QCUCram_Db.P("@today", Guades_QCUCram_Clock.Today),
                Guades_QCUCram_Db.P("@nowTime", Guades_QCUCram_Clock.Now.TimeOfDay));
            AddDisplayColumns(dt);

            gvUpcoming.DataSource = dt;
            gvUpcoming.DataBind();
        }

        private static void AddDisplayColumns(DataTable dt)
        {
            dt.Columns.Add("SubmittedText", typeof(string));
            dt.Columns.Add("RequesterHtml", typeof(string));
            dt.Columns.Add("WhenHtml", typeof(string));
            dt.Columns.Add("StatusHtml", typeof(string));
            dt.Columns.Add("Overdue", typeof(bool));

            DateTime now = Guades_QCUCram_Clock.Now;
            foreach (DataRow row in dt.Rows)
            {
                DateTime date = Convert.ToDateTime(row["ReservationDate"]).Date;
                TimeSpan start = (TimeSpan)row["StartTime"];
                TimeSpan end = (TimeSpan)row["EndTime"];
                bool overdue = date.Add(start) <= now;

                row["SubmittedText"] = Convert.ToDateTime(row["CreatedAt"]).ToString("MMM d, h:mm tt", Inv);
                row["Overdue"] = overdue;
                row["StatusHtml"] = StatusBadge(Convert.ToString(row["Status"]));

                string mi = Convert.ToString(row["MI"]);
                string name = Convert.ToString(row["LastName"]) + ", " + Convert.ToString(row["FirstName"]) +
                              (string.IsNullOrWhiteSpace(mi) ? "" : " " + mi);
                row["RequesterHtml"] =
                    "<strong>" + HttpUtility.HtmlEncode(name) + "</strong><br /><small>" +
                    HttpUtility.HtmlEncode(Convert.ToString(row["StudentNumber"]) + " | " +
                                           Convert.ToString(row["RequesterType"]) + " | " +
                                           Convert.ToString(row["Program"])) +
                    "<br />Strikes: " + Convert.ToInt32(row["NoShowCount"]) + "</small>";

                row["WhenHtml"] =
                    HttpUtility.HtmlEncode(date.ToString("ddd, MMM d, yyyy", Inv)) + "<br />" +
                    HttpUtility.HtmlEncode(Guades_QCUCram_ReservationService.FormatSlot(start, end)) +
                    (overdue ? "<br /><span class=\"badge text-bg-danger\">Slot already started</span>" : "");
            }
        }

        private static string StatusBadge(string status)
        {
            string css;
            switch (status)
            {
                case "Pending": css = "text-bg-warning"; break;
                case "Approved": css = "text-bg-success"; break;
                case "Reassigned": css = "text-bg-info"; break;
                default: css = "text-bg-secondary"; break;
            }
            return "<span class=\"badge " + css + "\">" + HttpUtility.HtmlEncode(status) + "</span>";
        }

        // ------------------------------------------------------------------ grid buttons
        protected void gvQueue_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            int id;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out id)) return;

            if (e.CommandName == "ApproveRes") DoApprove(id);
            else if (e.CommandName == "ReviewRes") ShowDetails(id);
            else if (e.CommandName == "ReassignRes") StartReassign(id);
        }

        protected void gvUpcoming_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            int id;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out id)) return;

            if (e.CommandName == "ReassignRes") StartReassign(id);
        }

        // ------------------------------------------------------------------ review / decline panel
        private void ShowDetails(int id)
        {
            DataTable dt = Guades_QCUCram_Db.Query(BaseSql + "WHERE r.ReservationID = @id",
                                                   Guades_QCUCram_Db.P("@id", id));
            if (dt.Rows.Count == 0 || Convert.ToString(dt.Rows[0]["Status"]) != Guades_QCUCram_Status.Pending)
            {
                ShowMessage(lblMessage, "Reservation #" + id + " is no longer pending. The list has been refreshed.", true);
                _reviewOpen = false;
                BindAll();
                return;
            }

            CloseReassign();

            DataRow row = dt.Rows[0];
            DateTime date = Convert.ToDateTime(row["ReservationDate"]).Date;
            TimeSpan start = (TimeSpan)row["StartTime"];
            TimeSpan end = (TimeSpan)row["EndTime"];
            bool overdue = date.Add(start) <= Guades_QCUCram_Clock.Now;

            string mi = Convert.ToString(row["MI"]);
            string name = Convert.ToString(row["LastName"]) + ", " + Convert.ToString(row["FirstName"]) +
                          (string.IsNullOrWhiteSpace(mi) ? "" : " " + mi);

            var sb = new StringBuilder();
            if (overdue)
            {
                sb.Append("<div class=\"alert alert-warning\">The slot has already started, so this request can only be declined.</div>");
            }
            sb.Append("<dl class=\"row mb-0\">");
            AppendDt(sb, "Reservation #", Convert.ToString(id));
            AppendDt(sb, "Requester", name);
            AppendDt(sb, "ID / Type", Convert.ToString(row["StudentNumber"]) + " | " + Convert.ToString(row["RequesterType"]));
            AppendDt(sb, "Program", Convert.ToString(row["Program"]));
            AppendDt(sb, "Strikes (no-shows)", Convert.ToString(Convert.ToInt32(row["NoShowCount"])));
            AppendDt(sb, "Room", Convert.ToString(row["RoomName"]));
            AppendDt(sb, "Date", date.ToString("dddd, MMM d, yyyy", Inv));
            AppendDt(sb, "Time", Guades_QCUCram_ReservationService.FormatSlot(start, end));
            AppendDt(sb, "Group size", Convert.ToString(Convert.ToInt32(row["NumberOfMembers"])));
            AppendDt(sb, "Purpose", Convert.ToString(row["Purpose"]));
            AppendDt(sb, "Submitted", Convert.ToDateTime(row["CreatedAt"]).ToString("MMM d, yyyy h:mm tt", Inv));
            sb.Append("</dl>");

            DataTable members = Guades_QCUCram_Db.Query(
                "SELECT LastName, FirstName, MI FROM ReservationMembers WHERE ReservationID = @id ORDER BY MemberID",
                Guades_QCUCram_Db.P("@id", id));
            sb.Append("<h6 class=\"mt-3\">Members (").Append(members.Rows.Count).Append(")</h6><ol class=\"mb-0\">");
            foreach (DataRow m in members.Rows)
            {
                string mMi = Convert.ToString(m["MI"]);
                string line = Convert.ToString(m["LastName"]) + ", " + Convert.ToString(m["FirstName"]) +
                              (string.IsNullOrWhiteSpace(mMi) ? "" : " " + mMi);
                sb.Append("<li>").Append(HttpUtility.HtmlEncode(line)).Append("</li>");
            }
            sb.Append("</ol>");

            litDetails.Text = sb.ToString();
            txtReason.Text = "";
            btnApprove.Visible = !overdue;
            btnToReassign.Visible = !overdue;
            SelectedId = id;
            _reviewOpen = true;
        }

        private static void AppendDt(StringBuilder sb, string label, string value)
        {
            sb.Append("<dt class=\"col-sm-3\">").Append(HttpUtility.HtmlEncode(label)).Append("</dt>");
            sb.Append("<dd class=\"col-sm-9\">").Append(HttpUtility.HtmlEncode(value)).Append("</dd>");
        }

        protected void btnApprove_Click(object sender, EventArgs e)
        {
            if (SelectedId > 0) DoApprove(SelectedId);
        }

        protected void btnToReassign_Click(object sender, EventArgs e)
        {
            if (SelectedId > 0) StartReassign(SelectedId);
        }

        protected void btnDecline_Click(object sender, EventArgs e)
        {
            int id = SelectedId;
            if (id <= 0) return;

            string reason = txtReason.Text.Trim();
            if (reason == "")
            {
                ShowMessage(lblMessage, "Please enter a reason before declining.", true);
                return;   // the panel stays open
            }
            if (reason.Length > 250)
            {
                ShowMessage(lblMessage, "The reason must be 250 characters or fewer.", true);
                return;
            }

            try
            {
                Guades_QCUCram_ReservationService.ChangeStatus(
                    id,
                    Guades_QCUCram_Status.Declined,
                    Guades_QCUCram_ReservationService.ActorAdmin,
                    CurrentUserId,
                    reason);

                ShowMessage(lblMessage, "Reservation #" + id + " was declined. The requester has been notified.", false);
                ClosePanel();
            }
            catch (InvalidOperationException ex)
            {
                ShowMessage(lblMessage, ex.Message, true);
                ClosePanel();
            }
            catch (Exception)
            {
                ShowMessage(lblMessage, "Something went wrong while declining. Please try again.", true);
            }

            BindAll();
        }

        protected void btnClose_Click(object sender, EventArgs e)
        {
            ClosePanel();
        }

        private void ClosePanel()
        {
            _reviewOpen = false;
            SelectedId = 0;
            txtReason.Text = "";
        }

        // ------------------------------------------------------------------ approve
        private void DoApprove(int id)
        {
            var info = Guades_QCUCram_ReservationService.GetInfo(id);
            if (info == null)
            {
                ShowMessage(lblMessage, "Reservation #" + id + " was not found.", true);
            }
            else if (info.Status != Guades_QCUCram_Status.Pending)
            {
                ShowMessage(lblMessage, "Reservation #" + id + " is no longer pending (it is " + info.Status + ").", true);
                ClosePanel();
            }
            else if (info.SlotStart <= Guades_QCUCram_Clock.Now)
            {
                ShowMessage(lblMessage, "Reservation #" + id + " cannot be approved because its slot has already started. Decline it instead.", true);
            }
            else
            {
                try
                {
                    Guades_QCUCram_ReservationService.ChangeStatus(
                        id,
                        Guades_QCUCram_Status.Approved,
                        Guades_QCUCram_ReservationService.ActorAdmin,
                        CurrentUserId,
                        null);

                    ShowMessage(lblMessage, "Reservation #" + id + " was approved. The requester has been notified.", false);
                    ClosePanel();
                }
                catch (InvalidOperationException ex)
                {
                    ShowMessage(lblMessage, ex.Message, true);
                    ClosePanel();
                }
                catch (Exception)
                {
                    ShowMessage(lblMessage, "Something went wrong while approving. Please try again.", true);
                }
            }

            BindAll();
        }

        // ------------------------------------------------------------------ reassign
        private void StartReassign(int id)
        {
            var info = Guades_QCUCram_ReservationService.GetInfo(id);
            if (info == null)
            {
                ShowMessage(lblMessage, "Reservation #" + id + " was not found.", true);
                BindAll();
                return;
            }
            if (!Guades_QCUCram_ReservationService.CanTransition(info.Status, Guades_QCUCram_Status.Reassigned))
            {
                ShowMessage(lblMessage, "Reservation #" + id + " is " + info.Status + " and can't be reassigned.", true);
                BindAll();
                return;
            }
            if (info.SlotStart <= Guades_QCUCram_Clock.Now)
            {
                ShowMessage(lblMessage, "Reservation #" + id + " has already started and can't be reassigned.", true);
                BindAll();
                return;
            }

            ClosePanel();   // hide the review panel if it was open

            int options = FillTargets(info);
            if (options == 0)
            {
                ShowMessage(lblMessage,
                    "No other open room or time slot fits this booking on " +
                    info.Date.ToString("MMM d, yyyy", Inv) + ", so it can't be reassigned.", true);
                CloseReassign();
                return;
            }

            ReassignId = id;
            litReassignInfo.Text =
                "<p class=\"mb-0\">Reservation <strong>#" + id + "</strong> is currently in <strong>" +
                HttpUtility.HtmlEncode(info.Summary) + "</strong> (" + HttpUtility.HtmlEncode(info.Status) + ").</p>";
            txtReassignReason.Text = "";
            _reassignOpen = true;
        }

        // Fills ddlTarget with every OPEN room/slot pair on the booking's date that fits the group.
        // Returns the number of real options (not counting the placeholder item).
        private int FillTargets(Guades_QCUCram_ReservationService.ReservationInfo info)
        {
            ddlTarget.Items.Clear();
            ddlTarget.Items.Add(new ListItem("-- Select new room and time --", ""));

            DataTable rooms = Guades_QCUCram_Db.Query(
                "SELECT RoomID, RoomName, MinCapacity, MaxCapacity FROM Rooms WHERE IsActive = 1 ORDER BY RoomID");
            DataTable slots = Guades_QCUCram_Db.Query(
                "SELECT TimeSlotID, StartTime, EndTime FROM TimeSlots WHERE IsActive = 1 ORDER BY StartTime");
            DataTable held = Guades_QCUCram_Db.Query(
                @"SELECT RoomID, TimeSlotID FROM Reservations
                   WHERE ReservationDate = @d AND Status NOT IN ('Declined','Cancelled','Forfeited')",
                Guades_QCUCram_Db.P("@d", info.Date.Date));
            int members = Convert.ToInt32(Guades_QCUCram_Db.Scalar(
                "SELECT NumberOfMembers FROM Reservations WHERE ReservationID = @id",
                Guades_QCUCram_Db.P("@id", info.ReservationId)));

            var heldSet = new HashSet<string>();
            foreach (DataRow h in held.Rows)
                heldSet.Add(Convert.ToString(h["RoomID"]) + "|" + Convert.ToString(h["TimeSlotID"]));

            DateTime now = Guades_QCUCram_Clock.Now;
            int count = 0;
            foreach (DataRow s in slots.Rows)
            {
                TimeSpan start = (TimeSpan)s["StartTime"];
                if (info.Date.Date.Add(start) <= now) continue;   // slot already started

                string slotLabel = Guades_QCUCram_ReservationService.FormatSlot(start, (TimeSpan)s["EndTime"]);
                foreach (DataRow r in rooms.Rows)
                {
                    int min = Convert.ToInt32(r["MinCapacity"]);
                    int max = Convert.ToInt32(r["MaxCapacity"]);
                    if (members < min || members > max) continue;   // group doesn't fit the room

                    string key = Convert.ToString(r["RoomID"]) + "|" + Convert.ToString(s["TimeSlotID"]);
                    if (heldSet.Contains(key)) continue;            // taken/pending (includes this booking's own slot)

                    ddlTarget.Items.Add(new ListItem(slotLabel + "  |  " + Convert.ToString(r["RoomName"]), key));
                    count++;
                }
            }
            return count;
        }

        protected void btnDoReassign_Click(object sender, EventArgs e)
        {
            int id = ReassignId;
            if (id <= 0) return;

            string value = ddlTarget.SelectedValue;
            if (value == "")
            {
                ShowMessage(lblMessage, "Please choose the new room and time.", true);
                return;
            }

            string[] parts = value.Split('|');
            int roomId, slotId;
            if (parts.Length != 2 || !int.TryParse(parts[0], out roomId) || !int.TryParse(parts[1], out slotId))
            {
                ShowMessage(lblMessage, "Please choose the new room and time.", true);
                return;
            }

            string reason = txtReassignReason.Text.Trim();
            if (reason == "")
            {
                ShowMessage(lblMessage, "Please enter a reason for the reassignment.", true);
                return;   // panel stays open
            }

            try
            {
                Guades_QCUCram_ReservationService.Reassign(id, roomId, slotId, reason, CurrentUserId);
                ShowMessage(lblMessage, "Reservation #" + id + " was reassigned. The requester has been notified.", false);
                CloseReassign();
            }
            catch (InvalidOperationException ex)
            {
                ShowMessage(lblMessage, ex.Message, true);

                // The options may be out of date (e.g. someone just took the slot): refresh them
                var info = Guades_QCUCram_ReservationService.GetInfo(id);
                if (info != null &&
                    Guades_QCUCram_ReservationService.CanTransition(info.Status, Guades_QCUCram_Status.Reassigned) &&
                    info.SlotStart > Guades_QCUCram_Clock.Now &&
                    FillTargets(info) > 0)
                {
                    // keep the panel open with fresh options
                }
                else
                {
                    CloseReassign();
                }
            }
            catch (Exception)
            {
                ShowMessage(lblMessage, "Something went wrong while reassigning. Please try again.", true);
            }

            BindAll();
        }

        protected void btnReassignClose_Click(object sender, EventArgs e)
        {
            CloseReassign();
        }

        private void CloseReassign()
        {
            _reassignOpen = false;
            ReassignId = 0;
            txtReassignReason.Text = "";
        }
    }
}