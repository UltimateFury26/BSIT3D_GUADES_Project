using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI.WebControls;
using Guades_QCUCram.Helpers;

namespace Guades_QCUCram.Guades_QCUCram_Admin
{
    public partial class Guades_QCUCram_Students : Guades_QCUCram_AdminPage
    {
        private static readonly Regex NumberRegex = new Regex(@"^[A-Za-z0-9\-]{3,20}$");
        private static readonly Regex StudentNumberRegex = new Regex(@"^\d{2}-\d{4}$");
        private static readonly Regex EmailRegex = new Regex(@"^\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$");

        // True only during the request that wants the pop-up showing (Add, Edit, or a failed save).
        // It is a plain field on purpose (not ViewState), so the pop-up is closed on every other refresh.
        private bool _modalOpen;

        protected string ModalOpenCss
        {
            get { return _modalOpen ? " is-open" : ""; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                BindGrid();
            }
        }

        // Tell qcu-ui.js which controls belong to the student-number box (their client IDs are only known here).
        protected void Page_PreRender(object sender, EventArgs e)
        {
            txtNumber.Attributes["data-qcu-idtype"] = ddlType.ClientID;
            txtNumber.Attributes["data-qcu-idlabel"] = lblNumberCaption.ClientID;
            txtNumber.Attributes["data-qcu-idhint"] = "hintStudentNumber";
        }

        // ---------------- Display helpers ----------------
        protected string FormatRestricted(object isRestricted, object liftDate)
        {
            if (isRestricted == null || isRestricted == DBNull.Value || !Convert.ToBoolean(isRestricted))
                return "No";
            if (liftDate == null || liftDate == DBNull.Value)
                return "Yes";
            return "Yes (until " +
                   Convert.ToDateTime(liftDate).ToString("MMM d, yyyy h:mm tt", CultureInfo.InvariantCulture) + ")";
        }

        // ---------------- Search + Grid ----------------
        private void BindGrid()
        {
            string q = txtSearch.Text.Trim();
            var parms = new List<SqlParameter>();
            string where = "";

            if (q.Length > 0)
            {
                // Escape LIKE wildcards so what the admin types is searched literally.
                string like = "%" + q.Replace("\\", "\\\\").Replace("%", "\\%")
                                     .Replace("_", "\\_").Replace("[", "\\[") + "%";
                where = @" WHERE s.StudentNumber LIKE @q ESCAPE '\'
                              OR s.LastName LIKE @q ESCAPE '\'
                              OR s.FirstName LIKE @q ESCAPE '\'
                              OR s.Email LIKE @q ESCAPE '\'
                              OR s.Program LIKE @q ESCAPE '\'
                              OR (s.FirstName + ' ' + s.LastName) LIKE @q ESCAPE '\'
                              OR (s.LastName + ', ' + s.FirstName) LIKE @q ESCAPE '\' ";
                parms.Add(Guades_QCUCram_Db.P("@q", like));
            }

            DataTable dt = Guades_QCUCram_Db.Query(
                @"SELECT s.StudentID, s.StudentNumber,
                         s.LastName + ', ' + s.FirstName +
                           CASE WHEN s.MI IS NULL OR s.MI = '' THEN '' ELSE ' ' + s.MI + '.' END AS FullName,
                         s.Email, s.Program, s.RequesterType,
                         s.NoShowCount, s.IsRestricted, s.RestrictionLiftDate,
                         (SELECT COUNT(*) FROM Reservations x WHERE x.StudentID = s.StudentID) AS ResCount
                    FROM Students s" + where + @"
                   ORDER BY s.LastName, s.FirstName, s.StudentID",
                parms.ToArray());

            // Stay on a valid page after a delete / new search.
            int maxPage = Math.Max(0, (dt.Rows.Count - 1) / gvStudents.PageSize);
            if (gvStudents.PageIndex > maxPage) gvStudents.PageIndex = maxPage;

            gvStudents.DataSource = dt;
            gvStudents.DataBind();

            lblCount.Text = q.Length > 0
                ? dt.Rows.Count + " student(s) match \"" + HttpUtility.HtmlEncode(q) + "\"."
                : dt.Rows.Count + " student(s) registered.";
        }

        protected void btnSearch_Click(object sender, EventArgs e)
        {
            lblMessage.Visible = false;
            gvStudents.PageIndex = 0;
            BindGrid();
        }

        protected void btnClearSearch_Click(object sender, EventArgs e)
        {
            lblMessage.Visible = false;
            txtSearch.Text = "";
            gvStudents.PageIndex = 0;
            BindGrid();
        }

        protected void gvStudents_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvStudents.PageIndex = e.NewPageIndex;
            BindGrid();
        }

