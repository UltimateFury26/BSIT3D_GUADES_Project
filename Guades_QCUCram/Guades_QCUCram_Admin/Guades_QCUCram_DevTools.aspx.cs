using System;
using System.Globalization;
using System.Web;
using Guades_QCUCram.Helpers;

namespace Guades_QCUCram.Guades_QCUCram_Admin
{
    public partial class Guades_QCUCram_DevTools : Guades_QCUCram_AdminPage
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        private const string OffsetKey = "DevTimeOffsetMinutes";
        private const int MaxOffset = 1000000;   // same limit as the Settings page

        // The test page must not run the sweep on its own, so "Run sweep now" shows real counts
        protected override bool RunSweepOnLoad
        {
            get { return false; }
        }

        // Drawn after the buttons are handled, so it always shows the latest state
        protected void Page_PreRender(object sender, EventArgs e)
        {
            ShowClock();
            ShowCounts();
        }

        // ------------------------------------------------------------------ clock
        private void ShowClock()
        {
            int offset = Guades_QCUCram_SettingsHelper.GetInt(OffsetKey, 0);
            DateTime real = DateTime.Now;
            DateTime sim = real.AddMinutes(offset);

            litClock.Text =
                "<table class=\"table table-sm table-bordered w-auto\">" +
                "<tr><th>Real time</th><td>" + HttpUtility.HtmlEncode(real.ToString("ddd, MMM d, yyyy h:mm:ss tt", Inv)) + "</td></tr>" +
                "<tr><th>Offset</th><td>" + offset + " minute(s)</td></tr>" +
                "<tr><th>System time (used by CRAM)</th><td><strong>" +
                    HttpUtility.HtmlEncode(sim.ToString("ddd, MMM d, yyyy h:mm:ss tt", Inv)) + "</strong></td></tr>" +
                "</table>";
        }

        protected void btnShift_Command(object sender, System.Web.UI.WebControls.CommandEventArgs e)
        {
            string arg = Convert.ToString(e.CommandArgument);

            if (arg == "reset")
            {
                Guades_QCUCram_SettingsHelper.Set(OffsetKey, "0");
                ShowMessage(lblMessage, "Clock reset to real time.", false);
                return;
            }

            int delta;
            if (!int.TryParse(arg, out delta)) return;

            long next = (long)Guades_QCUCram_SettingsHelper.GetInt(OffsetKey, 0) + delta;
            if (next < 0) next = 0;
            if (next > MaxOffset) next = MaxOffset;

            Guades_QCUCram_SettingsHelper.Set(OffsetKey, next.ToString(Inv));
            ShowMessage(lblMessage, "Clock moved forward. The offset is now " + next + " minute(s).", false);
        }

        // ------------------------------------------------------------------ sweep
        protected void btnSweep_Click(object sender, EventArgs e)
        {
            try
            {
                var result = Guades_QCUCram_MaintenanceService.RunSweep();
                string css = result.Errors > 0 ? "alert-danger" : (result.ChangedAnything ? "alert-success" : "alert-info");
                litSweepResult.Text = "<div class=\"alert " + css + "\">" +
                                      HttpUtility.HtmlEncode(result.Summary) + "</div>";
            }
            catch (Exception ex)
            {
                litSweepResult.Text = "<div class=\"alert alert-danger\">The sweep failed: " +
                                      HttpUtility.HtmlEncode(ex.Message) + "</div>";
            }
        }

        // ------------------------------------------------------------------ sample data
        private void ShowCounts()
        {
            Guades_QCUCram_SampleData.Counts c = Guades_QCUCram_SampleData.GetCounts();
            litCounts.Text =
                "<table class=\"table table-sm table-bordered w-auto\">" +
                "<tr><th>Students</th><td>" + c.Students + " (" + c.SampleStudents + " sample: " +
                    c.SampleRestricted + " restricted now, " + c.SampleOneStrikeAway + " one strike away)</td></tr>" +
                "<tr><th>Reservations</th><td>" + c.Reservations + " (" + c.SampleReservations + " sample)</td></tr>" +
                "<tr><th>Notifications</th><td>" + c.Notifications + "</td></tr>" +
                "</table>";
        }

        // Details of the last action stay on the page (the toast disappears after 5 seconds)
        private void ShowResult(string html)
        {
            litSampleResult.Text = "<div class=\"alert alert-success py-2 small\">" + html + "</div>";
        }

