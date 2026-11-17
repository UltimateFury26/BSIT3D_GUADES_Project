using System;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Guades_QCUCram.Helpers
{
    // Base class for every page that needs a login.
    // Pages that need a specific role inherit Guades_QCUCram_StudentPage or Guades_QCUCram_AdminPage instead.
    public class Guades_QCUCram_SecurePage : Page
    {
        // ---- Session keys (use these constants everywhere, never type the strings by hand) ----
        public const string SessionRole = "Role";          // "Student" or "Admin"
        public const string SessionUserId = "UserID";      // StudentID or AdminID (int)
        public const string SessionUserName = "UserName";  // display name

        public const string RoleStudent = "Student";
        public const string RoleAdmin = "Admin";

        public const string LoginUrl = "~/Guades_QCUCram_Account/Guades_QCUCram_Login.aspx";

        // null = any logged-in user. Student/Admin page classes override this.
        protected virtual string RequiredRole
        {
            get { return null; }
        }

        public string CurrentRole
        {
            get { return Session[SessionRole] as string; }
        }

        public int CurrentUserId
        {
            get
            {
                object v = Session[SessionUserId];
                return (v is int) ? (int)v : 0;
            }
        }

        public string CurrentUserName
        {
            get { return Session[SessionUserName] as string ?? ""; }
        }

        protected override void OnInit(EventArgs e)
        {
            // Stop the browser from showing a cached copy after logout (Back button).
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            Response.Cache.SetExpires(DateTime.UtcNow.AddMinutes(-1));

            if (CurrentRole == null || CurrentUserId <= 0)
            {
                // Not logged in (or session expired)
                Response.Redirect(LoginUrl + "?msg=login", true);
            }
            else if (RequiredRole != null && CurrentRole != RequiredRole)
            {
                // Logged in, but wrong role for this page
                Response.Redirect(LoginUrl + "?msg=role", true);
            }

            base.OnInit(e);
        }

        // Pages that must NOT trigger the automatic sweep (e.g. Dev Tools) override this to false.
        protected virtual bool RunSweepOnLoad
        {
            get { return true; }
        }

        protected override void OnLoad(EventArgs e)
        {
            // Apply the automatic rules (expiries, forfeits...) BEFORE the page reads its data.
            // Throttled to once per 30 seconds; never throws.
            if (RunSweepOnLoad)
                Guades_QCUCram_MaintenanceService.TryRunIfDue(30);

            base.OnLoad(e);
        }

        // ---- Feedback helpers ----
        // Messages now appear as a toast in the upper-right corner (slides in, a bar counts down 5 seconds,
        // then it slides out). See Scripts/qcu-ui.js (qcuToast). Static so that Login/Register (which are
        // NOT secure pages) can call Guades_QCUCram_SecurePage.ShowMessage(...) too.
        //
        // The messages that sit INSIDE an Add / Edit pop-up (label ID contains "Modal") stay inline, so a
        // validation problem is shown right next to the fields while the pop-up stays open.
        public static void ShowMessage(Label label, string text, bool isError)
        {
            if (label == null) return;
            label.Text = HttpUtility.HtmlEncode(text);

            bool inline = label.ID != null && label.ID.IndexOf("Modal", StringComparison.OrdinalIgnoreCase) >= 0;
            if (!inline && QueueToast(label.Page, text, isError))
            {
                label.Visible = false;      // the toast replaces the old in-page banner
                return;
            }

            label.CssClass = isError ? "alert alert-danger d-block" : "alert alert-success d-block";
            label.Visible = true;
        }

        public static void ShowMessage(Panel panel, string text, bool isError)
        {
            if (panel == null) return;
            if (QueueToast(panel.Page, text, isError))
            {
                panel.Visible = false;
                return;
            }

            panel.Controls.Clear();
            panel.Controls.Add(new LiteralControl(HttpUtility.HtmlEncode(text)));
            panel.CssClass = isError ? "alert alert-danger" : "alert alert-success";
            panel.Visible = true;
        }

        // Registers a one-time startup script that queues the toast. It works for normal page loads AND for
        // UpdatePanel refreshes, and runs exactly once per call (a message can never come back by itself).
        private static bool QueueToast(Page page, string text, bool isError)
        {
            if (page == null) return false;
            try
            {
                string script =
                    "(window.qcuQueue=window.qcuQueue||[]).push({m:\"" + HttpUtility.JavaScriptStringEncode(text ?? "") +
                    "\",e:" + (isError ? "true" : "false") + "});" +
                    "if(window.qcuFlush){window.qcuFlush();}";
                string key = "qcuToast_" + Guid.NewGuid().ToString("N");

                if (ScriptManager.GetCurrent(page) != null)
                    ScriptManager.RegisterStartupScript(page, typeof(Page), key, script, true);
                else
                    page.ClientScript.RegisterStartupScript(typeof(Page), key, script, true);
                return true;
            }
            catch (Exception)
            {
                return false;   // fall back to the old in-page banner
            }
        }
    }
}