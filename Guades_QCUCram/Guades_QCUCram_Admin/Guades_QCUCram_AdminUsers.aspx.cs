using System;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI.WebControls;
using Guades_QCUCram.Helpers;

namespace Guades_QCUCram.Guades_QCUCram_Admin
{
    public partial class Guades_QCUCram_AdminUsers : Guades_QCUCram_AdminPage
    {
        private static readonly Regex UsernameRegex = new Regex(@"^[A-Za-z0-9._\-]{3,50}$");

        // True only during the request that wants the pop-up showing (Add, Edit, or a failed save).
        // It is a plain field on purpose (not ViewState), so the pop-up is closed on every other refresh.
        private bool _modalOpen;

        protected string ModalOpenCss
        {
            get { return _modalOpen ? " is-open" : ""; }
        }

        // The temporary password shown in the Add pop-up, e.g. "admin6" when 5 admin accounts already exist.
        protected string TempPasswordPreview = "";

        // Number the next admin account will get = how many accounts exist now + 1.
        private static int NextAdminNumber()
        {
            return Convert.ToInt32(Guades_QCUCram_Db.Scalar("SELECT COUNT(*) FROM AdminUsers")) + 1;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                BindGrid();
            }
        }

        // ---------------- Display helpers ----------------
        protected bool IsSelf(object adminId)
        {
            return Convert.ToInt32(adminId) == CurrentUserId;
        }

        protected string FormatUsername(object username, object adminId)
        {
            string text = HttpUtility.HtmlEncode(Convert.ToString(username));
            return IsSelf(adminId) ? text + " <span class=\"text-muted\">(you)</span>" : text;
        }

        // ---------------- Grid ----------------
        private void BindGrid()
        {
            DataTable dt = Guades_QCUCram_Db.Query(
                "SELECT AdminID, Username, FullName, IsActive FROM AdminUsers ORDER BY Username");
            gvAdmins.DataSource = dt;
            gvAdmins.DataBind();
        }