        protected void gvStudents_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            int studentId;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out studentId)) return;

            try
            {
                switch (e.CommandName)
                {
                    case "EditStudent":
                        LoadForEdit(studentId);
                        break;
                    case "ResetPassword":
                        ResetPassword(studentId);
                        break;
                    case "DeleteStudent":
                        DeleteStudent(studentId);
                        break;
                }
            }
            catch (Exception)
            {
                ShowMessage(lblMessage, "Sorry, that action could not be completed. Please try again.", true);
            }
        }

        // ---------------- Pop-up: helpers ----------------
        // placeholder = text of an empty first choice ("-- Select --"), or null for none.
        private static void BindList(DropDownList ddl, string[] items, string placeholder)
        {
            ddl.Items.Clear();
            if (placeholder != null) ddl.Items.Add(new ListItem(placeholder, ""));
            foreach (string item in items)
                ddl.Items.Add(new ListItem(item, item));
        }

        // Select the stored value; if the dropdown list no longer contains it, keep it so nothing is lost.
        private static void SelectOrAdd(DropDownList ddl, string value)
        {
            if (ddl.Items.FindByValue(value) == null)
                ddl.Items.Insert(0, new ListItem(value, value));
            ddl.SelectedValue = value;
        }

        // A problem found while saving keeps the pop-up open and is shown inside it.
        private void ModalError(string message)
        {
            ShowMessage(lblModalMessage, message, true);
            _modalOpen = true;
        }

        private void CloseModal()
        {
            _modalOpen = false;
            hfStudentId.Value = "";
            lblModalMessage.Visible = false;
        }

        // ---------------- Pop-up: open for ADD ----------------
        protected void btnAddNew_Click(object sender, EventArgs e)
        {
            lblMessage.Visible = false;
            lblModalMessage.Visible = false;

            BindList(ddlType, Guades_QCUCram_Programs.RequesterTypes, "-- Select type --");
            BindList(ddlProgram, Guades_QCUCram_Programs.All, "-- Select program --");

            hfStudentId.Value = "";
            ddlType.SelectedValue = "Student";            // the most common choice
            txtNumber.Text = "";
            txtNumber.Attributes["data-qcu-idstrict"] = "1";   // Student numbers must look like 23-0001
            txtLast.Text = "";
            txtFirst.Text = "";
            txtMI.Text = "";
            txtEmail.Text = "";
            pnlPassword.Visible = true;                   // a temporary password is generated on save

            lblFormTitle.Text = "Add Student";
            btnSave.Text = "Add Student";
            _modalOpen = true;
        }

        // ---------------- Pop-up: open for EDIT ----------------
        private void LoadForEdit(int studentId)
        {
            DataTable dt = Guades_QCUCram_Db.Query(
                @"SELECT StudentID, StudentNumber, LastName, FirstName, MI, Email, Program, RequesterType
                    FROM Students WHERE StudentID = @id",
                Guades_QCUCram_Db.P("@id", studentId));
            if (dt.Rows.Count == 0)
            {
                ShowMessage(lblMessage, "That student no longer exists.", true);
                CloseModal();
                BindGrid();
                return;
            }

            DataRow r = dt.Rows[0];
            BindList(ddlType, Guades_QCUCram_Programs.RequesterTypes, null);
            BindList(ddlProgram, Guades_QCUCram_Programs.All, null);

            lblMessage.Visible = false;
            lblModalMessage.Visible = false;
            hfStudentId.Value = studentId.ToString();
            SelectOrAdd(ddlType, r["RequesterType"].ToString());
            txtNumber.Text = r["StudentNumber"].ToString();
            txtNumber.Attributes["data-qcu-idstrict"] = "0";   // keep older IDs (e.g. SAMPLE-001) editable
            txtLast.Text = r["LastName"].ToString();
            txtFirst.Text = r["FirstName"].ToString();
            txtMI.Text = r["MI"] == DBNull.Value ? "" : r["MI"].ToString();
            txtEmail.Text = r["Email"].ToString();
            SelectOrAdd(ddlProgram, r["Program"].ToString());
            pnlPassword.Visible = false;                  // passwords are changed with Reset Password

            lblFormTitle.Text = "Edit Student #" + studentId;
            btnSave.Text = "Update Student";
            _modalOpen = true;
        }

        // ---------------- Pop-up: save (Add or Edit) ----------------
        protected void btnSave_Click(object sender, EventArgs e)
        {
            lblModalMessage.Visible = false;

            if (!Page.IsValid)
            {
                _modalOpen = true;      // the validators show their own messages
                return;
            }

            int id;
            if (int.TryParse(hfStudentId.Value, out id) && id > 0)
                SaveEdit(id);
            else
                SaveNew();
        }

        // ----- Add a new account (registration rules + an automatic temporary password) -----
        private void SaveNew()
        {
            string type = ddlType.SelectedValue;
            string number = txtNumber.Text.Trim();
            string last = txtLast.Text.Trim();
            string first = txtFirst.Text.Trim();
            string mi = txtMI.Text.Trim().TrimEnd('.').ToUpperInvariant();
            if (mi.Length > 2) mi = mi.Substring(0, 2);
            string email = txtEmail.Text.Trim();
            string program = ddlProgram.SelectedValue;
            string password = null;                       // filled in just before the INSERT

            // A student typed as 230001 means 23-0001
            if (type == "Student") number = Guades_QCUCram_Programs.NormalizeStudentNumber(number);

            // Server-side re-checks (browser checks can be bypassed)
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
            else if (!NumberRegex.IsMatch(number))
            {
                errors.Add("ID number must be 3-20 letters, numbers, or dashes only.");
            }
            if (last.Length == 0 || first.Length == 0 || last.Length > 60 || first.Length > 60)
                errors.Add("Last name and first name are required (maximum 60 characters each).");
            if (email.Length == 0 || email.Length > 120 || !EmailRegex.IsMatch(email))
                errors.Add("Please enter a valid email address.");

            if (errors.Count > 0)
            {
                ModalError(string.Join(" ", errors));
                return;
            }

            try
            {
                int numberTaken = Convert.ToInt32(Guades_QCUCram_Db.Scalar(
                    "SELECT COUNT(*) FROM Students WHERE StudentNumber = @n",
                    Guades_QCUCram_Db.P("@n", number)));
                if (numberTaken > 0)
                    errors.Add("Another account already uses that ID / Student Number.");

                int emailTaken = Convert.ToInt32(Guades_QCUCram_Db.Scalar(
                    "SELECT COUNT(*) FROM Students WHERE Email = @e",
                    Guades_QCUCram_Db.P("@e", email)));
                if (emailTaken > 0)
                    errors.Add("Another account already uses that email address.");

                if (errors.Count > 0)
                {
                    ModalError(string.Join(" ", errors));
                    return;
                }

                // Temporary password: "student" + the last 4 digits of the ID (23-0001 -> student0001).
                // The password strength rules are for passwords people choose, so they are not applied here.
                password = Guades_QCUCram_PasswordHelper.StudentTemporaryPassword(number);

                // NoShowCount / IsRestricted use their column defaults (0 / 0), same as Register.
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
                // 2601 / 2627 = unique constraint hit (two people saving at the same moment)
                if (ex.Number == 2601 || ex.Number == 2627)
                    ModalError("That ID / Student Number or email is already used by another account.");
                else
                    ModalError("Sorry, the student could not be saved. Please try again.");
                return;
            }

            CloseModal();
            ShowMessage(lblMessage,
                "Account " + number + " added. Temporary password: " + password + ".", false);
            BindGrid();
        }

        // ----- Update an existing account -----
        private void SaveEdit(int id)
        {
            string type = ddlType.SelectedValue;
            string number = txtNumber.Text.Trim();
            string last = txtLast.Text.Trim();
            string first = txtFirst.Text.Trim();
            string mi = txtMI.Text.Trim().TrimEnd('.');
            string email = txtEmail.Text.Trim();
            string program = ddlProgram.SelectedValue;

            try
            {
                DataTable cur = Guades_QCUCram_Db.Query(
                    "SELECT Program FROM Students WHERE StudentID = @id", Guades_QCUCram_Db.P("@id", id));
                if (cur.Rows.Count == 0)
                {
                    CloseModal();
                    ShowMessage(lblMessage, "That student no longer exists.", true);
                    BindGrid();
                    return;
                }
                string currentProgram = cur.Rows[0]["Program"].ToString();

                // Server-side re-checks (browser checks can be bypassed)
                var errors = new List<string>();
                if (Array.IndexOf(Guades_QCUCram_Programs.RequesterTypes, type) < 0)
                    errors.Add("Please select a valid requester type.");
                if (Array.IndexOf(Guades_QCUCram_Programs.All, program) < 0 && program != currentProgram)
                    errors.Add("Please select a valid program.");
                if (!NumberRegex.IsMatch(number))
                    errors.Add("ID / Student Number must be 3-20 letters, numbers, or dashes.");
                if (last.Length == 0 || first.Length == 0 || last.Length > 60 || first.Length > 60)
                    errors.Add("Last name and first name are required (maximum 60 characters each).");
                if (mi.Length > 5)
                    errors.Add("Middle initial is too long.");
                if (email.Length == 0 || email.Length > 120 || !EmailRegex.IsMatch(email))
                    errors.Add("Please enter a valid email address.");

                if (errors.Count > 0)
                {
                    ModalError(string.Join(" ", errors));
                    return;
                }

                // Unique checks (ignoring this student)
                int numberTaken = Convert.ToInt32(Guades_QCUCram_Db.Scalar(
                    "SELECT COUNT(*) FROM Students WHERE StudentNumber = @n AND StudentID <> @id",
                    Guades_QCUCram_Db.P("@n", number), Guades_QCUCram_Db.P("@id", id)));
                if (numberTaken > 0)
                    errors.Add("Another account already uses that ID / Student Number.");

                int emailTaken = Convert.ToInt32(Guades_QCUCram_Db.Scalar(
                    "SELECT COUNT(*) FROM Students WHERE Email = @e AND StudentID <> @id",
                    Guades_QCUCram_Db.P("@e", email), Guades_QCUCram_Db.P("@id", id)));
                if (emailTaken > 0)
                    errors.Add("Another account already uses that email address.");

                if (errors.Count > 0)
                {
                    ModalError(string.Join(" ", errors));
                    return;
                }

                int rows = Guades_QCUCram_Db.Execute(
                    @"UPDATE Students
                         SET StudentNumber = @n, LastName = @last, FirstName = @first, MI = @mi,
                             Email = @e, Program = @prog, RequesterType = @type
                       WHERE StudentID = @id",
                    Guades_QCUCram_Db.P("@n", number),
                    Guades_QCUCram_Db.P("@last", last),
                    Guades_QCUCram_Db.P("@first", first),
                    Guades_QCUCram_Db.P("@mi", mi.Length == 0 ? null : mi),
                    Guades_QCUCram_Db.P("@e", email),
                    Guades_QCUCram_Db.P("@prog", program),
                    Guades_QCUCram_Db.P("@type", type),
                    Guades_QCUCram_Db.P("@id", id));

                if (rows == 0)
                {
                    CloseModal();
                    ShowMessage(lblMessage, "That student no longer exists.", true);
                    BindGrid();
                    return;
                }
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2601 || ex.Number == 2627)
                    ModalError("That ID / Student Number or email is already used by another account.");
                else
                    ModalError("Sorry, the student could not be saved. Please try again.");
                return;
            }

            CloseModal();
            ShowMessage(lblMessage, "Student updated.", false);
            BindGrid();
        }

        // ---------------- Reset password ----------------
        private void ResetPassword(int studentId)
        {
            DataTable dt = Guades_QCUCram_Db.Query(
                "SELECT StudentNumber FROM Students WHERE StudentID = @id", Guades_QCUCram_Db.P("@id", studentId));
            if (dt.Rows.Count == 0)
            {
                ShowMessage(lblMessage, "That student no longer exists.", true);
                BindGrid();
                return;
            }

            string temp = Guades_QCUCram_PasswordHelper.StudentTemporaryPassword(Convert.ToString(dt.Rows[0]["StudentNumber"]));
            Guades_QCUCram_Db.Execute(
                "UPDATE Students SET PasswordHash = @h WHERE StudentID = @id",
                Guades_QCUCram_Db.P("@h", Guades_QCUCram_PasswordHelper.Hash(temp)),
                Guades_QCUCram_Db.P("@id", studentId));

            ShowMessage(lblMessage,
                "Password reset for " + dt.Rows[0]["StudentNumber"] + ". Temporary password: " + temp + ".",
                false);
        }

        // ---------------- Delete ----------------
        private void DeleteStudent(int studentId)
        {
            int resCount = Convert.ToInt32(Guades_QCUCram_Db.Scalar(
                "SELECT COUNT(*) FROM Reservations WHERE StudentID = @id", Guades_QCUCram_Db.P("@id", studentId)));
            if (resCount > 0)
            {
                ShowMessage(lblMessage,
                    "This student has reservations and cannot be deleted. Their history must be kept.", true);
                return;
            }

            try
            {
                // Notifications reference the student too, so they go in the same all-or-nothing batch.
                Guades_QCUCram_Db.Execute(
                    @"SET XACT_ABORT ON;
                      BEGIN TRAN;
                      IF NOT EXISTS (SELECT 1 FROM Reservations WHERE StudentID = @id)
                      BEGIN
                          DELETE FROM Notifications WHERE StudentID = @id;
                          DELETE FROM Students WHERE StudentID = @id;
                      END
                      COMMIT;",
                    Guades_QCUCram_Db.P("@id", studentId));
            }
            catch (SqlException ex)
            {
                if (ex.Number == 547)
                {
                    ShowMessage(lblMessage, "This student is still in use and cannot be deleted.", true);
                    return;
                }
                throw;
            }

            int stillThere = Convert.ToInt32(Guades_QCUCram_Db.Scalar(
                "SELECT COUNT(*) FROM Students WHERE StudentID = @id", Guades_QCUCram_Db.P("@id", studentId)));
            if (stillThere > 0)
            {
                ShowMessage(lblMessage,
                    "This student has reservations and cannot be deleted. Their history must be kept.", true);
                return;
            }

            ShowMessage(lblMessage, "Student account deleted.", false);
            BindGrid();
        }
    }
}