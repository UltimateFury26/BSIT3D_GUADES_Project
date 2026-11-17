using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Guades_QCUCram.Helpers;

namespace Guades_QCUCram.Guades_QCUCram_Admin
{
    public partial class Guades_QCUCram_Records : Guades_QCUCram_AdminPage
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        // Details pop-up: true while the modal should show (plain field, reset each request)
        private bool _detailOpen;
        protected string ModalOpenCss { get { return _detailOpen ? " is-open" : ""; } }

        private const string BaseSql =
            @"SELECT r.ReservationID, r.ReservationDate, ts.StartTime, ts.EndTime,
                     rm.RoomName, s.StudentNumber, s.Program, s.Email, s.RequesterType,
                     r.NumberOfMembers, r.Purpose, r.Status, r.CreatedAt, r.CheckedInAt,
                     s.LastName + ', ' + s.FirstName +
                       CASE WHEN s.MI IS NULL OR s.MI = '' THEN '' ELSE ' ' + s.MI + '.' END AS Requester
                FROM Reservations r
                JOIN Students s   ON s.StudentID = r.StudentID
                JOIN Rooms rm     ON rm.RoomID = r.RoomID
                JOIN TimeSlots ts ON ts.TimeSlotID = r.TimeSlotID ";

        private static readonly string[] AllStatuses =
        {
            Guades_QCUCram_Status.Pending, Guades_QCUCram_Status.Approved, Guades_QCUCram_Status.Reassigned,
            Guades_QCUCram_Status.Declined, Guades_QCUCram_Status.Cancelled, Guades_QCUCram_Status.CheckedIn,
            Guades_QCUCram_Status.Completed, Guades_QCUCram_Status.Forfeited
        };

        private static readonly string[] SortKeys =
        {
            "ReservationID", "ReservationDate", "StartTime", "RoomName",
            "Requester", "Program", "NumberOfMembers", "Status"
        };

        // Current sort (survives postbacks). Default: newest date first.
        private string SortKey
        {
            get { return (ViewState["SortKey"] as string) ?? "ReservationDate"; }
            set { ViewState["SortKey"] = value; }
        }

