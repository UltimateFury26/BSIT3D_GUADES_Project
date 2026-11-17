using System;
using System.Web;
using System.Web.UI;
using Guades_QCUCram.Helpers;

namespace Guades_QCUCram
{
    public partial class SiteMaster : MasterPage
    {
        // Added to the blue nav bar's class list. "qcu-nav-hidden" hides the bar on Login / Register for guests.
        protected string NavExtraClass = "";
        protected string NavExtraAttr = "";

        // Quick links shown in the footer (depends on who is logged in). Built in Page_Load.
        protected string FooterLinks = "";

        protected void Page_Load(object sender, EventArgs e)
        {
            string role = Session[Guades_QCUCram_SecurePage.SessionRole] as string;
            object idValue = Session[Guades_QCUCram_SecurePage.SessionUserId];
            int userId = (idValue is int) ? (int)idValue : 0;
            string name = Session[Guades_QCUCram_SecurePage.SessionUserName] as string ?? "";

            bool isStudent = (role == Guades_QCUCram_SecurePage.RoleStudent && userId > 0);
            bool isAdmin = (role == Guades_QCUCram_SecurePage.RoleAdmin && userId > 0);

            phGuest.Visible = !isStudent && !isAdmin;
            phStudent.Visible = isStudent;
            phStudentRight.Visible = isStudent;
            phAdmin.Visible = isAdmin;
            phAdminRight.Visible = isAdmin;
            phUser.Visible = isStudent || isAdmin;

            // Login / Register show only the QCU logo header (their Login/Register nav links are redundant).
            // Detect by page class (works with extensionless / friendly URLs), with a file-name fallback.
            string pageName = System.IO.Path.GetFileNameWithoutExtension(Request.Path ?? "");
            bool isAuthPage = Page is global::Guades_QCUCram.Guades_QCUCram_Account.Guades_QCUCram_Login ||
                              Page is global::Guades_QCUCram.Guades_QCUCram_Account.Guades_QCUCram_Register ||
                              string.Equals(pageName, "Guades_QCUCram_Login", StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(pageName, "Guades_QCUCram_Register", StringComparison.OrdinalIgnoreCase);
            if (isAuthPage && !isStudent && !isAdmin)
            {
                phGuest.Visible = false;                 // no Login / Register links at all
                NavExtraClass = " qcu-nav-hidden";       // and no empty blue bar
                NavExtraAttr = " style=\"display:none\"";
            }

            if (isStudent || isAdmin)
            {
                // name + a small role tag, styled by .qcu-user-name / .qcu-user-role
                litUser.Text = "<span class=\"qcu-user-name\">" + HttpUtility.HtmlEncode(name) + "</span>" +
                               " <span class=\"qcu-user-role\">" + HttpUtility.HtmlEncode(role) + "</span>";
            }

            FooterLinks = BuildFooterLinks(isStudent, isAdmin);

            if (isStudent)
            {
                int unread = Convert.ToInt32(Guades_QCUCram_Db.Scalar(
                    "SELECT COUNT(*) FROM Notifications WHERE StudentID = @id AND IsRead = 0",
                    Guades_QCUCram_Db.P("@id", userId)));

                // small orange count on the bell (hidden when there is nothing unread)
                litUnread.Text = (unread > 0)
                    ? "<span class=\"qcu-nav-badge\" aria-label=\"" + unread + " unread\">" + (unread > 99 ? "99+" : unread.ToString()) + "</span>"
                    : "";
            }
        }
        // Footer links: only pages the current visitor can actually open.
        private string BuildFooterLinks(bool isStudent, bool isAdmin)
        {
            var sb = new System.Text.StringBuilder();
            Action<string, string> add = (path, text) =>
                sb.Append("<a href=\"" + HttpUtility.HtmlAttributeEncode(ResolveUrl(path)) + "\">" + HttpUtility.HtmlEncode(text) + "</a>");

            if (isStudent)
            {
                add("~/Guades_QCUCram_Student/Guades_QCUCram_Dashboard.aspx", "Dashboard");
                add("~/Guades_QCUCram_Availability.aspx", "Room Availability");
                add("~/Guades_QCUCram_Student/Guades_QCUCram_Reserve.aspx", "Reserve a Room");
                add("~/Guades_QCUCram_Student/Guades_QCUCram_MyReservations.aspx", "My Reservations");
            }
            else if (isAdmin)
            {
                add("~/Guades_QCUCram_Admin/Guades_QCUCram_Dashboard.aspx", "Dashboard");
                add("~/Guades_QCUCram_Availability.aspx", "Room Availability");
                add("~/Guades_QCUCram_Admin/Guades_QCUCram_Queue.aspx", "Review Queue");
                add("~/Guades_QCUCram_Reports/Guades_QCUCram_DailySchedule.aspx", "Daily Schedule");
            }
            else
            {
                add("~/Guades_QCUCram_Account/Guades_QCUCram_Login.aspx", "Login");
                add("~/Guades_QCUCram_Account/Guades_QCUCram_Register.aspx", "Register");
            }
            return sb.ToString();
        }

        // Address of a CSS / JS file with its last-saved time added (?v=...). When the file changes the
        // address changes too, so the browser can never keep showing an old copy from its cache.
        protected string AssetUrl(string virtualPath)
        {
            string url = ResolveUrl(virtualPath);
            try
            {
                string file = Server.MapPath(virtualPath);
                if (System.IO.File.Exists(file))
                    return url + "?v=" + System.IO.File.GetLastWriteTimeUtc(file).ToString("yyyyMMddHHmmss");
            }
            catch (Exception)
            {
                // never break a page because of a cache stamp
            }
            return url;
        }

        // PreRender (not Load) so the banner is correct right after Settings / Dev Tools change the offset.
        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);

            try
            {
                int offset = Guades_QCUCram_Clock.OffsetMinutes;
                phDevClock.Visible = (offset != 0);
                if (offset != 0)
                {
                    litDevClock.Text = "system time is shifted by " + offset + " minute(s). It is now " +
                        HttpUtility.HtmlEncode(Guades_QCUCram_Clock.Now.ToString("MMM d, yyyy h:mm tt")) +
                        ". Set it back to 0 in Settings or Dev Tools.";
                }
            }
            catch (Exception)
            {
                phDevClock.Visible = false; // never break a page because of the banner
            }
        }
    }
}