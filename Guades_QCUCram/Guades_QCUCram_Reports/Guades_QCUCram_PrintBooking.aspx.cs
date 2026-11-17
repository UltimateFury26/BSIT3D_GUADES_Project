using System;
using System.Data;
using System.Globalization;
using System.Text;
using System.Web;
using Guades_QCUCram.Helpers;

namespace Guades_QCUCram.Guades_QCUCram_Reports
{
    public partial class Guades_QCUCram_PrintBooking : Guades_QCUCram_SecurePage
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        protected void Page_Load(object sender, EventArgs e)
        {
            bool isAdmin = (CurrentRole == RoleAdmin);

            // "Back" goes to the list the person came from
            hlBack.NavigateUrl = isAdmin
                ? "~/Guades_QCUCram_Admin/Guades_QCUCram_Records.aspx"
                : "~/Guades_QCUCram_Student/Guades_QCUCram_MyReservations.aspx";

            try
            {
                int id;
                if (!int.TryParse(Request.QueryString["id"], NumberStyles.None, Inv, out id) || id <= 0)
                {
                    NotFound();
                    return;
                }

                // Admin: any reservation. Student: only their own.
                // (A student asking for someone else's number gets the same "not found" message.)
                string sql =
                    @"SELECT r.ReservationID, r.ReservationDate, r.Purpose, r.NumberOfMembers, r.Status,
                             r.CreatedAt, r.CheckedInAt,
                             rm.RoomName, ts.StartTime, ts.EndTime,
                             s.StudentNumber, s.LastName, s.FirstName, s.MI, s.Email, s.Program, s.RequesterType
                        FROM Reservations r
                        JOIN Students s   ON s.StudentID = r.StudentID
                        JOIN Rooms rm     ON rm.RoomID = r.RoomID
                        JOIN TimeSlots ts ON ts.TimeSlotID = r.TimeSlotID
                       WHERE r.ReservationID = @id" + (isAdmin ? "" : " AND r.StudentID = @sid");

                DataTable res = isAdmin
                    ? Guades_QCUCram_Db.Query(sql, Guades_QCUCram_Db.P("@id", id))
                    : Guades_QCUCram_Db.Query(sql, Guades_QCUCram_Db.P("@id", id),
                                                   Guades_QCUCram_Db.P("@sid", CurrentUserId));

                if (res.Rows.Count == 0)
                {
                    NotFound();
                    return;
                }

                DataTable members = Guades_QCUCram_Db.Query(
                    @"SELECT LastName, FirstName, MI FROM ReservationMembers
                       WHERE ReservationID = @id ORDER BY MemberID",
                    Guades_QCUCram_Db.P("@id", id));

                DataTable history = Guades_QCUCram_Db.Query(
                    @"SELECT h.OldStatus, h.NewStatus, h.ChangedAt, h.Notes,
                             CASE h.ChangedByRole
                                  WHEN 'Admin'   THEN ISNULL(a.FullName, 'Library Assistant')
                                  WHEN 'Student' THEN 'Requester'
                                  ELSE 'System' END AS ChangedByText
                        FROM ReservationStatusHistory h
                        LEFT JOIN AdminUsers a ON a.AdminID = h.ChangedBy AND h.ChangedByRole = 'Admin'
                       WHERE h.ReservationID = @id
                       ORDER BY h.ChangedAt, h.HistoryID",
                    Guades_QCUCram_Db.P("@id", id));

                litReport.Text = BuildReport(res.Rows[0], members, history);
            }
            catch (Exception)
            {
                pnlActions.Visible = true;
                litReport.Text = "";
                ShowMessage(lblMessage, "Sorry, the report could not be loaded. Please try again.", true);
            }
        }

        private void NotFound()
        {
            pnlActions.Visible = true;     // keep the Back button
            litReport.Text = "";
            ShowMessage(lblMessage, "That reservation could not be found.", true);
        }

