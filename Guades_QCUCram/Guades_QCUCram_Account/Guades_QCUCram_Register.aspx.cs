using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Web.UI;
using System.Web.UI.WebControls;
using Guades_QCUCram.Helpers;

namespace Guades_QCUCram.Guades_QCUCram_Account
{
    public partial class Guades_QCUCram_Register : System.Web.UI.Page
    {
        private static readonly Regex StudentNumberRegex = new Regex(@"^\d{2}-\d{4}$");
        private static readonly Regex OtherIdRegex = new Regex(@"^[A-Za-z0-9\-]{3,20}$");

        protected void Page_Load(object sender, EventArgs e)
        {
            // Already logged in? No need to register again.
            if (Session[Guades_QCUCram_SecurePage.SessionRole] != null &&
                Session[Guades_QCUCram_SecurePage.SessionUserId] is int)
            {
                Response.Redirect("~/Guades_QCUCram_Default.aspx");
                return;
            }

            if (!IsPostBack)
            {
                BindList(ddlRequesterType, Guades_QCUCram_Programs.RequesterTypes, "-- Select type --");
                BindList(ddlProgram, Guades_QCUCram_Programs.All, "-- Select program --");
            }
        }

        private static void BindList(DropDownList ddl, string[] items, string placeholder)
        {
            ddl.Items.Clear();
            ddl.Items.Add(new ListItem(placeholder, ""));
            foreach (string item in items)
                ddl.Items.Add(new ListItem(item, item));
        }

        protected void btnRegister_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            string type = ddlRequesterType.SelectedValue;
            string number = txtStudentNumber.Text.Trim();
            string last = txtLastName.Text.Trim();
            string first = txtFirstName.Text.Trim();
            string mi = txtMI.Text.Trim().TrimEnd('.').ToUpperInvariant();
            if (mi.Length > 2) mi = mi.Substring(0, 2);
            string email = txtEmail.Text.Trim();
            string program = ddlProgram.SelectedValue;
            string password = txtPassword.Text;
            string confirm = txtConfirm.Text;

            // A student typing 230001 means 23-0001
            if (type == "Student") number = Guades_QCUCram_Programs.NormalizeStudentNumber(number);

            // ---- Server-side re-checks (the browser checks can be bypassed) ----
            var errors = new List<string>();

            if (Array.IndexOf(Guades_QCUCram_Programs.RequesterTypes, type) < 0)
                errors.Add("Please select a valid requester type.");
            if (Array.IndexOf(Guades_QCUCram_Programs.All, program) < 0)
                errors.Add("Please select a valid program.");
            if (type == "Student")
            {
                if (!StudentNumberRegex.IsMatch(number))
                    errors.Add("Student number must look like 23-0001.");
            }
            else if (!OtherIdRegex.IsMatch(number))
            {
                errors.Add("ID number must be 3-20 letters, numbers, or dashes only.");
            }
            if (last.Length == 0 || first.Length == 0)
                errors.Add("Last name and first name are required.");
            if (email.Length == 0 || email.Length > 120)
                errors.Add("Please enter a valid email address.");
            string pwIssue = Guades_QCUCram_PasswordHelper.GetStrengthMessage(password);
            if (pwIssue != null)
                errors.Add(pwIssue);
            if (password != confirm)
                errors.Add("Passwords do not match.");

            if (errors.Count > 0)
            {
                Guades_QCUCram_SecurePage.ShowMessage(lblMessage, string.Join(" ", errors), true);
                ResetButton();
                return;
            }

            try
            {
                // ---- Friendly duplicate checks ----
                int numberTaken = Convert.ToInt32(Guades_QCUCram_Db.Scalar(
                    "SELECT COUNT(*) FROM Students WHERE StudentNumber = @n",
                    Guades_QCUCram_Db.P("@n", number)));
                if (numberTaken > 0)
                    errors.Add("That ID / Student Number is already registered. Try logging in instead.");

                int emailTaken = Convert.ToInt32(Guades_QCUCram_Db.Scalar(
                    "SELECT COUNT(*) FROM Students WHERE Email = @e",
                    Guades_QCUCram_Db.P("@e", email)));
                if (emailTaken > 0)
                    errors.Add("That email address is already registered.");

                if (errors.Count > 0)
                {
                    Guades_QCUCram_SecurePage.ShowMessage(lblMessage, string.Join(" ", errors), true);
                    ResetButton();
                    return;
                }

                // ---- Save (NoShowCount / IsRestricted use their column defaults: 0 / 0) ----
                Guades_QCUCram_Db.Execute(
                    @"INSERT INTO Students
                        (StudentNumber, LastName, FirstName, MI, Email, Program, RequesterType, PasswordHash)
                      VALUES
                        (@n, @last, @first, @mi, @e, @prog, @type, @hash)",
                    Guades_QCUCram_Db.P("@n", number),
                    Guades_QCUCram_Db.P("@last", last),
                    Guades_QCUCram_Db.P("@first", first),
                    Guades_QCUCram_Db.P("@mi", mi.Length == 0 ? null : mi),
                    Guades_QCUCram_Db.P("@e", email),
                    Guades_QCUCram_Db.P("@prog", program),
                    Guades_QCUCram_Db.P("@type", type),
                    Guades_QCUCram_Db.P("@hash", Guades_QCUCram_PasswordHelper.Hash(password)));
            }
            catch (SqlException ex)
            {
                // 2601 / 2627 = unique constraint hit (two people registering at the same moment)
                if (ex.Number == 2601 || ex.Number == 2627)
                    Guades_QCUCram_SecurePage.ShowMessage(lblMessage,
                        "That ID / Student Number or email is already registered.", true);
                else
                    Guades_QCUCram_SecurePage.ShowMessage(lblMessage,
                        "Sorry, registration could not be completed. Please try again.", true);
                ResetButton();
                return;
            }

            // Step 9's Login page shows the "registered" success message.
            Response.Redirect(Guades_QCUCram_SecurePage.LoginUrl + "?msg=registered");
        }

        // The client script disables the button while submitting; turn it back on after an error.
        private void ResetButton()
        {
            btnRegister.Enabled = true;
        }
    }
}