using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Web.UI.WebControls;
using Guades_QCUCram.Helpers;

namespace Guades_QCUCram.Guades_QCUCram_Admin
{
    public partial class Guades_QCUCram_Settings : Guades_QCUCram_AdminPage
    {
        // Allowed whole-number range per setting: { min, max }
        private static readonly Dictionary<string, int[]> Rules = new Dictionary<string, int[]>
        {
            { "GraceMinutes",         new[] { 1, 240 } },
            { "NoShowThreshold",      new[] { 1, 20 } },
            { "RestrictionDays",      new[] { 0, 365 } },
            { "AdvanceBookingDays",   new[] { 0, 30 } },
            { "DevTimeOffsetMinutes", new[] { 0, 1000000 } }
        };

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                BindGrid();
            }
        }

        // ---------------- Range helpers (used by the .aspx validators) ----------------
        private static int[] RuleFor(object key)
        {
            int[] r;
            return Rules.TryGetValue(Convert.ToString(key), out r) ? r : new[] { 0, 1000000 };
        }

        protected string GetMin(object key) { return RuleFor(key)[0].ToString(CultureInfo.InvariantCulture); }
        protected string GetMax(object key) { return RuleFor(key)[1].ToString(CultureInfo.InvariantCulture); }

        protected string RangeText(object key)
        {
            int[] r = RuleFor(key);
            return "Whole number from " + r[0] + " to " + r[1] + ".";
        }

        // ---------------- Grid ----------------
        private void BindGrid()
        {
            DataTable dt = Guades_QCUCram_Db.Query(
                @"SELECT SettingKey, SettingValue, Description
                    FROM Settings
                   ORDER BY CASE SettingKey
                              WHEN 'GraceMinutes' THEN 1
                              WHEN 'NoShowThreshold' THEN 2
                              WHEN 'RestrictionDays' THEN 3
                              WHEN 'AdvanceBookingDays' THEN 4
                              WHEN 'DevTimeOffsetMinutes' THEN 5
                              ELSE 6 END, SettingKey");
            gvSettings.DataSource = dt;
            gvSettings.DataBind();
        }

        // ---------------- Save ----------------
        protected void btnSave_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            var errors = new List<string>();
            var newValues = new Dictionary<string, int>();

            // Server-side re-check of every row (browser checks can be bypassed)
            foreach (GridViewRow row in gvSettings.Rows)
            {
                var hf = row.FindControl("hfKey") as HiddenField;
                var txt = row.FindControl("txtValue") as TextBox;
                if (hf == null || txt == null) continue;

                string key = hf.Value;
                if (!Rules.ContainsKey(key)) continue; // ignore unknown keys

                int value;
                if (!int.TryParse(txt.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                {
                    errors.Add(key + " must be a whole number.");
                    continue;
                }

                int[] r = Rules[key];
                if (value < r[0] || value > r[1])
                {
                    errors.Add(key + " must be from " + r[0] + " to " + r[1] + ".");
                    continue;
                }

                newValues[key] = value;
            }

            if (errors.Count > 0)
            {
                ShowMessage(lblMessage, string.Join(" ", errors), true);
                return;
            }

            try
            {
                int changed = 0;
                foreach (KeyValuePair<string, int> kv in newValues)
                {
                    string current = Guades_QCUCram_SettingsHelper.GetString(kv.Key);
                    string next = kv.Value.ToString(CultureInfo.InvariantCulture);
                    if (current != next)
                    {
                        Guades_QCUCram_SettingsHelper.Set(kv.Key, next);
                        changed++;
                    }
                }

                ShowMessage(lblMessage,
                    changed == 0 ? "No changes to save." : "Settings saved (" + changed + " changed).",
                    false);
            }
            catch (Exception)
            {
                ShowMessage(lblMessage, "Sorry, the settings could not be saved. Please try again.", true);
            }

            BindGrid();
        }
    }
}