        private bool SortAsc
        {
            get { object v = ViewState["SortAsc"]; return v != null && (bool)v; }
            set { ViewState["SortAsc"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                LoadFilterLists();
                ApplyQueryStringFilters();
                BindGrid();
            }
        }

        // Lets other pages (e.g. the Admin Dashboard) open Records pre-filtered, e.g.
        //   Guades_QCUCram_Records.aspx?status=Forfeited&from=2026-10-06&to=2026-10-06
        private void ApplyQueryStringFilters()
        {
            string status = Request.QueryString["status"];
            if (!string.IsNullOrEmpty(status) && ddlStatus.Items.FindByValue(status) != null)
                ddlStatus.SelectedValue = status;

            DateTime d;
            if (TryParseDate(Request.QueryString["from"], out d)) txtFrom.Text = d.ToString("yyyy-MM-dd", Inv);
            if (TryParseDate(Request.QueryString["to"], out d)) txtTo.Text = d.ToString("yyyy-MM-dd", Inv);
        }

        // ---------------------------------------------------------------- filter lists
        private void LoadFilterLists()
        {
            ddlStatus.Items.Clear();
            ddlStatus.Items.Add(new ListItem("All statuses", ""));
            foreach (string s in AllStatuses)
                ddlStatus.Items.Add(new ListItem(s == Guades_QCUCram_Status.CheckedIn ? "Checked In" : s, s));

            ddlRoom.Items.Clear();
            ddlRoom.Items.Add(new ListItem("All rooms", ""));
            DataTable rooms = Guades_QCUCram_Db.Query("SELECT RoomID, RoomName, IsActive FROM Rooms ORDER BY RoomName");
            foreach (DataRow r in rooms.Rows)
            {
                string label = Convert.ToString(r["RoomName"]);
                if (!Convert.ToBoolean(r["IsActive"])) label += " (inactive)";
                ddlRoom.Items.Add(new ListItem(label, Convert.ToString(r["RoomID"], Inv)));
            }

            ddlProgram.Items.Clear();
            ddlProgram.Items.Add(new ListItem("All programs", ""));
            foreach (string p in Guades_QCUCram_Programs.All)
                ddlProgram.Items.Add(new ListItem(p, p));
        }

        // ---------------------------------------------------------------- query + grid
        private static bool TryParseDate(string text, out DateTime value)
        {
            return DateTime.TryParseExact((text ?? "").Trim(), "yyyy-MM-dd", Inv, DateTimeStyles.None, out value);
        }

        private string BuildOrderBy()
        {
            string d = SortAsc ? "ASC" : "DESC";
            const string tail = ", r.ReservationDate DESC, r.ReservationID DESC";

            switch (SortKey)
            {
                case "ReservationID":
                    return "r.ReservationID " + d;
                case "StartTime":
                    return "ts.StartTime " + d + tail;
                case "RoomName":
                    return "rm.RoomName " + d + tail;
                case "Requester":
                    return "s.LastName " + d + ", s.FirstName " + d + tail;
                case "Program":
                    return "s.Program " + d + tail;
                case "NumberOfMembers":
                    return "r.NumberOfMembers " + d + tail;
                case "Status":
                    return "r.Status " + d + tail;
                default: // ReservationDate
                    return "r.ReservationDate " + d + ", ts.StartTime " + d + ", r.ReservationID " + d;
            }
        }

        private void ShowEmptyWithError(string message)
        {
            ShowMessage(lblMessage, message, true);
            gvRecords.DataSource = null;
            gvRecords.DataBind();
            lblCount.Text = "";
        }

        // Reads the filter boxes + the current sort and returns ALL matching rows.
        // Returns null (after showing a message) when a filter value is invalid.
        private DataTable GetFilteredTable(out bool hasFilters)
        {
            hasFilters = false;

            var where = new List<string>();
            var parms = new List<SqlParameter>();

            // ---- date range
            DateTime fromDate = DateTime.MinValue, toDate = DateTime.MinValue;
            bool hasFrom = txtFrom.Text.Trim().Length > 0;
            bool hasTo = txtTo.Text.Trim().Length > 0;

            if ((hasFrom && !TryParseDate(txtFrom.Text, out fromDate)) ||
                (hasTo && !TryParseDate(txtTo.Text, out toDate)))
            {
                ShowEmptyWithError("Enter the dates as year-month-day, for example 2026-10-06.");
                return null;
            }
            if (hasFrom && hasTo && fromDate > toDate)
            {
                ShowEmptyWithError("The From date is after the To date.");
                return null;
            }
            if (hasFrom)
            {
                where.Add("r.ReservationDate >= @from");
                parms.Add(Guades_QCUCram_Db.P("@from", fromDate.Date));
            }
            if (hasTo)
            {
                where.Add("r.ReservationDate <= @to");
                parms.Add(Guades_QCUCram_Db.P("@to", toDate.Date));
            }

            // ---- status / room / program
            if (ddlStatus.SelectedValue.Length > 0)
            {
                where.Add("r.Status = @status");
                parms.Add(Guades_QCUCram_Db.P("@status", ddlStatus.SelectedValue));
            }

            int roomId;
            if (int.TryParse(ddlRoom.SelectedValue, NumberStyles.Integer, Inv, out roomId))
            {
                where.Add("r.RoomID = @room");
                parms.Add(Guades_QCUCram_Db.P("@room", roomId));
            }

            if (ddlProgram.SelectedValue.Length > 0)
            {
                where.Add("s.Program = @program");
                parms.Add(Guades_QCUCram_Db.P("@program", ddlProgram.SelectedValue));
            }

            // ---- student name / number (LIKE wildcards typed by the admin are searched literally)
            string q = txtStudent.Text.Trim();
            if (q.Length > 0)
            {
                string like = "%" + q.Replace("\\", "\\\\").Replace("%", "\\%")
                                     .Replace("_", "\\_").Replace("[", "\\[") + "%";
                where.Add(@"(s.StudentNumber LIKE @q ESCAPE '\'
                          OR s.LastName LIKE @q ESCAPE '\'
                          OR s.FirstName LIKE @q ESCAPE '\'
                          OR (s.FirstName + ' ' + s.LastName) LIKE @q ESCAPE '\'
                          OR (s.LastName + ', ' + s.FirstName) LIKE @q ESCAPE '\')");
                parms.Add(Guades_QCUCram_Db.P("@q", like));
            }

            string sql = BaseSql
                       + (where.Count > 0 ? " WHERE " + string.Join(" AND ", where.ToArray()) : "")
                       + " ORDER BY " + BuildOrderBy();

            DataTable dt = Guades_QCUCram_Db.Query(sql, parms.ToArray());

            dt.Columns.Add("DateText", typeof(string));
            dt.Columns.Add("TimeText", typeof(string));
            foreach (DataRow row in dt.Rows)
            {
                row["DateText"] = Convert.ToDateTime(row["ReservationDate"]).ToString("ddd, MMM d, yyyy", Inv);
                row["TimeText"] = Guades_QCUCram_ReservationService.FormatSlot(
                    (TimeSpan)row["StartTime"], (TimeSpan)row["EndTime"]);
            }

            hasFilters = where.Count > 0;
            return dt;
        }

        private void BindGrid()
        {
            lblMessage.Visible = false;

            bool hasFilters;
            DataTable dt = GetFilteredTable(out hasFilters);
            if (dt == null) return;   // invalid filter - message already shown

            // Stay on a valid page.
            int maxPage = Math.Max(0, (dt.Rows.Count - 1) / gvRecords.PageSize);
            if (gvRecords.PageIndex > maxPage) gvRecords.PageIndex = maxPage;

            gvRecords.DataSource = dt;
            gvRecords.DataBind();

            lblCount.Text = hasFilters
                ? dt.Rows.Count + " reservation(s) match the filters."
                : dt.Rows.Count + " reservation(s) in total.";
        }

        // ---------------------------------------------------------------- filter buttons
        protected void btnFilter_Click(object sender, EventArgs e)
        {
            _detailOpen = false;
            gvRecords.PageIndex = 0;
            BindGrid();
        }

        protected void btnClear_Click(object sender, EventArgs e)
        {
            txtFrom.Text = "";
            txtTo.Text = "";
            txtStudent.Text = "";
            ddlStatus.SelectedIndex = 0;
            ddlRoom.SelectedIndex = 0;
            ddlProgram.SelectedIndex = 0;

            _detailOpen = false;
            gvRecords.PageIndex = 0;
            BindGrid();
        }

        // ---------------------------------------------------------------- CSV export (Step 35)
        // Exports every row that matches the current filters + sort (not just the visible page).
        protected void btnExportCsv_Click(object sender, EventArgs e)
        {
            lblMessage.Visible = false;

            DataTable dt;
            try
            {
                bool hasFilters;
                dt = GetFilteredTable(out hasFilters);
            }
            catch (Exception)
            {
                ShowMessage(lblMessage, "Sorry, the export could not be created. Please try again.", true);
                return;
            }

            if (dt == null) return;   // invalid filter - message already shown

            if (dt.Rows.Count == 0)
            {
                ShowMessage(lblMessage, "There are no reservations to export for these filters.", true);
                return;
            }

            var sb = new StringBuilder();
            sb.Append("Reservation No.,Date,Start Time,End Time,Room,ID / Student No.,Requester,Requester Type,")
              .Append("Program,Email,Members,Purpose,Status,Submitted,Checked In\r\n");

            foreach (DataRow r in dt.Rows)
            {
                DateTime start = DateTime.Today.Add((TimeSpan)r["StartTime"]);
                DateTime end = DateTime.Today.Add((TimeSpan)r["EndTime"]);

                sb.Append(Csv(r["ReservationID"])).Append(',')
                  .Append(Csv(Convert.ToDateTime(r["ReservationDate"]).ToString("yyyy-MM-dd", Inv))).Append(',')
                  .Append(Csv(start.ToString("h:mm tt", Inv))).Append(',')
                  .Append(Csv(end.ToString("h:mm tt", Inv))).Append(',')
                  .Append(Csv(r["RoomName"])).Append(',')
                  .Append(Csv(r["StudentNumber"])).Append(',')
                  .Append(Csv(r["Requester"])).Append(',')
                  .Append(Csv(r["RequesterType"])).Append(',')
                  .Append(Csv(r["Program"])).Append(',')
                  .Append(Csv(r["Email"])).Append(',')
                  .Append(Csv(r["NumberOfMembers"])).Append(',')
                  .Append(Csv(r["Purpose"])).Append(',')
                  .Append(Csv(Convert.ToString(r["Status"]) == Guades_QCUCram_Status.CheckedIn
                                  ? "Checked In" : Convert.ToString(r["Status"]))).Append(',')
                  .Append(Csv(Convert.ToDateTime(r["CreatedAt"]).ToString("yyyy-MM-dd HH:mm", Inv))).Append(',')
                  .Append(Csv(r["CheckedInAt"] == DBNull.Value
                                  ? "" : Convert.ToDateTime(r["CheckedInAt"]).ToString("yyyy-MM-dd HH:mm", Inv)))
                  .Append("\r\n");
            }

            string fileName = "CRAM_Reservations_" + Guades_QCUCram_Clock.Now.ToString("yyyyMMdd_HHmm", Inv) + ".csv";

            Response.Clear();
            Response.ContentType = "text/csv";
            Response.Charset = "utf-8";
            Response.AddHeader("Content-Disposition", "attachment; filename=" + fileName);
            Response.BinaryWrite(Encoding.UTF8.GetPreamble());          // BOM so Excel reads UTF-8 correctly
            Response.BinaryWrite(Encoding.UTF8.GetBytes(sb.ToString()));
            Response.Flush();
            Response.End();
        }

        // Quotes one CSV cell. Also stops Excel from running text that starts with = + - @ as a formula.
        private static string Csv(object value)
        {
            string s = (value == null || value == DBNull.Value) ? "" : Convert.ToString(value, Inv);
            s = s.Replace("\r", " ").Replace("\n", " ");
            if (s.Length > 0 && "=+-@".IndexOf(s[0]) >= 0) s = "'" + s;
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        }

        // ---------------------------------------------------------------- paging / sorting
        protected void gvRecords_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            _detailOpen = false;
            gvRecords.PageIndex = e.NewPageIndex;
            BindGrid();
        }

        protected void gvRecords_Sorting(object sender, GridViewSortEventArgs e)
        {
            string key = e.SortExpression;
            if (Array.IndexOf(SortKeys, key) < 0) return;   // ignore anything unexpected

            if (key == SortKey)
            {
                SortAsc = !SortAsc;
            }
            else
            {
                SortKey = key;
                SortAsc = true;
            }

            _detailOpen = false;
            gvRecords.PageIndex = 0;
            BindGrid();
        }

        // Shows a small arrow on the column the grid is sorted by.
        protected void gvRecords_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.Header) return;

