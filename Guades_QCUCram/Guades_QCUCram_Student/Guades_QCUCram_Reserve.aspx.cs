using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;
using System.Web;
using System.Web.UI.WebControls;
using Guades_QCUCram.Helpers;

namespace Guades_QCUCram.Guades_QCUCram_Student
{
    public partial class Guades_QCUCram_Reserve : Guades_QCUCram_StudentPage
    {
        private const string DateFormat = "yyyy-MM-dd";
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        // One row of the member list (public so the Repeater can Eval() it)
        public class MemberRow
        {
            public int Number { get; set; }
            public string LastName { get; set; }
            public string FirstName { get; set; }
            public string MI { get; set; }
        }

        // ------------------------------------------------------------------ page load
        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) return;   // controls keep their values through ViewState

            DataRow me = LoadStudent();
            if (me == null)
            {
                ShowMessage(lblMessage, "Your account could not be found. Please log in again.", true);
                pnlForm.Visible = false;
                return;
            }

            ShowRequester(me);
            ShowRules();
            ShowRestrictionBanner(me);
            LoadRoomsAndSlots();
            ApplyQueryString();

            // Default: 5 members, row 1 pre-filled with the requester
            ddlMembers.SelectedValue = "5";
            var first = new List<MemberRow>();
            first.Add(new MemberRow
            {
                LastName = Convert.ToString(me["LastName"]),
                FirstName = Convert.ToString(me["FirstName"]),
                MI = Convert.ToString(me["MI"])
            });
            BindMembers(5, first);
        }

        // ------------------------------------------------------------------ requester + rules
        private DataRow LoadStudent()
        {
            DataTable dt = Guades_QCUCram_Db.Query(
                @"SELECT StudentNumber, LastName, FirstName, MI, Email, Program, RequesterType,
                         NoShowCount, IsRestricted, RestrictionLiftDate
                    FROM Students WHERE StudentID = @id",
                Guades_QCUCram_Db.P("@id", CurrentUserId));
            return dt.Rows.Count == 0 ? null : dt.Rows[0];
        }

        // Restricted = flag is on AND the cooldown has not passed yet
        private static bool IsRestrictedNow(DataRow me)
        {
            if (!Convert.ToBoolean(me["IsRestricted"])) return false;
            if (me["RestrictionLiftDate"] == DBNull.Value) return true;   // no end date = until lifted manually
            return Convert.ToDateTime(me["RestrictionLiftDate"]) > Guades_QCUCram_Clock.Now;
        }

        private void ShowRequester(DataRow me)
        {
            string mi = Convert.ToString(me["MI"]);
            string fullName = Convert.ToString(me["LastName"]) + ", " + Convert.ToString(me["FirstName"]) +
                              (string.IsNullOrWhiteSpace(mi) ? "" : " " + mi);

            var sb = new StringBuilder();
            sb.Append("<dl class=\"row mb-0\">");
            AppendDt(sb, "Name", fullName);
            AppendDt(sb, "ID / Student number", Convert.ToString(me["StudentNumber"]));
            AppendDt(sb, "Email", Convert.ToString(me["Email"]));
            AppendDt(sb, "Program", Convert.ToString(me["Program"]));
            AppendDt(sb, "Requester type", Convert.ToString(me["RequesterType"]));
            sb.Append("</dl>");
            litRequester.Text = sb.ToString();
        }

        private static void AppendDt(StringBuilder sb, string label, string value)
        {
            sb.Append("<dt class=\"col-sm-3\">").Append(HttpUtility.HtmlEncode(label)).Append("</dt>");
            sb.Append("<dd class=\"col-sm-9\">").Append(HttpUtility.HtmlEncode(value)).Append("</dd>");
        }

        private void ShowRules()
        {
            // Same wording as the "Booking Rules" pop-up on the Availability page
            litRules.Text = Guades_QCUCram_SettingsHelper.BuildRulesHtml();
        }

        private void ShowRestrictionBanner(DataRow me)
        {
            if (!IsRestrictedNow(me)) return;

            string until = me["RestrictionLiftDate"] == DBNull.Value
                ? "further notice"
                : Convert.ToDateTime(me["RestrictionLiftDate"]).ToString("MMM d, yyyy h:mm tt", Inv);

            litRestricted.Text = "<strong>You are restricted from booking until " + HttpUtility.HtmlEncode(until) +
                                 ".</strong> Reason: " + Convert.ToInt32(me["NoShowCount"]) + " forfeited reservation(s).";
            pnlRestricted.Visible = true;
            pnlForm.Enabled = false;   // the form is shown but cannot be used
        }

        // ------------------------------------------------------------------ dropdowns
        private void LoadRoomsAndSlots()
        {
            ddlRoom.Items.Clear();
            ddlRoom.Items.Add(new ListItem("-- Select a room --", ""));
            DataTable rooms = Guades_QCUCram_Db.Query(
                "SELECT RoomID, RoomName, MinCapacity, MaxCapacity FROM Rooms WHERE IsActive = 1 ORDER BY RoomID");
            foreach (DataRow r in rooms.Rows)
            {
                string text = Convert.ToString(r["RoomName"]) + " (" +
                              r["MinCapacity"] + "-" + r["MaxCapacity"] + " persons)";
                ddlRoom.Items.Add(new ListItem(text, Convert.ToString(r["RoomID"])));
            }

            ddlSlot.Items.Clear();
            ddlSlot.Items.Add(new ListItem("-- Select a time slot --", ""));
            DataTable slots = Guades_QCUCram_Db.Query(
                "SELECT TimeSlotID, StartTime, EndTime FROM TimeSlots WHERE IsActive = 1 ORDER BY StartTime");
            foreach (DataRow s in slots.Rows)
            {
                string label = Guades_QCUCram_ReservationService.FormatSlot(
                    (TimeSpan)s["StartTime"], (TimeSpan)s["EndTime"]);
                ddlSlot.Items.Add(new ListItem(label, Convert.ToString(s["TimeSlotID"])));
            }
        }

        // Links from the availability grid arrive as ?room=1&slot=2&date=2026-10-07
        private void ApplyQueryString()
        {
            int ahead = Guades_QCUCram_SettingsHelper.GetInt("AdvanceBookingDays", 1);
            DateTime date = Guades_QCUCram_Clock.Today.AddDays(ahead);

            DateTime fromQuery;
            string q = Request.QueryString["date"];
            if (!string.IsNullOrEmpty(q) &&
                DateTime.TryParseExact(q, DateFormat, Inv, DateTimeStyles.None, out fromQuery))
            {
                date = fromQuery.Date;
            }
            txtDate.Text = date.ToString(DateFormat, Inv);

            SelectIfExists(ddlRoom, Request.QueryString["room"]);
            SelectIfExists(ddlSlot, Request.QueryString["slot"]);
        }

        private static void SelectIfExists(DropDownList ddl, string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            ListItem li = ddl.Items.FindByValue(value);
            if (li != null) ddl.SelectedValue = li.Value;
        }

        // ------------------------------------------------------------------ member rows
        protected void ddlMembers_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Keep what the student already typed, then rebuild with the new count
            List<MemberRow> current = ReadMembers();
            BindMembers(int.Parse(ddlMembers.SelectedValue), current);
        }

        private void BindMembers(int count, List<MemberRow> existing)
        {
            var rows = new List<MemberRow>();
            for (int i = 0; i < count; i++)
            {
                MemberRow r = (existing != null && i < existing.Count) ? existing[i] : new MemberRow();
                r.Number = i + 1;
                rows.Add(r);
            }
            rptMembers.DataSource = rows;
            rptMembers.DataBind();
        }

        private List<MemberRow> ReadMembers()
        {
            var list = new List<MemberRow>();
            foreach (RepeaterItem item in rptMembers.Items)
            {
                if (item.ItemType != ListItemType.Item && item.ItemType != ListItemType.AlternatingItem) continue;

                var txtLast = (TextBox)item.FindControl("txtLast");
                var txtFirst = (TextBox)item.FindControl("txtFirst");
                var txtMI = (TextBox)item.FindControl("txtMI");

                list.Add(new MemberRow
                {
                    Number = list.Count + 1,
                    LastName = txtLast.Text.Trim(),
                    FirstName = txtFirst.Text.Trim(),
                    MI = txtMI.Text.Trim()
                });
            }
            return list;
        }

        // ------------------------------------------------------------------ buttons
        protected void btnReset_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/Guades_QCUCram_Student/Guades_QCUCram_Reserve.aspx");
        }

        // Collects the form, runs every rule in the service, and saves the request.
        protected void btnSubmit_Click(object sender, EventArgs e)
        {
            litPreview.Text = "";
            lblMessage.Visible = false;

            DateTime date;
            bool dateOk = DateTime.TryParseExact((txtDate.Text ?? "").Trim(), DateFormat, Inv,
                                                 DateTimeStyles.None, out date);
            int roomId, slotId, memberCount;
            int.TryParse(ddlRoom.SelectedValue, out roomId);
            int.TryParse(ddlSlot.SelectedValue, out slotId);
            int.TryParse(ddlMembers.SelectedValue, out memberCount);

            var req = new Guades_QCUCram_ReservationService.CreateRequest
            {
                StudentId = CurrentUserId,
                Date = dateOk ? date : DateTime.MinValue,
                RoomId = roomId,
                TimeSlotId = slotId,
                Purpose = txtPurpose.Text.Trim(),
                NumberOfMembers = memberCount,
                Members = new List<Guades_QCUCram_ReservationService.MemberInput>()
            };
            foreach (MemberRow m in ReadMembers())
            {
                req.Members.Add(new Guades_QCUCram_ReservationService.MemberInput
                {
                    LastName = m.LastName,
                    FirstName = m.FirstName,
                    MI = m.MI
                });
            }

            Guades_QCUCram_ReservationService.CreateResult result = null;
            try
            {
                result = Guades_QCUCram_ReservationService.ValidateAndCreate(req);
            }
            catch (Exception)
            {
                ShowMessage(lblMessage, "Something went wrong while saving your request. Please try again.", true);
                return;
            }

            if (!result.Success)
            {
                var sb = new StringBuilder();
                sb.Append("<div class=\"alert alert-danger\"><strong>Please fix the following:</strong><ul class=\"mb-0\">");
                foreach (string err in result.Errors)
                    sb.Append("<li>").Append(HttpUtility.HtmlEncode(err)).Append("</li>");
                sb.Append("</ul></div>");
                litPreview.Text = sb.ToString();
                return;
            }

            // Success -> My Reservations (Step 19 will show a confirmation using ?created=)
            Response.Redirect("~/Guades_QCUCram_Student/Guades_QCUCram_MyReservations.aspx?created=" + result.ReservationId);
        }
    }
}