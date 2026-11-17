using System;
using Guades_QCUCram.Helpers;

namespace Guades_QCUCram.Guades_QCUCram_Account
{
    // Inherits SecurePage: any logged-in user (Student or Admin) may open it.
    public partial class Guades_QCUCram_ChangePassword : Guades_QCUCram_SecurePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
        }

        protected void btnChange_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            string current = txtCurrent.Text;
            string newPassword = txtNew.Text;
            string confirm = txtConfirm.Text;

            string pwIssue = Guades_QCUCram_PasswordHelper.GetStrengthMessage(newPassword);
            if (pwIssue != null)
            {
                ShowMessage(lblMessage, pwIssue, true);
                return;
            }
            if (newPassword != confirm)
            {
                ShowMessage(lblMessage, "Passwords do not match.", true);
                return;
            }

            bool isStudent = (CurrentRole == RoleStudent);

            try
            {
                string selectSql = isStudent
                    ? "SELECT PasswordHash FROM Students WHERE StudentID = @id"
                    : "SELECT PasswordHash FROM AdminUsers WHERE AdminID = @id";

                object stored = Guades_QCUCram_Db.Scalar(selectSql, Guades_QCUCram_Db.P("@id", CurrentUserId));

                if (stored == null || !Guades_QCUCram_PasswordHelper.Verify(current, stored.ToString()))
                {
                    ShowMessage(lblMessage, "Your current password is incorrect.", true);
                    return;
                }

                if (current == newPassword)
                {
                    ShowMessage(lblMessage, "The new password must be different from the current one.", true);
                    return;
                }

                string updateSql = isStudent
                    ? "UPDATE Students SET PasswordHash = @h WHERE StudentID = @id"
                    : "UPDATE AdminUsers SET PasswordHash = @h WHERE AdminID = @id";

                Guades_QCUCram_Db.Execute(updateSql,
                    Guades_QCUCram_Db.P("@h", Guades_QCUCram_PasswordHelper.Hash(newPassword)),
                    Guades_QCUCram_Db.P("@id", CurrentUserId));
            }
            catch (Exception)
            {
                ShowMessage(lblMessage, "Sorry, the password could not be changed. Please try again.", true);
                return;
            }

            ShowMessage(lblMessage, "Your password has been changed.", false);
        }
    }
}