        // ------------------------------------------------------------------ report HTML
        private string BuildReport(DataRow r, DataTable members, DataTable history)
        {
            int id = Convert.ToInt32(r["ReservationID"]);
            string status = Convert.ToString(r["Status"]);
            DateTime date = Convert.ToDateTime(r["ReservationDate"]).Date;
            string slot = Guades_QCUCram_ReservationService.FormatSlot((TimeSpan)r["StartTime"], (TimeSpan)r["EndTime"]);

            var sb = new StringBuilder();
            sb.Append("<div class=\"report\">");

            // ---- Header
            sb.Append("<div class=\"report-header\">");
            sb.Append("<img class=\"report-seal\" alt=\"\" src=\"").Append(ResolveUrl("~/Content/images/qcu-logo.png")).Append("\" />");
            sb.Append("<h3>Quezon City University</h3>");
            sb.Append("<div class=\"sub\">QCU Library &mdash; Collaboration Room Reservation</div>");
            sb.Append("<div><strong>Reservation #").Append(id).Append("</strong> &nbsp;|&nbsp; Status: <strong>")
              .Append(HttpUtility.HtmlEncode(StatusLabel(status))).Append("</strong></div>");
            sb.Append("</div>");

            // ---- Requester
            sb.Append("<h5>Requester</h5><table class=\"kv\">");
            KV(sb, "Name", PersonName(r["LastName"], r["FirstName"], r["MI"]));
            KV(sb, "ID / Student Number", Convert.ToString(r["StudentNumber"]));
            KV(sb, "Requester type", Convert.ToString(r["RequesterType"]));
            KV(sb, "Program", Convert.ToString(r["Program"]));
            KV(sb, "Email", Convert.ToString(r["Email"]));
            sb.Append("</table>");

            // ---- Booking
            sb.Append("<h5>Reservation Details</h5><table class=\"kv\">");
            KV(sb, "Room", Convert.ToString(r["RoomName"]));
            KV(sb, "Date", date.ToString("dddd, MMMM d, yyyy", Inv));
            KV(sb, "Time", slot);
            KV(sb, "Number of members", Convert.ToString(r["NumberOfMembers"]));
            KV(sb, "Purpose", Convert.ToString(r["Purpose"]));
            KV(sb, "Requested on", Convert.ToDateTime(r["CreatedAt"]).ToString("MMM d, yyyy h:mm tt", Inv));
            KV(sb, "Checked in at", r["CheckedInAt"] == DBNull.Value
                ? "\u2014"
                : Convert.ToDateTime(r["CheckedInAt"]).ToString("MMM d, yyyy h:mm tt", Inv));
            sb.Append("</table>");

            // ---- Members
            sb.Append("<h5>Group Members</h5>");
            if (members.Rows.Count == 0)
            {
                sb.Append("<p class=\"text-muted\">No member names on file.</p>");
            }
            else
            {
                sb.Append("<table class=\"grid\"><thead><tr><th style=\"width:50px\">No.</th><th>Name (Last, First MI)</th></tr></thead><tbody>");
                int n = 1;
                foreach (DataRow m in members.Rows)
                {
                    sb.Append("<tr><td>").Append(n++).Append("</td><td>")
                      .Append(HttpUtility.HtmlEncode(PersonName(m["LastName"], m["FirstName"], m["MI"])))
                      .Append("</td></tr>");
                }
                sb.Append("</tbody></table>");
            }

            // ---- Status history
            sb.Append("<h5>Status History</h5>");
            if (history.Rows.Count == 0)
            {
                sb.Append("<p class=\"text-muted\">No status changes recorded.</p>");
            }
            else
            {
                sb.Append("<table class=\"grid\"><thead><tr><th>When</th><th>Change</th><th>By</th><th>Notes</th></tr></thead><tbody>");
                foreach (DataRow h in history.Rows)
                {
                    string from = h["OldStatus"] == DBNull.Value ? "New" : StatusLabel(Convert.ToString(h["OldStatus"]));
                    sb.Append("<tr><td>")
                      .Append(HttpUtility.HtmlEncode(Convert.ToDateTime(h["ChangedAt"]).ToString("MMM d, yyyy h:mm tt", Inv)))
                      .Append("</td><td>")
                      .Append(HttpUtility.HtmlEncode(from + " \u2192 " + StatusLabel(Convert.ToString(h["NewStatus"]))))
                      .Append("</td><td>").Append(HttpUtility.HtmlEncode(Convert.ToString(h["ChangedByText"])))
                      .Append("</td><td>").Append(HttpUtility.HtmlEncode(h["Notes"] == DBNull.Value ? "" : Convert.ToString(h["Notes"])))
                      .Append("</td></tr>");
                }
                sb.Append("</tbody></table>");
            }

            // ---- Signatures
            sb.Append("<div class=\"sign-row\">");
            sb.Append("<div class=\"sign-box\"><div class=\"sign-line\">Library Assistant</div></div>");
            sb.Append("<div class=\"sign-box\"><div class=\"sign-line\">Group Representative</div></div>");
            sb.Append("</div>");
            sb.Append("<p class=\"mt-3\">QCID verified &#9744;</p>");

            sb.Append("<p class=\"text-muted small mb-0\">Printed on ")
              .Append(HttpUtility.HtmlEncode(Guades_QCUCram_Clock.Now.ToString("MMM d, yyyy h:mm tt", Inv)))
              .Append("</p>");

            sb.Append("</div>");
            return sb.ToString();
        }

        private static void KV(StringBuilder sb, string key, string value)
        {
            sb.Append("<tr><td class=\"k\">").Append(HttpUtility.HtmlEncode(key))
              .Append("</td><td>").Append(HttpUtility.HtmlEncode(value)).Append("</td></tr>");
        }

        private static string PersonName(object last, object first, object mi)
        {
            string m = (mi == null || mi == DBNull.Value) ? "" : Convert.ToString(mi).Trim().TrimEnd('.');
            return Convert.ToString(last) + ", " + Convert.ToString(first) + (m.Length == 0 ? "" : " " + m + ".");
        }

        private static string StatusLabel(string status)
        {
            return status == Guades_QCUCram_Status.CheckedIn ? "Checked In" : status;
        }
    }
}