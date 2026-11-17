namespace Guades_QCUCram.Helpers
{
    public static class Guades_QCUCram_Status
    {
        public const string Pending = "Pending";
        public const string Approved = "Approved";
        public const string Reassigned = "Reassigned";
        public const string Declined = "Declined";
        public const string Cancelled = "Cancelled";
        public const string CheckedIn = "CheckedIn";
        public const string Completed = "Completed";
        public const string Forfeited = "Forfeited";

        // "Active" = the slot is held (everything except Declined, Cancelled, Forfeited)
        public static bool IsActive(string status)
        {
            return status != Declined && status != Cancelled && status != Forfeited;
        }

        // "Occupied" = confirmed bookings (shown as Taken on the availability grid)
        public static bool IsOccupied(string status)
        {
            return status == Approved || status == Reassigned
                || status == CheckedIn || status == Completed;
        }
    }
}