using System;
using System.Web;

namespace Guades_QCUCram.Helpers
{
    // ALWAYS use this instead of DateTime.Now so the Dev Tools time-shift works.
    public static class Guades_QCUCram_Clock
    {
        private const string CacheKey = "Guades_QCUCram_ClockOffset";

        public static int OffsetMinutes
        {
            get
            {
                HttpContext ctx = HttpContext.Current;
                if (ctx != null && ctx.Items[CacheKey] != null)
                    return (int)ctx.Items[CacheKey];

                int offset = Guades_QCUCram_SettingsHelper.GetInt("DevTimeOffsetMinutes", 0);
                if (ctx != null) ctx.Items[CacheKey] = offset;
                return offset;
            }
        }

        public static DateTime Now
        {
            get { return DateTime.Now.AddMinutes(OffsetMinutes); }
        }

        public static DateTime Today
        {
            get { return Now.Date; }
        }
    }
}