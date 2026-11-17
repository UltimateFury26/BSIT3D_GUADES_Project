using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Web;
using System.Web.UI.WebControls;
using Guades_QCUCram.Helpers;

namespace Guades_QCUCram.Guades_QCUCram_Reports
{
    public partial class Guades_QCUCram_Analytics : Guades_QCUCram_AdminPage
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        private const string DateFmt = "yyyy-MM-dd";
        private const string Dash = "\u2014";

        // Which column drives each GridView's bar, and what "100%" means for that bar
        private class BarInfo
        {
            public string Column;
            public double Max;
        }

        private readonly Dictionary<string, BarInfo> _bars = new Dictionary<string, BarInfo>();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                SetLast30Days();
                LoadAnalytics();
            }
        }

        // ------------------------------------------------------------------ buttons
        protected void btnApply_Click(object sender, EventArgs e)
        {
            LoadAnalytics();
        }

        protected void btnLast30_Click(object sender, EventArgs e)
        {
            SetLast30Days();
            LoadAnalytics();
        }

        protected void btnThisMonth_Click(object sender, EventArgs e)
        {
            DateTime today = Guades_QCUCram_Clock.Now.Date;
            DateTime first = new DateTime(today.Year, today.Month, 1);
            DateTime last = first.AddMonths(1).AddDays(-1);
            txtFrom.Text = first.ToString(DateFmt, Inv);
            txtTo.Text = last.ToString(DateFmt, Inv);
            LoadAnalytics();
        }

        protected void btnAllTime_Click(object sender, EventArgs e)
        {
            try
            {
                DataTable dt = Guades_QCUCram_Db.Query(
                    "SELECT MIN(ReservationDate) AS MinD, MAX(ReservationDate) AS MaxD FROM Reservations");

                if (dt.Rows.Count == 0 || dt.Rows[0]["MinD"] == DBNull.Value)
                {
                    SetLast30Days();   // no reservations at all
                }
                else
                {
                    txtFrom.Text = Convert.ToDateTime(dt.Rows[0]["MinD"]).ToString(DateFmt, Inv);
                    txtTo.Text = Convert.ToDateTime(dt.Rows[0]["MaxD"]).ToString(DateFmt, Inv);
                }
            }
            catch (Exception)
            {
                SetLast30Days();
            }
            LoadAnalytics();
        }

        private void SetLast30Days()
        {
            DateTime today = Guades_QCUCram_Clock.Now.Date;
            txtFrom.Text = today.AddDays(-29).ToString(DateFmt, Inv);
            txtTo.Text = today.ToString(DateFmt, Inv);
        }

        // ------------------------------------------------------------------ date range
        private bool TryReadRange(out DateTime from, out DateTime to)
        {
            from = DateTime.MinValue;
            to = DateTime.MinValue;

            if (!DateTime.TryParseExact((txtFrom.Text ?? "").Trim(), DateFmt, Inv, DateTimeStyles.None, out from) ||
                !DateTime.TryParseExact((txtTo.Text ?? "").Trim(), DateFmt, Inv, DateTimeStyles.None, out to))
            {
                ShowError("Please enter both a valid From date and To date.");
                return false;
            }

            if (from > to)
            {
                ShowError("The From date cannot be later than the To date.");
                return false;
            }

            if ((to - from).TotalDays > 3660)
            {
                ShowError("Please choose a range of 10 years or less.");
                return false;
            }

            return true;
        }

        private void ShowError(string text)
        {
            pnlResults.Visible = false;
            ShowMessage(lblMessage, text, true);
        }

        // ------------------------------------------------------------------ load everything
        private void LoadAnalytics()
        {
            lblMessage.Visible = false;

            DateTime from, to;
            if (!TryReadRange(out from, out to)) return;

            try
            {
                lblRange.Text = HttpUtility.HtmlEncode(
                    from.ToString("MMM d, yyyy", Inv) + " to " + to.ToString("MMM d, yyyy", Inv));

                // ---- Summary cards
                lblTotal.Text = Guades_QCUCram_AnalyticsService.TotalBookings(from, to).ToString("N0", Inv);

                DataRow ns = Guades_QCUCram_AnalyticsService.NoShowRate(from, to).Rows[0];
                lblNoShowPct.Text = ns["NoShowPct"] == DBNull.Value
                    ? Dash
                    : Convert.ToDecimal(ns["NoShowPct"], Inv).ToString("0.0", Inv) + "%";
                lblNoShowDetail.Text = Convert.ToString(ns["Forfeited"], Inv) + " forfeited of " +
                                       Convert.ToString(ns["ApprovedOrLater"], Inv) + " approved or later";

                lblRestricted.Text = Guades_QCUCram_AnalyticsService.CurrentlyRestricted().ToString("N0", Inv);

                decimal? avg = Guades_QCUCram_AnalyticsService.AverageGroupSize(from, to);
                lblAvgGroup.Text = avg.HasValue ? avg.Value.ToString("0.0", Inv) : Dash;

                // ---- Room utilization (also feeds the most/least booked cards)
                DataTable rooms = Guades_QCUCram_AnalyticsService.RoomUtilization(from, to);
                if (rooms.Rows.Count > 0 && Num(rooms.Rows[0]["Booked"]) > 0)
                {
                    DataRow most = rooms.Rows[0];
                    DataRow least = rooms.Rows[rooms.Rows.Count - 1];
                    lblMostRoom.Text = HttpUtility.HtmlEncode(Convert.ToString(most["RoomName"])) +
                                       " (" + Convert.ToString(most["Booked"], Inv) + ")";
                    lblLeastRoom.Text = HttpUtility.HtmlEncode(Convert.ToString(least["RoomName"])) +
                                        " (" + Convert.ToString(least["Booked"], Inv) + ")";
                }
                else
                {
                    lblMostRoom.Text = Dash;
                    lblLeastRoom.Text = Dash;
                }

                // ---- Peak slot (also feeds its own table)
                DataTable slots = Guades_QCUCram_AnalyticsService.PeakTimeSlots(from, to);
                if (slots.Rows.Count > 0 && Num(slots.Rows[0]["Total"]) > 0)
                    lblPeakSlot.Text = HttpUtility.HtmlEncode(Convert.ToString(slots.Rows[0]["Slot"])) +
                                       " (" + Convert.ToString(slots.Rows[0]["Total"], Inv) + ")";
                else
                    lblPeakSlot.Text = Dash;

                // ---- Tables
                BindBars(gvStatus, Guades_QCUCram_AnalyticsService.StatusBreakdown(from, to), "Total", 0);
                BindBars(gvMonth, Guades_QCUCram_AnalyticsService.BookingsPerMonth(from, to), "Total", 0);
                BindBars(gvWeekday, Guades_QCUCram_AnalyticsService.BookingsPerWeekday(from, to), "Total", 0);
                BindBars(gvSlot, slots, "Total", 0);
                BindBars(gvRoom, rooms, "UtilizationPct", 100);   // percentage bars: 100% = full bar
                BindBars(gvProgram, Guades_QCUCram_AnalyticsService.BookingsPerProgram(from, to), "Total", 0);
                BindBars(gvDecisions, Guades_QCUCram_AnalyticsService.AdminDecisions(from, to), "Total", 0);

                gvNoShowStudents.DataSource = Guades_QCUCram_AnalyticsService.NoShowsPerStudent(from, to);
                gvNoShowStudents.DataBind();

                gvTopBookers.DataSource = Guades_QCUCram_AnalyticsService.TopRepeatBookers(from, to);
                gvTopBookers.DataBind();

                pnlResults.Visible = true;
            }
            catch (Exception)
            {
                ShowError("Sorry, the analytics could not be loaded. Please try again.");
            }
        }

        // ------------------------------------------------------------------ bars
        // fixedMax = 0 means "the biggest value in this table is a full bar"
        private void BindBars(GridView gv, DataTable dt, string column, double fixedMax)
        {
            double max = fixedMax;
            if (max <= 0)
            {
                foreach (DataRow r in dt.Rows)
                    max = Math.Max(max, Num(r[column]));
            }

            _bars[gv.ID] = new BarInfo { Column = column, Max = max };
            gv.DataSource = dt;
            gv.DataBind();
        }

        // Shared by every GridView that shows a bar
        protected void gvBar_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;

            var gv = (GridView)sender;
            BarInfo info;
            if (!_bars.TryGetValue(gv.ID, out info)) return;

            var lit = e.Row.FindControl("litBar") as Literal;
            var row = e.Row.DataItem as DataRowView;
            if (lit == null || row == null) return;

            double value = Num(row[info.Column]);
            double pct = info.Max > 0 ? Math.Min(100.0, value * 100.0 / info.Max) : 0;

            lit.Text = "<div class=\"bar-track\"><div class=\"bar-fill\" style=\"width:" +
                       pct.ToString("0.#", Inv) + "%\"></div></div>";
        }

        private static double Num(object value)
        {
            if (value == null || value == DBNull.Value) return 0;
            return Convert.ToDouble(value, Inv);
        }
    }
}