            foreach (TableCell cell in e.Row.Cells)
            {
                if (cell.Controls.Count > 0 && cell.Controls[0] is LinkButton)
                {
                    LinkButton lb = (LinkButton)cell.Controls[0];
                    if (lb.CommandArgument == SortKey)
                        lb.Text += SortAsc ? " \u25B2" : " \u25BC";
                }
            }
        }

        // ---------------------------------------------------------------- display helpers
        protected string StatusBadge(object status)
        {
            string s = Convert.ToString(status);
            string css;

            switch (s)
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

            string label = (s == Guades_QCUCram_Status.CheckedIn) ? "Checked In" : s;
            return "<span class=\"badge " + css + "\">" + HttpUtility.HtmlEncode(label) + "</span>";
        }

        private static string FullName(DataRow r)
        {
            string name = Convert.ToString(r["LastName"]) + ", " + Convert.ToString(r["FirstName"]);
            string mi = Convert.ToString(r["MI"]);
            if (mi.Length > 0) name += " " + mi + ".";
            return name;
        }

        private static void AppendRow(StringBuilder sb, string label, string value)
        {
            sb.Append("<tr><th>").Append(HttpUtility.HtmlEncode(label)).Append("</th><td>")
              .Append(HttpUtility.HtmlEncode(value)).Append("</td></tr>");
        }

        // ---------------------------------------------------------------- details
        protected void gvRecords_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "ViewRes") return;

            int id;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), NumberStyles.Integer, Inv, out id)) return;

            try
            {
                ShowDetail(id);
            }
            catch (Exception)
            {
                ShowMessage(lblMessage, "Sorry, the details could not be loaded. Please try again.", true);
            }
        }

        protected void btnCloseDetail_Click(object sender, EventArgs e)
        {
            _detailOpen = false;
        }

        private void ShowDetail(int id)
        {
            lblMessage.Visible = false;

            DataTable head = Guades_QCUCram_Db.Query(
                @"SELECT r.ReservationID, r.ReservationDate, r.Purpose, r.NumberOfMembers, r.Status,
                         r.CreatedAt, r.CheckedInAt, ts.StartTime, ts.EndTime, rm.RoomName,
                         s.StudentNumber, s.LastName, s.FirstName, s.MI, s.Email, s.Program, s.RequesterType
                    FROM Reservations r
                    JOIN Students s   ON s.StudentID = r.StudentID
                    JOIN Rooms rm     ON rm.RoomID = r.RoomID
                    JOIN TimeSlots ts ON ts.TimeSlotID = r.TimeSlotID
                   WHERE r.ReservationID = @id",
                Guades_QCUCram_Db.P("@id", id));

            if (head.Rows.Count == 0)
            {
                _detailOpen = false;
                ShowMessage(lblMessage, "That reservation no longer exists.", true);
                return;
            }

            DataRow h = head.Rows[0];

            // ---- summary
            var sb = new StringBuilder();
            sb.Append("<table class=\"table table-sm table-bordered w-auto mb-0\">");
            AppendRow(sb, "Requester", FullName(h) + " (" + Convert.ToString(h["RequesterType"]) + ")");
            AppendRow(sb, "ID / Student No.", Convert.ToString(h["StudentNumber"]));
            AppendRow(sb, "Email", Convert.ToString(h["Email"]));
            AppendRow(sb, "Program", Convert.ToString(h["Program"]));
            AppendRow(sb, "Room", Convert.ToString(h["RoomName"]));
            AppendRow(sb, "Date", Convert.ToDateTime(h["ReservationDate"]).ToString("dddd, MMM d, yyyy", Inv));
            AppendRow(sb, "Time", Guades_QCUCram_ReservationService.FormatSlot(
                (TimeSpan)h["StartTime"], (TimeSpan)h["EndTime"]));
            AppendRow(sb, "Members", Convert.ToString(h["NumberOfMembers"], Inv));
            AppendRow(sb, "Purpose", Convert.ToString(h["Purpose"]));
            sb.Append("<tr><th>Status</th><td>").Append(StatusBadge(h["Status"])).Append("</td></tr>");
            AppendRow(sb, "Submitted", Convert.ToDateTime(h["CreatedAt"]).ToString("MMM d, yyyy h:mm tt", Inv));
            AppendRow(sb, "Checked in", h["CheckedInAt"] == DBNull.Value
                ? "-"
                : Convert.ToDateTime(h["CheckedInAt"]).ToString("MMM d, yyyy h:mm tt", Inv));
            sb.Append("</table>");
            litDetail.Text = sb.ToString();

            // ---- members
            gvMembers.DataSource = Guades_QCUCram_Db.Query(
                @"SELECT LastName + ', ' + FirstName +
                         CASE WHEN MI IS NULL OR MI = '' THEN '' ELSE ' ' + MI + '.' END AS MemberName
                    FROM ReservationMembers
                   WHERE ReservationID = @id
                   ORDER BY MemberID",
                Guades_QCUCram_Db.P("@id", id));
            gvMembers.DataBind();

            // ---- status history (in the order the changes actually happened)
            DataTable hist = Guades_QCUCram_Db.Query(
                @"SELECT h.HistoryID, h.OldStatus, h.NewStatus, h.ChangedAt, h.Notes,
                         CASE h.ChangedByRole
                              WHEN 'Admin' THEN 'Admin: ' + ISNULL((SELECT a.FullName FROM AdminUsers a
                                                                     WHERE a.AdminID = h.ChangedBy), '(account removed)')
                              WHEN 'Student' THEN 'Student: ' + ISNULL((SELECT x.LastName + ', ' + x.FirstName FROM Students x
                                                                         WHERE x.StudentID = h.ChangedBy), '(account removed)')
                              ELSE 'System'
                         END AS ActorText
                    FROM ReservationStatusHistory h
                   WHERE h.ReservationID = @id
                   ORDER BY h.HistoryID",
                Guades_QCUCram_Db.P("@id", id));

            hist.Columns.Add("WhenText", typeof(string));
            hist.Columns.Add("ChangeText", typeof(string));
            foreach (DataRow row in hist.Rows)
            {
                row["WhenText"] = Convert.ToDateTime(row["ChangedAt"]).ToString("MMM d, yyyy h:mm:ss tt", Inv);
                string oldStatus = row["OldStatus"] == DBNull.Value ? "(new)" : Convert.ToString(row["OldStatus"]);
                row["ChangeText"] = oldStatus + " \u2192 " + Convert.ToString(row["NewStatus"]);
            }
            gvHistory.DataSource = hist;
            gvHistory.DataBind();

            lblDetailTitle.Text = "Reservation #" + id.ToString(Inv);
            hlPrint.NavigateUrl = "~/Guades_QCUCram_Reports/Guades_QCUCram_PrintBooking.aspx?id=" + id.ToString(Inv);
            hlPrint.Attributes["data-qcu-load-title"] = "Reservation #" + id.ToString(Inv) + " report";
            _detailOpen = true;

        }
    }
}