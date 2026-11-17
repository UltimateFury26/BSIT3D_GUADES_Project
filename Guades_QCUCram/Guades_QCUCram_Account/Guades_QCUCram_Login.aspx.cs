using System;
using System.Collections.Generic;
using System.Data;
using Guades_QCUCram.Helpers;

namespace Guades_QCUCram.Guades_QCUCram_Account
{
    public partial class Guades_QCUCram_Login : System.Web.UI.Page
    {
        private const string InvalidCredentials = "Invalid credentials.";

        // First-field label; set from the selected role so a failed login (postback) shows the right one.
        protected string IdLabel = "Student Number / ID";

        private void ApplyRoleLabels()
        {
            bool admin = (rblRole.SelectedValue == "Admin");
            IdLabel = admin ? "Username" : "Student Number / ID";
            txtId.Attributes["placeholder"] = admin ? "username" : "23-0001";
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            ApplyRoleLabels();
            if (IsPostBack) return;

            string msg = Request.QueryString["msg"];
            bool loggedIn = Session[Guades_QCUCram_SecurePage.SessionRole] != null &&
                            Session[Guades_QCUCram_SecurePage.SessionUserId] is int;

            // Already logged in and just opened the login page -> go to your dashboard.
            // (If we were sent here with ?msg=role, stay and explain instead.)
            if (loggedIn && string.IsNullOrEmpty(msg))
            {
                Response.Redirect("~/Guades_QCUCram_Default.aspx");
                return;
            }

            switch (msg)
            {
                case "login":
                    Guades_QCUCram_SecurePage.ShowMessage(lblMessage, "Please log in to continue.", true);
                    break;
                case "role":
                    Guades_QCUCram_SecurePage.ShowMessage(lblMessage,
                        "You do not have access to that page. Log in with the correct account type.", true);
                    break;
                case "registered":
                    Guades_QCUCram_SecurePage.ShowMessage(lblMessage,
                        "Registration successful. You can now log in.", false);
                    break;
                case "loggedout":
                    Guades_QCUCram_SecurePage.ShowMessage(lblMessage, "You have been logged out.", false);
                    break;
                case "pwchanged":
                    Guades_QCUCram_SecurePage.ShowMessage(lblMessage,
                        "Password changed. Please log in with your new password.", false);
                    break;
            }
        }

        protected void btnLogin_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            string id = txtId.Text.Trim();
            string password = txtPassword.Text;

            bool ok = false;
            if (rblRole.SelectedValue == "Admin")
            {
                ok = TryAdminLogin(id, password);
            }
            else
            {
                // Stored student numbers contain the dash (23-0001). Try what was typed, the dashed form,
                // and the undashed form (a faculty ID that is all digits is stored exactly as entered).
                foreach (string candidate in StudentNumberCandidates(id))
                {
                    if (TryStudentLogin(candidate, password)) { ok = true; break; }
                }
            }

            if (!ok)
            {
                // Same message for wrong ID and wrong password.
                Guades_QCUCram_SecurePage.ShowMessage(lblMessage, InvalidCredentials, true);
            }
        }

        private static IEnumerable<string> StudentNumberCandidates(string typed)
        {
            var list = new List<string>();
            string[] options =
            {
                typed,
                Guades_QCUCram_Programs.NormalizeStudentNumber(typed),
                typed.Replace("-", "")
            };
            foreach (string o in options)
                if (o.Length > 0 && !list.Contains(o)) list.Add(o);
            return list;
        }

