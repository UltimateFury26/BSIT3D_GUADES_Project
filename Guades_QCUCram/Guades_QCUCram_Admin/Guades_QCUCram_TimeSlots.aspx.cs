using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Web.UI.WebControls;
using Guades_QCUCram.Helpers;

namespace Guades_QCUCram.Guades_QCUCram_Admin
{
    public partial class Guades_QCUCram_TimeSlots : Guades_QCUCram_AdminPage
    {
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
                ClearForm();
            }
        }

        // ---------------- Display helpers ----------------
        // Used by the grid: "8:00 AM – 9:00 AM"
        protected string FormatRange(object start, object end)
        {
            return FormatTime((TimeSpan)start) + " \u2013 " + FormatTime((TimeSpan)end);
        }

        private static string FormatTime(TimeSpan t)
        {
            return DateTime.Today.Add(t).ToString("h:mm tt", CultureInfo.InvariantCulture);
        }

        // Accepts "08:00", "8:00", "08:00:00" (browser time box) and "8:00 AM".
        private static bool TryParseTime(string text, out TimeSpan result)
        {
            result = TimeSpan.Zero;
            DateTime dt;
            if (DateTime.TryParseExact((text ?? "").Trim(),
                    new[] { "H:mm", "H:mm:ss", "h:mm tt" },
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
            {
                result = new TimeSpan(dt.Hour, dt.Minute, 0);
                return true;
            }
            return false;
        }

        // ---------------- Grid ----------------
        private void BindGrid()
        {
            DataTable dt = Guades_QCUCram_Db.Query(
                @"SELECT t.TimeSlotID, t.StartTime, t.EndTime, t.IsActive,
                         (SELECT COUNT(*) FROM Reservations x WHERE x.TimeSlotID = t.TimeSlotID) AS ResCount
                    FROM TimeSlots t
                   ORDER BY t.StartTime");
            gvSlots.DataSource = dt;
            gvSlots.DataBind();
        }

        protected void gvSlots_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            int slotId;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out slotId)) return;

            try
            {
                switch (e.CommandName)
                {
                    case "EditSlot":
                        LoadForEdit(slotId);
                        break;
                    case "ToggleSlot":
                        ToggleSlot(slotId);
                        break;
                    case "DeleteSlot":
                        DeleteSlot(slotId);
                        break;
                }
            }
            catch (Exception)
            {
                ShowMessage(lblMessage, "Sorry, that action could not be completed. Please try again.", true);
            }
        }

        // ---------------- Pop-up: open ----------------
        protected void btnAddNew_Click(object sender, EventArgs e)
        {
            lblMessage.Visible = false;
            ClearForm();
            _modalOpen = true;
        }

        private void LoadForEdit(int slotId)
        {
            DataTable dt = Guades_QCUCram_Db.Query(
                "SELECT TimeSlotID, StartTime, EndTime, IsActive FROM TimeSlots WHERE TimeSlotID = @id",
                Guades_QCUCram_Db.P("@id", slotId));
            if (dt.Rows.Count == 0)
            {
                ShowMessage(lblMessage, "That time slot no longer exists.", true);
                BindGrid();
                return;
            }

            DataRow r = dt.Rows[0];
            lblMessage.Visible = false;
            lblModalMessage.Visible = false;
            hfSlotId.Value = slotId.ToString();
            txtStart.Text = ((TimeSpan)r["StartTime"]).ToString(@"hh\:mm");
            txtEnd.Text = ((TimeSpan)r["EndTime"]).ToString(@"hh\:mm");
            chkActive.Checked = Convert.ToBoolean(r["IsActive"]);
            lblFormTitle.Text = "Edit Time Slot #" + slotId;
            btnSave.Text = "Update Time Slot";
            _modalOpen = true;
        }

        private void ToggleSlot(int slotId)
        {
            Guades_QCUCram_Db.Execute(
                "UPDATE TimeSlots SET IsActive = CASE WHEN IsActive = 1 THEN 0 ELSE 1 END WHERE TimeSlotID = @id",
                Guades_QCUCram_Db.P("@id", slotId));

            bool nowActive = Convert.ToBoolean(Guades_QCUCram_Db.Scalar(
                "SELECT IsActive FROM TimeSlots WHERE TimeSlotID = @id", Guades_QCUCram_Db.P("@id", slotId)));

            if (nowActive)
            {
                ShowMessage(lblMessage, "Time slot enabled.", false);
            }
            else
            {
                int upcoming = Convert.ToInt32(Guades_QCUCram_Db.Scalar(
                    @"SELECT COUNT(*) FROM Reservations
                       WHERE TimeSlotID = @id AND ReservationDate >= @today
                         AND Status NOT IN ('Declined','Cancelled','Forfeited')",
                    Guades_QCUCram_Db.P("@id", slotId),
                    Guades_QCUCram_Db.P("@today", Guades_QCUCram_Clock.Today)));

                string msg = "Time slot disabled. It won't be offered for new bookings.";
                if (upcoming > 0)
                    msg += " " + upcoming + " upcoming reservation(s) still use it.";
                ShowMessage(lblMessage, msg, false);
            }

            BindGrid();
        }

        private void DeleteSlot(int slotId)
        {
            int resCount = Convert.ToInt32(Guades_QCUCram_Db.Scalar(
                "SELECT COUNT(*) FROM Reservations WHERE TimeSlotID = @id", Guades_QCUCram_Db.P("@id", slotId)));
            if (resCount > 0)
            {
                ShowMessage(lblMessage,
                    "This time slot has reservations and cannot be deleted. Disable it instead.", true);
                return;
            }

            try
            {
                Guades_QCUCram_Db.Execute("DELETE FROM TimeSlots WHERE TimeSlotID = @id", Guades_QCUCram_Db.P("@id", slotId));
            }
            catch (SqlException ex)
            {
                // 547 = foreign key still references the slot
                if (ex.Number == 547)
                {
                    ShowMessage(lblMessage,
                        "This time slot is still in use and cannot be deleted. Disable it instead.", true);
                    return;
                }
                throw;
            }

            ShowMessage(lblMessage, "Time slot deleted.", false);
            BindGrid();
        }

        // ---------------- Pop-up: save ----------------
        // A problem found while saving keeps the pop-up open and is shown inside it.
        private void ModalError(string message)
        {
            ShowMessage(lblModalMessage, message, true);
            _modalOpen = true;
        }

        protected void btnSave_Click(object sender, EventArgs e)
        {
            lblModalMessage.Visible = false;

            if (!Page.IsValid)
            {
                _modalOpen = true;      // the validators show their own messages
                return;
            }

            TimeSpan start, end;

            // Server-side re-checks (browser checks can be bypassed)
            if (!TryParseTime(txtStart.Text, out start) || !TryParseTime(txtEnd.Text, out end))
            {
                ModalError("Enter valid start and end times (for example 08:00 and 09:00).");
                return;
            }
            if (end <= start)
            {
                ModalError("End time must be later than start time.");
                return;
            }

            int editId;
            bool isEdit = int.TryParse(hfSlotId.Value, out editId) && editId > 0;
            int idForChecks = isEdit ? editId : 0;
            string done;

            try
            {
                // 1) Duplicate start time (ignoring the slot being edited)
                int dup = Convert.ToInt32(Guades_QCUCram_Db.Scalar(
                    "SELECT COUNT(*) FROM TimeSlots WHERE StartTime = @s AND TimeSlotID <> @id",
                    Guades_QCUCram_Db.P("@s", start),
                    Guades_QCUCram_Db.P("@id", idForChecks)));
                if (dup > 0)
                {
                    ModalError("A time slot starting at " + FormatTime(start) + " already exists.");
                    return;
                }

                // 2) Overlap with any other slot (touching edges are allowed)
                DataTable overlap = Guades_QCUCram_Db.Query(
                    @"SELECT TOP 1 StartTime, EndTime FROM TimeSlots
                       WHERE StartTime < @e AND EndTime > @s AND TimeSlotID <> @id
                       ORDER BY StartTime",
                    Guades_QCUCram_Db.P("@s", start),
                    Guades_QCUCram_Db.P("@e", end),
                    Guades_QCUCram_Db.P("@id", idForChecks));
                if (overlap.Rows.Count > 0)
                {
                    ModalError("This time overlaps with the existing slot " +
                        FormatRange(overlap.Rows[0]["StartTime"], overlap.Rows[0]["EndTime"]) + ".");
                    return;
                }

                if (isEdit)
                {
                    DataTable current = Guades_QCUCram_Db.Query(
                        "SELECT StartTime, EndTime FROM TimeSlots WHERE TimeSlotID = @id",
                        Guades_QCUCram_Db.P("@id", editId));
                    if (current.Rows.Count == 0)
                    {
                        // Slot was deleted by someone else: close the pop-up and say so on the page.
                        ClearForm();
                        ShowMessage(lblMessage, "That time slot no longer exists.", true);
                        BindGrid();
                        return;
                    }

                    // Changing the times of a slot that already has bookings would silently
                    // move those bookings, so it is blocked. Enabling/disabling is still allowed.
                    bool timesChanged = (TimeSpan)current.Rows[0]["StartTime"] != start
                                     || (TimeSpan)current.Rows[0]["EndTime"] != end;
                    if (timesChanged)
                    {
                        int resCount = Convert.ToInt32(Guades_QCUCram_Db.Scalar(
                            "SELECT COUNT(*) FROM Reservations WHERE TimeSlotID = @id",
                            Guades_QCUCram_Db.P("@id", editId)));
                        if (resCount > 0)
                        {
                            ModalError("This slot has reservations, so its times cannot be changed. " +
                                "Disable it and add a new slot instead.");
                            return;
                        }
                    }

                    Guades_QCUCram_Db.Execute(
                        @"UPDATE TimeSlots
                             SET StartTime = @s, EndTime = @e, IsActive = @a
                           WHERE TimeSlotID = @id",
                        Guades_QCUCram_Db.P("@s", start),
                        Guades_QCUCram_Db.P("@e", end),
                        Guades_QCUCram_Db.P("@a", chkActive.Checked),
                        Guades_QCUCram_Db.P("@id", editId));
                    done = "Time slot updated.";
                }
                else
                {
                    Guades_QCUCram_Db.Execute(
                        @"INSERT INTO TimeSlots (StartTime, EndTime, IsActive)
                          VALUES (@s, @e, @a)",
                        Guades_QCUCram_Db.P("@s", start),
                        Guades_QCUCram_Db.P("@e", end),
                        Guades_QCUCram_Db.P("@a", chkActive.Checked));
                    done = "Time slot added.";
                }
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2601 || ex.Number == 2627)
                    ModalError("A time slot with that start time already exists.");
                else
                    ModalError("Sorry, the time slot could not be saved. Please try again.");
                return;
            }

            // Success: close the pop-up, refresh the grid, message at the top of the page.
            ClearForm();
            ShowMessage(lblMessage, done, false);
            BindGrid();
        }

        // Back to "Add" defaults and close the pop-up.
        private void ClearForm()
        {
            _modalOpen = false;
            hfSlotId.Value = "";
            txtStart.Text = "";
            txtEnd.Text = "";
            chkActive.Checked = true;
            lblFormTitle.Text = "Add Time Slot";
            btnSave.Text = "Add Time Slot";
            lblModalMessage.Visible = false;
        }
    }
}
