using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI.WebControls;
using Guades_QCUCram.Helpers;

namespace Guades_QCUCram.Guades_QCUCram_Student
{
    public partial class Guades_QCUCram_Profile : Guades_QCUCram_StudentPage
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        private static readonly Regex EmailRegex = new Regex(@"^\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$");

        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                if (!IsPostBack)
                {
                    LoadForm();
                }

                ShowStanding();
                BuildHistory();
            }
            catch (Exception)
            {
                ShowMessage(lblMessage, "Sorry, your profile could not be fully loaded. Please refresh the page.", true);
            }
        }

        // ------------------------------------------------------------------ details form
        private void LoadForm()
        {
            DataTable dt = Guades_QCUCram_Db.Query(
                @"SELECT StudentNumber, LastName, FirstName, MI, Email, Program, RequesterType
                    FROM Students WHERE StudentID = @id",
                Guades_QCUCram_Db.P("@id", CurrentUserId));

            if (dt.Rows.Count == 0)
            {
                // The account no longer exists: send them back to login.
                Response.Redirect(LoginUrl + "?msg=login", true);
                return;
            }

            DataRow r = dt.Rows[0];
            lblNumber.Text = HttpUtility.HtmlEncode(Convert.ToString(r["StudentNumber"]));
            lblType.Text = HttpUtility.HtmlEncode(Convert.ToString(r["RequesterType"]));
            txtLast.Text = Convert.ToString(r["LastName"]);
            txtFirst.Text = Convert.ToString(r["FirstName"]);
            txtMI.Text = r["MI"] == DBNull.Value ? "" : Convert.ToString(r["MI"]);
            txtEmail.Text = Convert.ToString(r["Email"]);

            // Program list. If the saved program is no longer in the list, keep it so nothing is lost.
            string program = Convert.ToString(r["Program"]);
            ddlProgram.Items.Clear();
            ddlProgram.Items.Add(new ListItem("-- Select program --", ""));
            foreach (string p in Guades_QCUCram_Programs.All)
                ddlProgram.Items.Add(new ListItem(p, p));
            if (Array.IndexOf(Guades_QCUCram_Programs.All, program) < 0)
                ddlProgram.Items.Add(new ListItem(program, program));
            ddlProgram.SelectedValue = program;
        }

        protected void btnSave_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            string last = txtLast.Text.Trim();
            string first = txtFirst.Text.Trim();
            string mi = txtMI.Text.Trim().TrimEnd('.');
            string email = txtEmail.Text.Trim();
            string program = ddlProgram.SelectedValue;

            try
            {
                DataTable cur = Guades_QCUCram_Db.Query(
                    "SELECT Program FROM Students WHERE StudentID = @id",
                    Guades_QCUCram_Db.P("@id", CurrentUserId));
                if (cur.Rows.Count == 0)
                {
                    Response.Redirect(LoginUrl + "?msg=login", true);
                    return;
                }
                string currentProgram = Convert.ToString(cur.Rows[0]["Program"]);

                // Server-side re-checks (browser checks can be bypassed)
                var errors = new System.Collections.Generic.List<string>();
                if (last.Length == 0 || first.Length == 0 || last.Length > 60 || first.Length > 60)
                    errors.Add("Last name and first name are required (maximum 60 characters each).");
                if (mi.Length > 5)
                    errors.Add("Middle initial is too long.");
                if (email.Length == 0 || email.Length > 120 || !EmailRegex.IsMatch(email))
                    errors.Add("Please enter a valid email address.");
                if (Array.IndexOf(Guades_QCUCram_Programs.All, program) < 0 && program != currentProgram)
                    errors.Add("Please select a valid program.");

                if (errors.Count > 0)
                {
                    ShowMessage(lblMessage, string.Join(" ", errors), true);
                    return;
                }

                // Email must not belong to anyone else
                int emailTaken = Convert.ToInt32(Guades_QCUCram_Db.Scalar(
                    "SELECT COUNT(*) FROM Students WHERE Email = @e AND StudentID <> @id",
                    Guades_QCUCram_Db.P("@e", email),
                    Guades_QCUCram_Db.P("@id", CurrentUserId)));
                if (emailTaken > 0)
                {
                    ShowMessage(lblMessage, "That email address is already used by another account. Please use a different one.", true);
                    return;
                }

                Guades_QCUCram_Db.Execute(
                    @"UPDATE Students
                         SET LastName = @last, FirstName = @first, MI = @mi,
                             Email = @e, Program = @prog
                       WHERE StudentID = @id",
                    Guades_QCUCram_Db.P("@last", last),
                    Guades_QCUCram_Db.P("@first", first),
                    Guades_QCUCram_Db.P("@mi", mi.Length == 0 ? null : mi),
                    Guades_QCUCram_Db.P("@e", email),
                    Guades_QCUCram_Db.P("@prog", program),
                    Guades_QCUCram_Db.P("@id", CurrentUserId));
            }
            catch (SqlException ex)
            {
                // 2601 / 2627 = unique constraint (someone took the email a moment ago)
                if (ex.Number == 2601 || ex.Number == 2627)
                    ShowMessage(lblMessage, "That email address is already used by another account. Please use a different one.", true);
                else
                    ShowMessage(lblMessage, "Sorry, your changes could not be saved. Please try again.", true);
                return;
            }
            catch (Exception)
            {
                ShowMessage(lblMessage, "Sorry, your changes could not be saved. Please try again.", true);
                return;
            }

            // Keep the "Logged in as ..." name in the menu in step with the new name
            // (same format the Login page uses: first + last).
            Session[SessionUserName] = first + " " + last;

            LoadForm();
            ShowMessage(lblMessage, "Your profile has been updated.", false);
        }

        // ------------------------------------------------------------------ standing (strikes / restriction)
        private void ShowStanding()
        {
            DataTable dt = Guades_QCUCram_Db.Query(
                "SELECT NoShowCount, IsRestricted, RestrictionLiftDate FROM Students WHERE StudentID = @id",
                Guades_QCUCram_Db.P("@id", CurrentUserId));
            if (dt.Rows.Count == 0) { litStanding.Text = ""; return; }

            DataRow me = dt.Rows[0];
            int strikes = Convert.ToInt32(me["NoShowCount"]);
            int threshold = Guades_QCUCram_SettingsHelper.GetInt("NoShowThreshold", 3);
            bool restricted = Convert.ToBoolean(me["IsRestricted"]);
            DateTime now = Guades_QCUCram_Clock.Now;

            // A restriction whose cooldown has passed is about to be lifted by the sweep; don't show it.
            if (restricted && me["RestrictionLiftDate"] != DBNull.Value &&
                Convert.ToDateTime(me["RestrictionLiftDate"]) <= now)
            {
                restricted = false;
            }

            if (restricted)
            {
                string until = me["RestrictionLiftDate"] == DBNull.Value
                    ? "further notice"
                    : Convert.ToDateTime(me["RestrictionLiftDate"]).ToString("MMM d, yyyy h:mm tt", Inv);
                litStanding.Text =
                    "<div class=\"alert alert-danger\"><strong>You are restricted from booking until " +
                    HttpUtility.HtmlEncode(until) + ".</strong> Strikes: " + strikes + " of " + threshold +
                    " (forfeited reservations).</div>";
            }
            else if (strikes > 0)
            {
                litStanding.Text =
                    "<div class=\"alert alert-warning\"><strong>Strikes: " + strikes + " of " + threshold +
                    ".</strong> Each forfeited reservation (not checking in within the grace period) adds a strike. " +
                    "Reaching " + threshold + " temporarily restricts booking.</div>";
            }
            else
            {
                litStanding.Text =
                    "<div class=\"alert alert-success\"><strong>Good standing.</strong> No strikes, and you can book rooms.</div>";
            }
        }

        // ------------------------------------------------------------------ reservation history summary
        private void BuildHistory()
        {
            DataTable dt = Guades_QCUCram_Db.Query(
                @"SELECT COUNT(*) AS Total,
                         ISNULL(SUM(CASE WHEN Status = 'Pending'    THEN 1 ELSE 0 END), 0) AS CntPending,
                         ISNULL(SUM(CASE WHEN Status IN ('Approved','Reassigned') THEN 1 ELSE 0 END), 0) AS CntApproved,
                         ISNULL(SUM(CASE WHEN Status = 'CheckedIn'  THEN 1 ELSE 0 END), 0) AS CntCheckedIn,
                         ISNULL(SUM(CASE WHEN Status = 'Completed'  THEN 1 ELSE 0 END), 0) AS CntCompleted,
                         ISNULL(SUM(CASE WHEN Status = 'Forfeited'  THEN 1 ELSE 0 END), 0) AS CntForfeited,
                         ISNULL(SUM(CASE WHEN Status = 'Cancelled'  THEN 1 ELSE 0 END), 0) AS CntCancelled,
                         ISNULL(SUM(CASE WHEN Status = 'Declined'   THEN 1 ELSE 0 END), 0) AS CntDeclined
                    FROM Reservations WHERE StudentID = @id",
                Guades_QCUCram_Db.P("@id", CurrentUserId));

            DataRow c = dt.Rows[0];
            int total = Convert.ToInt32(c["Total"]);

            if (total == 0)
            {
                litHistory.Text = "<p class=\"text-muted mb-0\">You have not made any reservations yet.</p>";
                return;
            }

            var sb = new StringBuilder();
            sb.Append("<table class=\"table table-sm table-bordered mb-0\"><tbody>");
            Row(sb, "Total reservations", total, true);
            Row(sb, "Pending", Convert.ToInt32(c["CntPending"]), false);
            Row(sb, "Approved / Reassigned", Convert.ToInt32(c["CntApproved"]), false);
            Row(sb, "Checked in", Convert.ToInt32(c["CntCheckedIn"]), false);
            Row(sb, "Completed", Convert.ToInt32(c["CntCompleted"]), false);
            Row(sb, "Forfeited (no-show)", Convert.ToInt32(c["CntForfeited"]), false);
            Row(sb, "Cancelled", Convert.ToInt32(c["CntCancelled"]), false);
            Row(sb, "Declined", Convert.ToInt32(c["CntDeclined"]), false);
            sb.Append("</tbody></table>");
            litHistory.Text = sb.ToString();
        }

        private static void Row(StringBuilder sb, string label, int count, bool bold)
        {
            sb.Append("<tr><td>").Append(bold ? "<strong>" : "").Append(HttpUtility.HtmlEncode(label)).Append(bold ? "</strong>" : "")
              .Append("</td><td class=\"text-end\">").Append(bold ? "<strong>" : "").Append(count).Append(bold ? "</strong>" : "")
              .Append("</td></tr>");
        }
    }
}