        // ---------------- Student / Faculty / Visitor ----------------
        private bool TryStudentLogin(string studentNumber, string password)
        {
            DataTable dt;
            try
            {
                dt = Guades_QCUCram_Db.Query(
                    "SELECT StudentID, LastName, FirstName, PasswordHash FROM Students WHERE StudentNumber = @n",
                    Guades_QCUCram_Db.P("@n", studentNumber));
            }
            catch (Exception)
            {
                Guades_QCUCram_SecurePage.ShowMessage(lblMessage,
                    "Sorry, we could not sign you in right now. Please try again.", true);
                return true; // message already shown; don't overwrite with "Invalid credentials"
            }

            if (dt.Rows.Count == 0)
            {
                // Spend the same time as a real check so timing doesn't reveal unknown IDs.
                Guades_QCUCram_PasswordHelper.Verify(password, "AAAAAAAAAAAAAAAAAAAAAA==:AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=");
                return false;
            }

            DataRow row = dt.Rows[0];
            if (!Guades_QCUCram_PasswordHelper.Verify(password, row["PasswordHash"].ToString()))
                return false;

            int studentId = Convert.ToInt32(row["StudentID"]);
            string displayName = row["FirstName"] + " " + row["LastName"];

            // Auto-lift (2.4 rule 4): clear an expired restriction at login.
            TryAutoLift(studentId);

            StartSession(Guades_QCUCram_SecurePage.RoleStudent, studentId, displayName);
            Response.Redirect("~/Guades_QCUCram_Student/Guades_QCUCram_Dashboard.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
            return true;
        }

        private void TryAutoLift(int studentId)
        {
            try
            {
                int lifted = Guades_QCUCram_Db.Execute(
                    @"UPDATE Students
                         SET IsRestricted = 0, NoShowCount = 0, RestrictionLiftDate = NULL
                       WHERE StudentID = @id AND IsRestricted = 1
                         AND RestrictionLiftDate IS NOT NULL AND RestrictionLiftDate <= @now",
                    Guades_QCUCram_Db.P("@id", studentId),
                    Guades_QCUCram_Db.P("@now", Guades_QCUCram_Clock.Now));

                if (lifted > 0)
                {
                    Guades_QCUCram_Db.Execute(
                        "INSERT INTO Notifications (StudentID, ReservationID, Message, CreatedAt) VALUES (@id, NULL, @m, @now)",
                        Guades_QCUCram_Db.P("@id", studentId),
                        Guades_QCUCram_Db.P("@m", "Your booking restriction has ended. You can reserve rooms again."),
                        Guades_QCUCram_Db.P("@now", Guades_QCUCram_Clock.Now));
                }
            }
            catch (Exception)
            {
                // Never block a login because the auto-lift failed; the sweep (Step 24) will retry.
            }
        }

        // ---------------- Library Assistant ----------------
        private bool TryAdminLogin(string username, string password)
        {
            DataTable dt;
            try
            {
                dt = Guades_QCUCram_Db.Query(
                    "SELECT AdminID, FullName, PasswordHash FROM AdminUsers WHERE Username = @u AND IsActive = 1",
                    Guades_QCUCram_Db.P("@u", username));
            }
            catch (Exception)
            {
                Guades_QCUCram_SecurePage.ShowMessage(lblMessage,
                    "Sorry, we could not sign you in right now. Please try again.", true);
                return true;
            }

            if (dt.Rows.Count == 0)
            {
                Guades_QCUCram_PasswordHelper.Verify(password, "AAAAAAAAAAAAAAAAAAAAAA==:AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=");
                return false;
            }

            DataRow row = dt.Rows[0];
            if (!Guades_QCUCram_PasswordHelper.Verify(password, row["PasswordHash"].ToString()))
                return false;

            StartSession(Guades_QCUCram_SecurePage.RoleAdmin,
                         Convert.ToInt32(row["AdminID"]),
                         row["FullName"].ToString());
            Response.Redirect("~/Guades_QCUCram_Admin/Guades_QCUCram_Dashboard.aspx", false);
            Context.ApplicationInstance.CompleteRequest();
            return true;
        }

        // Wipes any previous login (e.g. a student switching to admin) and sets the 3 session keys.
        private void StartSession(string role, int userId, string displayName)
        {
            Session.Clear();
            Session[Guades_QCUCram_SecurePage.SessionRole] = role;
            Session[Guades_QCUCram_SecurePage.SessionUserId] = userId;
            Session[Guades_QCUCram_SecurePage.SessionUserName] = displayName;
        }
    }
}