        protected void gvAdmins_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            int adminId;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out adminId)) return;

            try
            {
                switch (e.CommandName)
                {
                    case "EditAdmin":
                        LoadForEdit(adminId);
                        break;
                    case "ToggleAdmin":
                        ToggleAdmin(adminId);
                        break;
                    case "ResetPassword":
                        ResetPassword(adminId);
                        break;
                }
            }
            catch (Exception)
            {
                ShowMessage(lblMessage, "Sorry, that action could not be completed. Please try again.", true);
            }
        }

        // ---------------- Pop-up: helpers ----------------
        // A problem found while saving keeps the pop-up open and is shown inside it.
        private void ModalError(string message)
        {
            ShowMessage(lblModalMessage, message, true);
            _modalOpen = true;
            if (pnlPassword.Visible)    // Add mode: keep showing the temporary password
                TempPasswordPreview = HttpUtility.HtmlEncode(Guades_QCUCram_PasswordHelper.AdminTemporaryPassword(NextAdminNumber()));
        }

        private void CloseModal()
        {
            _modalOpen = false;
            hfAdminId.Value = "";
            lblModalMessage.Visible = false;
        }

        // ---------------- Pop-up: open for ADD ----------------
        protected void btnAddNew_Click(object sender, EventArgs e)
        {
            lblMessage.Visible = false;
            lblModalMessage.Visible = false;

            hfAdminId.Value = "";
            txtUsername.Text = "";
            txtFullName.Text = "";
            pnlUsername.Visible = true;
            pnlPassword.Visible = true;
            TempPasswordPreview = HttpUtility.HtmlEncode(Guades_QCUCram_PasswordHelper.AdminTemporaryPassword(NextAdminNumber()));

            lblFormTitle.Text = "Add Admin Account";
            btnSave.Text = "Add Admin";
            _modalOpen = true;
        }

        // ---------------- Pop-up: open for EDIT (full name only) ----------------
        private void LoadForEdit(int adminId)
        {
            DataTable dt = Guades_QCUCram_Db.Query(
                "SELECT AdminID, Username, FullName FROM AdminUsers WHERE AdminID = @id",
                Guades_QCUCram_Db.P("@id", adminId));
            if (dt.Rows.Count == 0)
            {
                ShowMessage(lblMessage, "That account no longer exists.", true);
                CloseModal();
                BindGrid();
                return;
            }

            lblMessage.Visible = false;
            lblModalMessage.Visible = false;
            hfAdminId.Value = adminId.ToString();
            txtFullName.Text = dt.Rows[0]["FullName"].ToString();
            pnlUsername.Visible = false;       // a username cannot be changed
            pnlPassword.Visible = false;       // passwords are changed with Reset Password

            // Label.Text is not encoded automatically, so encode the username here.
            lblFormTitle.Text = "Edit Admin: " + HttpUtility.HtmlEncode(dt.Rows[0]["Username"].ToString());
            btnSave.Text = "Update Admin";
            _modalOpen = true;
        }

        // ---------------- Pop-up: save (Add or Edit) ----------------
        protected void btnSave_Click(object sender, EventArgs e)
        {
            lblModalMessage.Visible = false;

            if (!Page.IsValid)
            {
                _modalOpen = true;      // the validators show their own messages
                if (pnlPassword.Visible)
                    TempPasswordPreview = HttpUtility.HtmlEncode(Guades_QCUCram_PasswordHelper.AdminTemporaryPassword(NextAdminNumber()));
                return;
            }

            int id;
            if (int.TryParse(hfAdminId.Value, out id) && id > 0)
                SaveEdit(id);
            else
                SaveNew();
        }

        private void SaveNew()
        {
            string username = txtUsername.Text.Trim();
            string fullName = txtFullName.Text.Trim();

            // Server-side re-checks (browser checks can be bypassed)
            if (!UsernameRegex.IsMatch(username))
            {
                ModalError("Username must be 3-50 letters, numbers, dots, dashes or underscores.");
                return;
            }
            if (fullName.Length == 0 || fullName.Length > 120)
            {
                ModalError("Full name is required (maximum 120 characters).");
                return;
            }

            string password = null;     // set below, once we know how many admin accounts exist
            try
            {
                int taken = Convert.ToInt32(Guades_QCUCram_Db.Scalar(
                    "SELECT COUNT(*) FROM AdminUsers WHERE Username = @u",
                    Guades_QCUCram_Db.P("@u", username)));
                if (taken > 0)
                {
                    ModalError("That username is already taken.");
                    return;
                }

                // Temporary password = "admin" + this account's number (5 admins exist -> the new one gets admin6)
                password = Guades_QCUCram_PasswordHelper.AdminTemporaryPassword(NextAdminNumber());

                Guades_QCUCram_Db.Execute(
                    @"INSERT INTO AdminUsers (Username, PasswordHash, FullName, IsActive)
                      VALUES (@u, @h, @n, 1)",
                    Guades_QCUCram_Db.P("@u", username),
                    Guades_QCUCram_Db.P("@h", Guades_QCUCram_PasswordHelper.Hash(password)),
                    Guades_QCUCram_Db.P("@n", fullName));
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2601 || ex.Number == 2627)
                    ModalError("That username is already taken.");
                else
                    ModalError("Sorry, the account could not be saved. Please try again.");
                return;
            }

            CloseModal();
            ShowMessage(lblMessage, "Admin account '" + username + "' added. Temporary password: " + password + ".", false);
            BindGrid();
        }

        private void SaveEdit(int id)
        {
            string fullName = txtFullName.Text.Trim();
            if (fullName.Length == 0 || fullName.Length > 120)
            {
                ModalError("Full name is required (maximum 120 characters).");
                return;
            }

            try
            {
                int rows = Guades_QCUCram_Db.Execute(
                    "UPDATE AdminUsers SET FullName = @n WHERE AdminID = @id",
                    Guades_QCUCram_Db.P("@n", fullName),
                    Guades_QCUCram_Db.P("@id", id));

                if (rows == 0)
                {
                    CloseModal();
                    ShowMessage(lblMessage, "That account no longer exists.", true);
                    BindGrid();
                    return;
                }
            }
            catch (SqlException)
            {
                ModalError("Sorry, the account could not be saved. Please try again.");
                return;
            }

            // If the admin renamed themselves, refresh the name shown in the menu.
            if (id == CurrentUserId) Session[SessionUserName] = fullName;

            CloseModal();
            ShowMessage(lblMessage, "Admin account updated.", false);
            BindGrid();
        }

        // ---------------- Disable / Enable ----------------
        private void ToggleAdmin(int adminId)
        {
            DataTable dt = Guades_QCUCram_Db.Query(
                "SELECT Username, IsActive FROM AdminUsers WHERE AdminID = @id",
                Guades_QCUCram_Db.P("@id", adminId));
            if (dt.Rows.Count == 0)
            {
                ShowMessage(lblMessage, "That account no longer exists.", true);
                BindGrid();
                return;
            }

            string username = dt.Rows[0]["Username"].ToString();
            bool isActive = Convert.ToBoolean(dt.Rows[0]["IsActive"]);

            if (!isActive)
            {
                // Enable
                Guades_QCUCram_Db.Execute(
                    "UPDATE AdminUsers SET IsActive = 1 WHERE AdminID = @id",
                    Guades_QCUCram_Db.P("@id", adminId));
                ShowMessage(lblMessage, "Account '" + username + "' enabled.", false);
                BindGrid();
                return;
            }

            // Disable: rule 1 - cannot disable yourself
            if (adminId == CurrentUserId)
            {
                ShowMessage(lblMessage, "You cannot disable your own account.", true);
                return;
            }

            // Rule 2 - at least one active admin must remain.
            // Checked and updated in one locked transaction so two admins can't race each other.
            Guades_QCUCram_Db.Execute(
                @"SET XACT_ABORT ON;
                  BEGIN TRAN;
                  DECLARE @n INT = (SELECT COUNT(*) FROM AdminUsers WITH (UPDLOCK, HOLDLOCK) WHERE IsActive = 1);
                  UPDATE AdminUsers SET IsActive = 0
                   WHERE AdminID = @id AND IsActive = 1 AND AdminID <> @me AND @n > 1;
                  COMMIT;",
                Guades_QCUCram_Db.P("@id", adminId),
                Guades_QCUCram_Db.P("@me", CurrentUserId));

            bool stillActive = Convert.ToBoolean(Guades_QCUCram_Db.Scalar(
                "SELECT IsActive FROM AdminUsers WHERE AdminID = @id",
                Guades_QCUCram_Db.P("@id", adminId)));

            if (stillActive)
            {
                ShowMessage(lblMessage, "At least one active admin account must remain, so '" + username + "' was not disabled.", true);
                return;
            }

            ShowMessage(lblMessage, "Account '" + username + "' disabled. They can no longer log in.", false);
            BindGrid();
        }

        // ---------------- Reset password ----------------
        private void ResetPassword(int adminId)
        {
            if (adminId == CurrentUserId)
            {
                ShowMessage(lblMessage, "To change your own password, use Change Password.", true);
                return;
            }

            DataTable dt = Guades_QCUCram_Db.Query(
                "SELECT Username FROM AdminUsers WHERE AdminID = @id", Guades_QCUCram_Db.P("@id", adminId));
            if (dt.Rows.Count == 0)
            {
                ShowMessage(lblMessage, "That account no longer exists.", true);
                BindGrid();
                return;
            }

            // admin + this account's place in the list (oldest account = admin1, next = admin2 ...)
            int number = Convert.ToInt32(Guades_QCUCram_Db.Scalar(
                "SELECT COUNT(*) FROM AdminUsers WHERE AdminID <= @id", Guades_QCUCram_Db.P("@id", adminId)));
            string temp = Guades_QCUCram_PasswordHelper.AdminTemporaryPassword(number);
            Guades_QCUCram_Db.Execute(
                "UPDATE AdminUsers SET PasswordHash = @h WHERE AdminID = @id",
                Guades_QCUCram_Db.P("@h", Guades_QCUCram_PasswordHelper.Hash(temp)),
                Guades_QCUCram_Db.P("@id", adminId));

            ShowMessage(lblMessage,
                "Password reset for " + dt.Rows[0]["Username"] + ". Temporary password: " + temp + ".",
                false);
        }
    }
}