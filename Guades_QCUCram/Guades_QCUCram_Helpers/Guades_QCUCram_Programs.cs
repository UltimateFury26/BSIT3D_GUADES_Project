namespace Guades_QCUCram.Helpers
{
    // One place for the dropdown lists used by Register (and later Profile / Students pages).
    public static class Guades_QCUCram_Programs
    {
        public static readonly string[] RequesterTypes = { "Student", "Faculty", "Visitor" };

        // "230001" -> "23-0001". Anything that is not exactly 6 digits (with or without the dash) is returned unchanged.
        public static string NormalizeStudentNumber(string value)
        {
            value = (value ?? "").Trim();
            string digits = value.Replace("-", "");
            if (digits.Length != 6) return value;
            foreach (char c in digits) if (c < '0' || c > '9') return value;
            return digits.Substring(0, 2) + "-" + digits.Substring(2);
        }

        // Edit this list to match the real QCU programs.
        public static readonly string[] All =
        {
            "BS Information Technology",
            "BS Computer Science",
            "BS Entrepreneurship",
            "BS Accountancy",
            "BS Industrial Engineering",
            "BS Computer Engineering",
            "BS Education",
            "Not applicable (Faculty / Visitor)"
        };
    }
}