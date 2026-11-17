using System;
using System.Data;
using System.Globalization;
using System.Text;
using System.Web;

namespace Guades_QCUCram.Helpers
{
    public static class Guades_QCUCram_SettingsHelper
    {
        public static string GetString(string key, string defaultValue = null)
        {
            object v = Guades_QCUCram_Db.Scalar(
                "SELECT SettingValue FROM Settings WHERE SettingKey = @k",
                Guades_QCUCram_Db.P("@k", key));
            return (v == null || v == DBNull.Value) ? defaultValue : Convert.ToString(v);
        }

        public static int GetInt(string key, int defaultValue = 0)
        {
            int result;
            return int.TryParse(GetString(key), out result) ? result : defaultValue;
        }

        // ---------------- Library booking rules (ONE source of wording) ----------------
        // Used by the "Booking Rules" pop-up on the Availability page AND the rules card on the Reserve page,
        // so the two can never drift apart. Every number comes from Settings / TimeSlots, nothing is hard-coded
        // except the group size (5-10) and the branch name, which are fixed by the system rules.
        public static string BuildRulesHtml()
        {
            int ahead = GetInt("AdvanceBookingDays", 1);
            int grace = GetInt("GraceMinutes", 15);
            int threshold = Math.Max(1, GetInt("NoShowThreshold", 3));
            int restrictDays = GetInt("RestrictionDays", 7);

            string aheadText = ahead <= 0 ? "Reserve for <strong>today only</strong>."
                : ahead == 1 ? "<strong>Reserve one day in advance only.</strong> Bookings are made for tomorrow; same-day requests are not accepted."
                : "<strong>Reserve exactly " + ahead + " days in advance.</strong> Same-day requests are not accepted.";

            // Active time slots, e.g. "9 hourly slots from 8:00 AM to 5:00 PM"
            string slotText = "Time slots are set by the library.";
            try
            {
                DataTable dt = Guades_QCUCram_Db.Query(
                    "SELECT MIN(StartTime) AS FirstStart, MAX(EndTime) AS LastEnd, COUNT(*) AS Cnt FROM TimeSlots WHERE IsActive = 1");
                if (dt.Rows.Count > 0 && Convert.ToInt32(dt.Rows[0]["Cnt"]) > 0 && dt.Rows[0]["FirstStart"] != DBNull.Value)
                {
                    DateTime d = DateTime.Today;
                    string from = d.Add((TimeSpan)dt.Rows[0]["FirstStart"]).ToString("h:mm tt", CultureInfo.InvariantCulture);
                    string to = d.Add((TimeSpan)dt.Rows[0]["LastEnd"]).ToString("h:mm tt", CultureInfo.InvariantCulture);
                    int n = Convert.ToInt32(dt.Rows[0]["Cnt"]);
                    slotText = "<strong>Time slots:</strong> " + n + " slot" + (n == 1 ? "" : "s") +
                               " from " + from + " to " + to + ". Collaboration rooms are at the San Bartolome branch.";
                }
            }
            catch (Exception)
            {
                // the rules must still show if the slot lookup fails
            }

            var sb = new StringBuilder();
            sb.Append("<ol class=\"qcu-rules-list\">");
            sb.Append("<li>").Append(aheadText).Append("</li>");
            sb.Append("<li><strong>Bring the physical QCID of all members.</strong> The library assistant verifies it when you claim the room.</li>");
            sb.Append("<li><strong>Claim within ").Append(grace).Append(" minutes</strong> of the slot start, or the booking is forfeited.</li>");
            sb.Append("<li><strong>Maximum of 1 hour per day</strong> per requester (one active reservation per date).</li>");
            sb.Append("<li><strong>Group size: 5 to 10 members.</strong> Every member's name must be listed.</li>");
            sb.Append("<li>").Append(slotText).Append("</li>");
            sb.Append("<li><strong>No eating or drinking</strong> inside the collaboration rooms.</li>");
            sb.Append("<li><strong>Pending requests hold the slot</strong> and must be reviewed by the library assistant before the slot starts, or they expire.</li>");
            sb.Append("<li><strong>Cancelling</strong> before the slot starts is not a no-show.</li>");
            sb.Append("<li><strong>No-shows:</strong> after ").Append(threshold).Append(" forfeiture").Append(threshold == 1 ? "" : "s")
              .Append(" you cannot book for ").Append(restrictDays).Append(" day").Append(restrictDays == 1 ? "" : "s")
              .Append(". The restriction lifts automatically afterwards and your strikes reset.</li>");
            sb.Append("</ol>");
            return sb.ToString();
        }

        public static void Set(string key, string value)
        {
            int rows = Guades_QCUCram_Db.Execute(
                "UPDATE Settings SET SettingValue = @v WHERE SettingKey = @k",
                Guades_QCUCram_Db.P("@v", value),
                Guades_QCUCram_Db.P("@k", key));

            if (rows == 0)
            {
                Guades_QCUCram_Db.Execute(
                    "INSERT INTO Settings (SettingKey, SettingValue) VALUES (@k, @v)",
                    Guades_QCUCram_Db.P("@k", key),
                    Guades_QCUCram_Db.P("@v", value));
            }

            // Make sure the clock re-reads the offset after a settings change
            if (HttpContext.Current != null)
                HttpContext.Current.Items.Remove("Guades_QCUCram_ClockOffset");
        }
    }
}