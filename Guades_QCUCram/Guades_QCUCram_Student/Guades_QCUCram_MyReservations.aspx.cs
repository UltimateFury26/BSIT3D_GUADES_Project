using System;
using System.Data;
using System.Globalization;
using System.Web;
using System.Web.UI.WebControls;
using Guades_QCUCram.Helpers;

namespace Guades_QCUCram.Guades_QCUCram_Student
{
    public partial class Guades_QCUCram_MyReservations : Guades_QCUCram_StudentPage
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) return;

            // Arrived here right after submitting a request (Step 18 redirects with ?created=ID)
            int createdId;
            if (int.TryParse(Request.QueryString["created"], out createdId))
            {
                var info = Guades_QCUCram_ReservationService.GetInfo(createdId);
                if (info != null && info.StudentId == CurrentUserId)
                {
                    ShowMessage(lblMessage,
                        "Your request #" + createdId + " was submitted. You'll be notified once it's reviewed.",
                        false);
                }
            }

            BindGrid();
        }

        // ------------------------------------------------------------------ data
        private void BindGrid()
        {
            string filter = ddlStatus.SelectedValue;

            string sql =
                @"SELECT r.ReservationID, r.ReservationDate, ts.StartTime, ts.EndTime, rm.RoomName,
                         r.NumberOfMembers, r.Purpose, r.Status
                    FROM Reservations r
                    JOIN Rooms rm     ON rm.RoomID = r.RoomID
                    JOIN TimeSlots ts ON ts.TimeSlotID = r.TimeSlotID
                   WHERE r.StudentID = @sid" +
                (filter == "" ? "" : " AND r.Status = @st") + @"
                   ORDER BY r.ReservationDate DESC, ts.StartTime DESC, r.ReservationID DESC";

            DataTable dt = (filter == "")
                ? Guades_QCUCram_Db.Query(sql, Guades_QCUCram_Db.P("@sid", CurrentUserId))
                : Guades_QCUCram_Db.Query(sql, Guades_QCUCram_Db.P("@sid", CurrentUserId),
                                               Guades_QCUCram_Db.P("@st", filter));

            // Computed display columns
            dt.Columns.Add("DateText", typeof(string));
            dt.Columns.Add("TimeText", typeof(string));
            dt.Columns.Add("CanCancel", typeof(bool));

            DateTime now = Guades_QCUCram_Clock.Now;
            foreach (DataRow row in dt.Rows)
            {
                DateTime date = Convert.ToDateTime(row["ReservationDate"]).Date;
                TimeSpan start = (TimeSpan)row["StartTime"];
                TimeSpan end = (TimeSpan)row["EndTime"];
                string status = Convert.ToString(row["Status"]);

                row["DateText"] = date.ToString("ddd, MMM d, yyyy", Inv);
                row["TimeText"] = Guades_QCUCram_ReservationService.FormatSlot(start, end);

                // Cancellable: still pending/approved/reassigned AND the slot has not started yet
                bool cancellableStatus = status == Guades_QCUCram_Status.Pending
                                      || status == Guades_QCUCram_Status.Approved
                                      || status == Guades_QCUCram_Status.Reassigned;
                row["CanCancel"] = cancellableStatus && date.Add(start) > now;
            }

            gvReservations.DataSource = dt;
            gvReservations.DataBind();
        }

        // Coloured status label used by the grid
        protected static string StatusBadge(object statusObj)
        {
            string status = Convert.ToString(statusObj);
            string css;
            switch (status)
            {
                case "Pending": css = "text-bg-warning"; break;
                case "Approved": css = "text-bg-success"; break;
                case "Reassigned": css = "text-bg-info"; break;
                case "CheckedIn": css = "text-bg-primary"; break;
                case "Completed": css = "text-bg-dark"; break;
                case "Declined": css = "text-bg-danger"; break;
                case "Forfeited": css = "text-bg-danger"; break;
                default: css = "text-bg-secondary"; break;   // Cancelled
            }
            return "<span class=\"badge " + css + "\">" + HttpUtility.HtmlEncode(status) + "</span>";
        }

        // ------------------------------------------------------------------ events
        protected void ddlStatus_SelectedIndexChanged(object sender, EventArgs e)
        {
            gvReservations.PageIndex = 0;
            BindGrid();
        }

        protected void gvReservations_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvReservations.PageIndex = e.NewPageIndex;
            BindGrid();
        }

        protected void gvReservations_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "CancelRes") return;

            int id;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out id)) return;

            // Never trust the button: re-check ownership and timing on the server
            var info = Guades_QCUCram_ReservationService.GetInfo(id);
            if (info == null || info.StudentId != CurrentUserId)
            {
                ShowMessage(lblMessage, "That reservation could not be found.", true);
                BindGrid();
                return;
            }
            if (info.SlotStart <= Guades_QCUCram_Clock.Now)
            {
                ShowMessage(lblMessage, "Reservation #" + id + " has already started and can no longer be cancelled.", true);
                BindGrid();
                return;
            }

            try
            {
                Guades_QCUCram_ReservationService.ChangeStatus(
                    id,
                    Guades_QCUCram_Status.Cancelled,
                    Guades_QCUCram_ReservationService.ActorStudent,
                    CurrentUserId,
                    "Cancelled by the requester");

                ShowMessage(lblMessage,
                    "Reservation #" + id + " was cancelled. The slot is open again.", false);
            }
            catch (InvalidOperationException ex)
            {
                // Friendly messages from the service (e.g. already cancelled / not allowed)
                ShowMessage(lblMessage, ex.Message, true);
            }
            catch (Exception)
            {
                ShowMessage(lblMessage, "Something went wrong while cancelling. Please try again.", true);
            }

            BindGrid();
        }
    }
}