        protected void btnGenStudents_Click(object sender, EventArgs e)
        {
            try
            {
                Guades_QCUCram_SampleData.StudentBatch b =
                    Guades_QCUCram_SampleData.GenerateStudents(Guades_QCUCram_SampleData.StudentsPerClick, CurrentUserId);

                ShowResult(
                    "<strong>" + b.Created + " students added</strong> (" +
                    b.Clean + " clean &middot; " + b.OneStrikeAway + " one strike away &middot; " +
                    b.RestrictedNow + " restricted now &middot; " + b.ExpiredRestriction + " expired restriction; " +
                    (b.Created - b.Faculty - b.Visitors) + " students, " + b.Faculty + " faculty, " + b.Visitors + " visitor). " +
                    "Matching forfeited reservations added: " + b.StrikeReservations + ".<br />" +
                    "Log in with: " + HttpUtility.HtmlEncode(string.Join(", ", b.Numbers.ToArray())) +
                    " &middot; password <code>" + HttpUtility.HtmlEncode(Guades_QCUCram_SampleData.SamplePassword) + "</code>");

                ShowMessage(lblMessage, b.Created + " sample students added.", false);
            }
            catch (Exception)
            {
                ShowMessage(lblMessage, "Could not create the sample students.", true);
            }
        }

        protected void btnGenRes_Click(object sender, EventArgs e)
        {
            try
            {
                int days = int.Parse(ddlDays.SelectedValue, Inv);
                int ahead = int.Parse(ddlAhead.SelectedValue, Inv);
                Guades_QCUCram_SampleData.ReservationBatch b =
                    Guades_QCUCram_SampleData.GenerateReservations(days, ahead, CurrentUserId);

                string[] order = { "Pending", "Approved", "Reassigned", "Declined", "Cancelled", "CheckedIn", "Completed", "Forfeited" };
                var parts = new System.Collections.Generic.List<string>();
                foreach (string s in order) parts.Add(s + " " + b.Count(s));

                string dates = (b.FirstDate.HasValue && b.LastDate.HasValue)
                    ? ", " + b.FirstDate.Value.ToString("MMM d", Inv) + " to " + b.LastDate.Value.ToString("MMM d, yyyy", Inv)
                    : "";

                string html =
                    "<strong>" + b.Created + " sample reservations added</strong> (" + string.Join(" &middot; ", parts.ToArray()) + "). " +
                    b.RoomsCovered + " of " + b.RoomsTotal + " rooms covered" + dates + ".";
                if (b.StrikeReservations > 0)
                    html += " " + b.StrikeReservations + " of them are forfeited reservations that back the students' strikes.";
                if (b.CheckedInSkipped)
                    html += "<br /><strong>No CheckedIn reservation was made:</strong> no time slot is running right now. " +
                            "Move the clock into library hours (clock buttons above) and click again.";
                ShowResult(html);

                ShowMessage(lblMessage, b.Created + " sample reservations added.", false);
            }
            catch (InvalidOperationException ex)
            {
                ShowMessage(lblMessage, ex.Message, true);
            }
            catch (Exception)
            {
                ShowMessage(lblMessage, "Could not create the sample reservations.", true);
            }
        }

        protected void btnClearSampleRes_Click(object sender, EventArgs e)
        {
            try
            {
                int n = Guades_QCUCram_SampleData.ClearSampleReservations();
                ShowMessage(lblMessage, n + " sample reservations deleted.", false);
            }
            catch (Exception)
            {
                ShowMessage(lblMessage, "Could not delete the sample reservations.", true);
            }
        }

        protected void btnDeleteSampleStudents_Click(object sender, EventArgs e)
        {
            try
            {
                int n = Guades_QCUCram_SampleData.DeleteSampleStudents();
                ShowMessage(lblMessage, n + " sample students deleted.", false);
            }
            catch (Exception)
            {
                ShowMessage(lblMessage, "Could not delete the sample students.", true);
            }
        }

        // ------------------------------------------------------------------ danger zone
        protected void btnClearAll_Click(object sender, EventArgs e)
        {
            if (txtConfirmAll.Text.Trim() != "DELETE")
            {
                ShowMessage(lblMessage, "Type DELETE (all capitals) in the box to confirm. Nothing was deleted.", true);
                return;
            }

            try
            {
                int n = Guades_QCUCram_SampleData.ClearAllReservations();
                txtConfirmAll.Text = "";
                ShowMessage(lblMessage,
                    "Deleted " + n + " reservation(s). Strikes and restrictions were reset.", false);
            }
            catch (Exception)
            {
                ShowMessage(lblMessage, "Could not clear the data.", true);
            }
        }
    }
}