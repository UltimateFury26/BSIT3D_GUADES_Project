using System;
using System.Data;
using System.Data.SqlClient;

namespace Guades_QCUCram.Helpers
{
    // All analytics queries (Step 33). Every method takes the From/To date range and
    // returns a DataTable (or a single value) for the Analytics page (Step 34).
    public static class Guades_QCUCram_AnalyticsService
    {
        // Fixed status lists (constants, not user input)
        private const string Occupied = "'Approved','Reassigned','CheckedIn','Completed'";
        private const string OccupiedOrForfeited = "'Approved','Reassigned','CheckedIn','Completed','Forfeited'";

        private static SqlParameter[] Range(DateTime from, DateTime to)
        {
            return new[]
            {
                new SqlParameter("@From", SqlDbType.Date) { Value = from.Date },
                new SqlParameter("@To",   SqlDbType.Date) { Value = to.Date }
            };
        }

        // Summary card: total bookings in range
        public static int TotalBookings(DateTime from, DateTime to)
        {
            object v = Guades_QCUCram_Db.Scalar(
                "SELECT COUNT(*) FROM Reservations WHERE ReservationDate BETWEEN @From AND @To",
                Range(from, to));
            return Convert.ToInt32(v);
        }

        // 1. Status breakdown
        public static DataTable StatusBreakdown(DateTime from, DateTime to)
        {
            return Guades_QCUCram_Db.Query(
                @"SELECT Status, COUNT(*) AS Total
                    FROM Reservations
                   WHERE ReservationDate BETWEEN @From AND @To
                   GROUP BY Status
                   ORDER BY Total DESC", Range(from, to));
        }

        // 2. Bookings per month
        public static DataTable BookingsPerMonth(DateTime from, DateTime to)
        {
            return Guades_QCUCram_Db.Query(
                @"SELECT FORMAT(ReservationDate, 'yyyy-MM') AS [Month], COUNT(*) AS Total
                    FROM Reservations
                   WHERE ReservationDate BETWEEN @From AND @To
                   GROUP BY FORMAT(ReservationDate, 'yyyy-MM')
                   ORDER BY 1", Range(from, to));
        }

        // 3. Bookings per weekday (Monday first)
        public static DataTable BookingsPerWeekday(DateTime from, DateTime to)
        {
            return Guades_QCUCram_Db.Query(
                @"SELECT DATENAME(WEEKDAY, ReservationDate) AS [Day], COUNT(*) AS Total
                    FROM Reservations
                   WHERE ReservationDate BETWEEN @From AND @To
                   GROUP BY DATENAME(WEEKDAY, ReservationDate),
                            (DATEPART(WEEKDAY, ReservationDate) + @@DATEFIRST - 2) % 7
                   ORDER BY (DATEPART(WEEKDAY, ReservationDate) + @@DATEFIRST - 2) % 7", Range(from, to));
        }

        // 4. Room utilization (first row = most booked, last row = least booked)
        public static DataTable RoomUtilization(DateTime from, DateTime to)
        {
            return Guades_QCUCram_Db.Query(
                @"SELECT r.RoomName,
                         COUNT(res.ReservationID) AS Booked,
                         (SELECT COUNT(*) FROM TimeSlots WHERE IsActive = 1) * (DATEDIFF(DAY, @From, @To) + 1) AS Available,
                         CAST(100.0 * COUNT(res.ReservationID) /
                              NULLIF((SELECT COUNT(*) FROM TimeSlots WHERE IsActive = 1) * (DATEDIFF(DAY, @From, @To) + 1), 0)
                              AS DECIMAL(5,1)) AS UtilizationPct
                    FROM Rooms r
                    LEFT JOIN Reservations res ON res.RoomID = r.RoomID
                         AND res.ReservationDate BETWEEN @From AND @To
                         AND res.Status IN (" + Occupied + @")
                   GROUP BY r.RoomID, r.RoomName
                   ORDER BY Booked DESC, r.RoomName", Range(from, to));
        }

        // 5. Peak time slot (adds a readable "Slot" column such as "8:00 AM - 9:00 AM")
        public static DataTable PeakTimeSlots(DateTime from, DateTime to)
        {
            DataTable dt = Guades_QCUCram_Db.Query(
                @"SELECT t.StartTime, t.EndTime, COUNT(res.ReservationID) AS Total
                    FROM TimeSlots t
                    LEFT JOIN Reservations res ON res.TimeSlotID = t.TimeSlotID
                         AND res.ReservationDate BETWEEN @From AND @To
                         AND res.Status IN (" + OccupiedOrForfeited + @")
                   GROUP BY t.TimeSlotID, t.StartTime, t.EndTime
                   ORDER BY Total DESC, t.StartTime", Range(from, to));

            dt.Columns.Add("Slot", typeof(string));
            foreach (DataRow r in dt.Rows)
            {
                r["Slot"] = Guades_QCUCram_ReservationService.FormatSlot((TimeSpan)r["StartTime"], (TimeSpan)r["EndTime"]);
            }
            return dt;
        }

        // 6. No-show (forfeiture) rate overall. One row: Forfeited, ApprovedOrLater, NoShowPct (NoShowPct is NULL when there are no rows)
        public static DataTable NoShowRate(DateTime from, DateTime to)
        {
            return Guades_QCUCram_Db.Query(
                @"SELECT ISNULL(SUM(CASE WHEN Status = 'Forfeited' THEN 1 ELSE 0 END), 0) AS Forfeited,
                         COUNT(*) AS ApprovedOrLater,
                         CAST(100.0 * SUM(CASE WHEN Status = 'Forfeited' THEN 1 ELSE 0 END) / NULLIF(COUNT(*), 0) AS DECIMAL(5,1)) AS NoShowPct
                    FROM Reservations
                   WHERE ReservationDate BETWEEN @From AND @To
                     AND Status IN (" + OccupiedOrForfeited + ")", Range(from, to));
        }

        // 7. No-shows per student
        public static DataTable NoShowsPerStudent(DateTime from, DateTime to)
        {
            return Guades_QCUCram_Db.Query(
                @"SELECT s.StudentNumber, s.LastName + ', ' + s.FirstName AS Student, COUNT(*) AS Forfeitures
                    FROM Reservations res
                    JOIN Students s ON s.StudentID = res.StudentID
                   WHERE res.Status = 'Forfeited' AND res.ReservationDate BETWEEN @From AND @To
                   GROUP BY s.StudentID, s.StudentNumber, s.LastName, s.FirstName
                   ORDER BY Forfeitures DESC, s.StudentNumber", Range(from, to));
        }

        // 8. Currently restricted students (a "right now" number, not date-filtered)
        public static int CurrentlyRestricted()
        {
            object v = Guades_QCUCram_Db.Scalar("SELECT COUNT(*) FROM Students WHERE IsRestricted = 1");
            return Convert.ToInt32(v);
        }

        // 9. Bookings per program
        public static DataTable BookingsPerProgram(DateTime from, DateTime to)
        {
            return Guades_QCUCram_Db.Query(
                @"SELECT s.Program, COUNT(*) AS Total
                    FROM Reservations res
                    JOIN Students s ON s.StudentID = res.StudentID
                   WHERE res.ReservationDate BETWEEN @From AND @To
                   GROUP BY s.Program
                   ORDER BY Total DESC, s.Program", Range(from, to));
        }

        // 10. Average group size (null when there are no bookings in range)
        public static decimal? AverageGroupSize(DateTime from, DateTime to)
        {
            object v = Guades_QCUCram_Db.Scalar(
                @"SELECT CAST(AVG(CAST(NumberOfMembers AS FLOAT)) AS DECIMAL(4,1))
                    FROM Reservations WHERE ReservationDate BETWEEN @From AND @To", Range(from, to));
            if (v == null || v == DBNull.Value) return null;
            return Convert.ToDecimal(v);
        }

        // 11. Admin decision breakdown (from the audit trail)
        public static DataTable AdminDecisions(DateTime from, DateTime to)
        {
            return Guades_QCUCram_Db.Query(
                @"SELECT h.NewStatus, COUNT(*) AS Total
                    FROM ReservationStatusHistory h
                    JOIN Reservations r ON r.ReservationID = h.ReservationID
                   WHERE h.ChangedByRole = 'Admin'
                     AND h.NewStatus IN ('Approved','Declined','Reassigned')
                     AND r.ReservationDate BETWEEN @From AND @To
                   GROUP BY h.NewStatus
                   ORDER BY Total DESC", Range(from, to));
        }

        // 12. Top repeat bookers
        public static DataTable TopRepeatBookers(DateTime from, DateTime to)
        {
            return Guades_QCUCram_Db.Query(
                @"SELECT TOP 10 s.StudentNumber, s.LastName + ', ' + s.FirstName AS Student, COUNT(*) AS Bookings
                    FROM Reservations res
                    JOIN Students s ON s.StudentID = res.StudentID
                   WHERE res.ReservationDate BETWEEN @From AND @To
                   GROUP BY s.StudentID, s.StudentNumber, s.LastName, s.FirstName
                   ORDER BY Bookings DESC, s.StudentNumber", Range(from, to));
        }
    }
}