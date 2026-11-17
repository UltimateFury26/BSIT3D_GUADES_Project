<%@ Page Title="Time Slots" Language="C#" MasterPageFile="~/Guades_QCUCram_Site.Master" AutoEventWireup="true" CodeBehind="Guades_QCUCram_TimeSlots.aspx.cs" Inherits="Guades_QCUCram.Guades_QCUCram_Admin.Guades_QCUCram_TimeSlots" %>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
<asp:UpdatePanel runat="server">
<ContentTemplate>

    <div class="qcu-page-bar">
        <div class="qcu-page-head">
            <h2>Time Slots</h2>
            <p>The bookable hours for each day</p>
        </div>
        <asp:Button ID="btnAddNew" runat="server" Text="+ Add Time Slot" CssClass="btn btn-primary qcu-add-btn"
            CausesValidation="false" OnClick="btnAddNew_Click" />
    </div>

    <asp:Label ID="lblMessage" runat="server" Visible="false"></asp:Label>

    <div class="table-responsive">
<asp:GridView ID="gvSlots" runat="server" AutoGenerateColumns="false"
        CssClass="table table-bordered table-striped qcu-admin-table"
        EmptyDataText="No time slots yet. Click &quot;+ Add Time Slot&quot; to create one."
        OnRowCommand="gvSlots_RowCommand">
        <Columns>
            <asp:BoundField DataField="TimeSlotID" HeaderText="ID" />
            <asp:TemplateField HeaderText="Time Slot">
                <ItemTemplate>
                    <%# FormatRange(Eval("StartTime"), Eval("EndTime")) %>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Status">
                <ItemTemplate>
                    <%# Convert.ToBoolean(Eval("IsActive")) ? "<span class=\"badge text-bg-success\">Active</span>" : "<span class=\"badge text-bg-secondary\">Disabled</span>" %>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:BoundField DataField="ResCount" HeaderText="Reservations" />
            <asp:TemplateField HeaderText="Actions" ItemStyle-CssClass="qcu-actions-cell">
                <ItemTemplate>
                    <div class="qcu-actions">
                        <asp:LinkButton runat="server" Text="Edit" CommandName="EditSlot"
                            CommandArgument='<%# Eval("TimeSlotID") %>' CausesValidation="false" CssClass="btn btn-sm btn-outline-primary"></asp:LinkButton>
                        <asp:LinkButton runat="server" Text='<%# Convert.ToBoolean(Eval("IsActive")) ? "Disable" : "Enable" %>' CommandName="ToggleSlot"
                            CommandArgument='<%# Eval("TimeSlotID") %>' CausesValidation="false" CssClass="btn btn-sm btn-outline-secondary"
                            OnClientClick="return qcuConfirm(this, { tone: 'warning', title: '{Action} this time slot?', message: 'A disabled time slot cannot be booked. You can change this again at any time.', confirmText: '{Action}' });"></asp:LinkButton>
                        <asp:LinkButton runat="server" Text="Delete" CommandName="DeleteSlot"
                            CommandArgument='<%# Eval("TimeSlotID") %>' CausesValidation="false" CssClass="btn btn-sm btn-outline-danger"
                            Visible='<%# Convert.ToInt32(Eval("ResCount")) == 0 %>'
                            OnClientClick="return qcuConfirm(this, { tone: 'danger', title: 'Delete this time slot?', message: 'The time slot is removed permanently. This cannot be undone.', confirmText: 'Delete' });"></asp:LinkButton>
                    </div>
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
    </asp:GridView>
</div>
    <p class="text-muted">
        A slot that has reservations cannot be deleted or have its times changed. Disable it instead:
        it disappears from new bookings, but existing reservations still display.
        Slots may touch (for example 9:00-10:00 and 10:00-11:00) but may not overlap.
    </p>

    <%-- ============ Add / Edit Time Slot pop-up ============ --%>
    <div class="qcu-form-overlay<%= ModalOpenCss %>">
        <asp:Panel ID="pnlDialog" runat="server" CssClass="qcu-form-modal" DefaultButton="btnSave"
            role="dialog" aria-modal="true" aria-labelledby="slotModalTitle" TabIndex="-1">

            <div class="qcu-form-head">
                <div class="qcu-form-title" id="slotModalTitle"><asp:Label ID="lblFormTitle" runat="server" Text="Add Time Slot"></asp:Label></div>
                <button type="button" class="qcu-form-close" data-qcu-close aria-label="Close">&times;</button>
            </div>

            <div class="qcu-form-body">
                <asp:Label ID="lblModalMessage" runat="server" Visible="false"></asp:Label>
                <asp:HiddenField ID="hfSlotId" runat="server" Value="" />

                <div class="row">
                    <div class="col-6 mb-3">
                        <asp:Label runat="server" AssociatedControlID="txtStart" Text="Start Time" CssClass="form-label"></asp:Label>
                        <asp:TextBox ID="txtStart" runat="server" CssClass="form-control" TextMode="Time"></asp:TextBox>
                        <asp:RequiredFieldValidator runat="server" ControlToValidate="txtStart" ValidationGroup="Slot"
                            ErrorMessage="Start time is required." CssClass="text-danger" Display="Dynamic"></asp:RequiredFieldValidator>
                    </div>
                    <div class="col-6 mb-3">
                        <asp:Label runat="server" AssociatedControlID="txtEnd" Text="End Time" CssClass="form-label"></asp:Label>
                        <asp:TextBox ID="txtEnd" runat="server" CssClass="form-control" TextMode="Time"></asp:TextBox>
                        <asp:RequiredFieldValidator runat="server" ControlToValidate="txtEnd" ValidationGroup="Slot"
                            ErrorMessage="End time is required." CssClass="text-danger" Display="Dynamic"></asp:RequiredFieldValidator>
                    </div>
                </div>

                <div class="form-check mb-3">
                    <asp:CheckBox ID="chkActive" runat="server" Text="Active (available for booking)" Checked="true" />
                </div>
            </div>

            <div class="qcu-form-foot">
                <button type="button" class="btn btn-outline-secondary" data-qcu-close>Cancel</button>
                <asp:Button ID="btnSave" runat="server" Text="Add Time Slot" CssClass="btn btn-primary"
                    ValidationGroup="Slot" UseSubmitBehavior="false" data-qcu-gate="Slot"
                    OnClientClick="if (typeof(Page_ClientValidate) == 'function' && !Page_ClientValidate('Slot')) { return false; } this.disabled = true; this.value = 'Please wait...';"
                    OnClick="btnSave_Click" />
            </div>
        </asp:Panel>
    </div>

</ContentTemplate>
</asp:UpdatePanel>
</asp:Content>
