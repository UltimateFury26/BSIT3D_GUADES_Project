using System;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI.WebControls;
using Guades_QCUCram.Helpers;

namespace Guades_QCUCram.Guades_QCUCram_Admin
{
    public partial class Guades_QCUCram_Rooms : Guades_QCUCram_AdminPage
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

        // ---------------- Grid ----------------
        private void BindGrid()
        {
            DataTable dt = Guades_QCUCram_Db.Query(
                @"SELECT r.RoomID, r.RoomName, r.MinCapacity, r.MaxCapacity, r.IsActive,
                         (SELECT COUNT(*) FROM Reservations x WHERE x.RoomID = r.RoomID) AS ResCount
                    FROM Rooms r
                   ORDER BY r.RoomID");
            gvRooms.DataSource = dt;
            gvRooms.DataBind();
        }

        protected void gvRooms_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            int roomId;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out roomId)) return;

            try
            {
                switch (e.CommandName)
                {
                    case "EditRoom":
                        LoadForEdit(roomId);
                        break;
                    case "ToggleRoom":
                        ToggleRoom(roomId);
                        break;
                    case "DeleteRoom":
                        DeleteRoom(roomId);
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

        private void LoadForEdit(int roomId)
        {
            DataTable dt = Guades_QCUCram_Db.Query(
                "SELECT RoomID, RoomName, MinCapacity, MaxCapacity, IsActive FROM Rooms WHERE RoomID = @id",
                Guades_QCUCram_Db.P("@id", roomId));
            if (dt.Rows.Count == 0)
            {
                ShowMessage(lblMessage, "That room no longer exists.", true);
                BindGrid();
                return;
            }

            DataRow r = dt.Rows[0];
            lblMessage.Visible = false;
            lblModalMessage.Visible = false;
            hfRoomId.Value = roomId.ToString();
            txtName.Text = r["RoomName"].ToString();
            txtMin.Text = r["MinCapacity"].ToString();
            txtMax.Text = r["MaxCapacity"].ToString();
            chkActive.Checked = Convert.ToBoolean(r["IsActive"]);
            lblFormTitle.Text = "Edit Room #" + roomId;
            btnSave.Text = "Update Room";
            _modalOpen = true;
        }

        // ---------------- Enable / Disable / Delete ----------------
        private void ToggleRoom(int roomId)
        {
            Guades_QCUCram_Db.Execute(
                "UPDATE Rooms SET IsActive = CASE WHEN IsActive = 1 THEN 0 ELSE 1 END WHERE RoomID = @id",
                Guades_QCUCram_Db.P("@id", roomId));

            bool nowActive = Convert.ToBoolean(Guades_QCUCram_Db.Scalar(
                "SELECT IsActive FROM Rooms WHERE RoomID = @id", Guades_QCUCram_Db.P("@id", roomId)));

            if (nowActive)
            {
                ShowMessage(lblMessage, "Room enabled.", false);
            }
            else
            {
                int upcoming = Convert.ToInt32(Guades_QCUCram_Db.Scalar(
                    @"SELECT COUNT(*) FROM Reservations
                       WHERE RoomID = @id AND ReservationDate >= @today
                         AND Status NOT IN ('Declined','Cancelled','Forfeited')",
                    Guades_QCUCram_Db.P("@id", roomId),
                    Guades_QCUCram_Db.P("@today", Guades_QCUCram_Clock.Today)));

                string msg = "Room disabled. It won't be offered for new bookings.";
                if (upcoming > 0)
                    msg += " " + upcoming + " upcoming reservation(s) still use it.";
                ShowMessage(lblMessage, msg, false);
            }

            BindGrid();
        }

        private void DeleteRoom(int roomId)
        {
            int resCount = Convert.ToInt32(Guades_QCUCram_Db.Scalar(
                "SELECT COUNT(*) FROM Reservations WHERE RoomID = @id", Guades_QCUCram_Db.P("@id", roomId)));
            if (resCount > 0)
            {
                ShowMessage(lblMessage,
                    "This room has reservations and cannot be deleted. Disable it instead.", true);
                return;
            }

            try
            {
                Guades_QCUCram_Db.Execute("DELETE FROM Rooms WHERE RoomID = @id", Guades_QCUCram_Db.P("@id", roomId));
            }
            catch (SqlException ex)
            {
                // 547 = foreign key still references the room
                if (ex.Number == 547)
                {
                    ShowMessage(lblMessage,
                        "This room is still in use and cannot be deleted. Disable it instead.", true);
                    return;
                }
                throw;
            }

            ShowMessage(lblMessage, "Room deleted.", false);
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

            string name = txtName.Text.Trim();
            int min, max;

            // Server-side re-checks (browser checks can be bypassed)
            if (name.Length == 0 || name.Length > 50)
            {
                ModalError("Room name is required (maximum 50 characters).");
                return;
            }
            if (!int.TryParse(txtMin.Text.Trim(), out min) || !int.TryParse(txtMax.Text.Trim(), out max)
                || min < 1 || max < 1 || min > 100 || max > 100)
            {
                ModalError("Capacities must be whole numbers from 1 to 100.");
                return;
            }
            if (min > max)
            {
                ModalError("Min capacity cannot be greater than max capacity.");
                return;
            }

            int editId;
            bool isEdit = int.TryParse(hfRoomId.Value, out editId) && editId > 0;
            string done;

            try
            {
                // Unique name (ignoring the room being edited)
                int dup = Convert.ToInt32(Guades_QCUCram_Db.Scalar(
                    "SELECT COUNT(*) FROM Rooms WHERE RoomName = @n AND RoomID <> @id",
                    Guades_QCUCram_Db.P("@n", name),
                    Guades_QCUCram_Db.P("@id", isEdit ? editId : 0)));
                if (dup > 0)
                {
                    ModalError("A room named \"" + name + "\" already exists.");
                    return;
                }

                if (isEdit)
                {
                    int rows = Guades_QCUCram_Db.Execute(
                        @"UPDATE Rooms
                             SET RoomName = @n, MinCapacity = @min, MaxCapacity = @max, IsActive = @a
                           WHERE RoomID = @id",
                        Guades_QCUCram_Db.P("@n", name),
                        Guades_QCUCram_Db.P("@min", min),
                        Guades_QCUCram_Db.P("@max", max),
                        Guades_QCUCram_Db.P("@a", chkActive.Checked),
                        Guades_QCUCram_Db.P("@id", editId));

                    if (rows == 0)
                    {
                        // Room was deleted by someone else: close the pop-up and say so on the page.
                        ClearForm();
                        ShowMessage(lblMessage, "That room no longer exists.", true);
                        BindGrid();
                        return;
                    }
                    done = "Room updated.";
                }
                else
                {
                    Guades_QCUCram_Db.Execute(
                        @"INSERT INTO Rooms (RoomName, MinCapacity, MaxCapacity, IsActive)
                          VALUES (@n, @min, @max, @a)",
                        Guades_QCUCram_Db.P("@n", name),
                        Guades_QCUCram_Db.P("@min", min),
                        Guades_QCUCram_Db.P("@max", max),
                        Guades_QCUCram_Db.P("@a", chkActive.Checked));
                    done = "Room added.";
                }
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2601 || ex.Number == 2627)
                    ModalError("A room with that name already exists.");
                else
                    ModalError("Sorry, the room could not be saved. Please try again.");
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
            hfRoomId.Value = "";
            txtName.Text = "";
            txtMin.Text = "5";
            txtMax.Text = "10";
            chkActive.Checked = true;
            lblFormTitle.Text = "Add Room";
            btnSave.Text = "Add Room";
            lblModalMessage.Visible = false;
        }
    }
}
