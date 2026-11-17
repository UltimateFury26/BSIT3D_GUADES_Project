using System;
using System.Data;
using System.Globalization;
using System.Web.UI.WebControls;
using Guades_QCUCram.Helpers;

namespace Guades_QCUCram.Guades_QCUCram_Student
{
    public partial class Guades_QCUCram_Notifications : Guades_QCUCram_StudentPage
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack) BindGrid();
        }

        // ------------------------------------------------------------------ data
        private void BindGrid()
        {
            bool unreadOnly = ddlShow.SelectedValue == "unread";

            DataTable dt = Guades_QCUCram_Db.Query(
                @"SELECT NotificationID, ReservationID, Message, IsRead, CreatedAt
                    FROM Notifications
                   WHERE StudentID = @sid" + (unreadOnly ? " AND IsRead = 0" : "") + @"
                   ORDER BY CreatedAt DESC, NotificationID DESC",
                Guades_QCUCram_Db.P("@sid", CurrentUserId));

            dt.Columns.Add("WhenText", typeof(string));
            foreach (DataRow row in dt.Rows)
                row["WhenText"] = Convert.ToDateTime(row["CreatedAt"]).ToString("MMM d, yyyy h:mm tt", Inv);

            gvNotifications.DataSource = dt;
            gvNotifications.DataBind();

            ShowSummary();
        }

        private int UnreadCount()
        {
            return Convert.ToInt32(Guades_QCUCram_Db.Scalar(
                "SELECT COUNT(*) FROM Notifications WHERE StudentID = @sid AND IsRead = 0",
                Guades_QCUCram_Db.P("@sid", CurrentUserId)));
        }

        private void ShowSummary()
        {
            int unread = UnreadCount();
            int total = Convert.ToInt32(Guades_QCUCram_Db.Scalar(
                "SELECT COUNT(*) FROM Notifications WHERE StudentID = @sid",
                Guades_QCUCram_Db.P("@sid", CurrentUserId)));

            litSummary.Text = total == 0
                ? ""
                : "You have <strong>" + unread + "</strong> unread notification(s) out of " + total + " total.";
        }

        // The badge in the navbar was drawn before the button was handled,
        // so refresh it here to keep it in sync without a page reload.
        private void RefreshNavbarBadge()
        {
            var lit = Master.FindControl("litUnread") as Literal;
            if (lit == null) return;

            int unread = UnreadCount();
            lit.Text = unread > 0 ? " (" + unread + ")" : "";
        }

        // ------------------------------------------------------------------ events
        protected void gvNotifications_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;

            var drv = (DataRowView)e.Row.DataItem;
            if (!Convert.ToBoolean(drv["IsRead"]))
                e.Row.CssClass = "fw-bold";   // unread rows stand out
        }

        protected void ddlShow_SelectedIndexChanged(object sender, EventArgs e)
        {
            gvNotifications.PageIndex = 0;
            BindGrid();
        }

        protected void gvNotifications_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvNotifications.PageIndex = e.NewPageIndex;
            BindGrid();
        }

        protected void gvNotifications_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "MarkRead") return;

            int id;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out id)) return;

            // The StudentID condition means nobody can mark someone else's notification
            Guades_QCUCram_Db.Execute(
                "UPDATE Notifications SET IsRead = 1 WHERE NotificationID = @id AND StudentID = @sid",
                Guades_QCUCram_Db.P("@id", id),
                Guades_QCUCram_Db.P("@sid", CurrentUserId));

            BindGrid();
            RefreshNavbarBadge();
        }

        protected void btnMarkAll_Click(object sender, EventArgs e)
        {
            int changed = Guades_QCUCram_Db.Execute(
                "UPDATE Notifications SET IsRead = 1 WHERE StudentID = @sid AND IsRead = 0",
                Guades_QCUCram_Db.P("@sid", CurrentUserId));

            ShowMessage(lblMessage,
                changed == 0 ? "You have no unread notifications."
                             : changed + " notification(s) marked as read.",
                false);

            BindGrid();
            RefreshNavbarBadge();
        }
    }
}