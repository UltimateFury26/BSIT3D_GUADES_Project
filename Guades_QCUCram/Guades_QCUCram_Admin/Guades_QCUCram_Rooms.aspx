<%@ Page Title="Rooms" Language="C#" MasterPageFile="~/Guades_QCUCram_Site.Master" AutoEventWireup="true" CodeBehind="Guades_QCUCram_Rooms.aspx.cs" Inherits="Guades_QCUCram.Guades_QCUCram_Admin.Guades_QCUCram_Rooms" %>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
<asp:UpdatePanel runat="server">
<ContentTemplate>

    <div class="qcu-page-bar">
        <div class="qcu-page-head">
            <h2>Rooms</h2>
            <p>Collaboration rooms available for booking</p>
        </div>
        <asp:Button ID="btnAddNew" runat="server" Text="+ Add Room" CssClass="btn btn-primary qcu-add-btn"
            CausesValidation="false" OnClick="btnAddNew_Click" />
    </div>

    <asp:Label ID="lblMessage" runat="server" Visible="false"></asp:Label>

    <div class="table-responsive">
<asp:GridView ID="gvRooms" runat="server" AutoGenerateColumns="false"
        CssClass="table table-bordered table-striped qcu-admin-table"
        EmptyDataText="No rooms yet. Click &quot;+ Add Room&quot; to create one."
        OnRowCommand="gvRooms_RowCommand">
        <Columns>
            <asp:BoundField DataField="RoomID" HeaderText="ID" />
            <asp:BoundField DataField="RoomName" HeaderText="Room Name" />
            <asp:BoundField DataField="MinCapacity" HeaderText="Min Capacity" />
            <asp:BoundField DataField="MaxCapacity" HeaderText="Max Capacity" />
            <asp:TemplateField HeaderText="Status">
                <ItemTemplate>
                    <%# Convert.ToBoolean(Eval("IsActive")) ? "<span class=\"badge text-bg-success\">Active</span>" : "<span class=\"badge text-bg-secondary\">Disabled</span>" %>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:BoundField DataField="ResCount" HeaderText="Reservations" />
            <asp:TemplateField HeaderText="Actions" ItemStyle-CssClass="qcu-actions-cell">
                <ItemTemplate>
                    <div class="qcu-actions">
                        <asp:LinkButton runat="server" Text="Edit" CommandName="EditRoom"
                            CommandArgument='<%# Eval("RoomID") %>' CausesValidation="false" CssClass="btn btn-sm btn-outline-primary"></asp:LinkButton>
                        <asp:LinkButton runat="server" Text='<%# Convert.ToBoolean(Eval("IsActive")) ? "Disable" : "Enable" %>' CommandName="ToggleRoom"
                            CommandArgument='<%# Eval("RoomID") %>' CausesValidation="false" CssClass="btn btn-sm btn-outline-secondary"
                            OnClientClick="return qcuConfirm(this, { tone: 'warning', title: '{Action} this room?', message: 'A disabled room cannot be booked. You can change this again at any time.', confirmText: '{Action}' });"></asp:LinkButton>
                        <asp:LinkButton runat="server" Text="Delete" CommandName="DeleteRoom"
                            CommandArgument='<%# Eval("RoomID") %>' CausesValidation="false" CssClass="btn btn-sm btn-outline-danger"
                            Visible='<%# Convert.ToInt32(Eval("ResCount")) == 0 %>'
                            OnClientClick="return qcuConfirm(this, { tone: 'danger', title: 'Delete this room?', message: 'The room is removed permanently. This cannot be undone.', confirmText: 'Delete' });"></asp:LinkButton>
                    </div>
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
    </asp:GridView>
</div>
    <p class="text-muted">
        A room that has reservations cannot be deleted. Disable it instead: it disappears from new bookings,
        but existing reservations still display.
    </p>

    <%-- ============ Add / Edit Room pop-up ============ --%>
    <div class="qcu-form-overlay<%= ModalOpenCss %>">
        <asp:Panel ID="pnlDialog" runat="server" CssClass="qcu-form-modal" DefaultButton="btnSave"
            role="dialog" aria-modal="true" aria-labelledby="roomModalTitle" TabIndex="-1">

            <div class="qcu-form-head">
                <div class="qcu-form-title" id="roomModalTitle"><asp:Label ID="lblFormTitle" runat="server" Text="Add Room"></asp:Label></div>
                <button type="button" class="qcu-form-close" data-qcu-close aria-label="Close">&times;</button>
            </div>

            <div class="qcu-form-body">
                <asp:Label ID="lblModalMessage" runat="server" Visible="false"></asp:Label>
                <asp:HiddenField ID="hfRoomId" runat="server" Value="" />

                <div class="mb-3">
                    <asp:Label runat="server" AssociatedControlID="txtName" Text="Room Name" CssClass="form-label"></asp:Label>
                    <asp:TextBox ID="txtName" runat="server" CssClass="form-control" MaxLength="50" autocomplete="off"></asp:TextBox>
                    <asp:RequiredFieldValidator runat="server" ControlToValidate="txtName" ValidationGroup="Room"
                        ErrorMessage="Room name is required." CssClass="text-danger" Display="Dynamic"></asp:RequiredFieldValidator>
                </div>

                <div class="row">
                    <div class="col-6 mb-3">
                        <asp:Label runat="server" AssociatedControlID="txtMin" Text="Min Capacity" CssClass="form-label"></asp:Label>
                        <asp:TextBox ID="txtMin" runat="server" CssClass="form-control" MaxLength="3" autocomplete="off"></asp:TextBox>
                        <asp:RequiredFieldValidator runat="server" ControlToValidate="txtMin" ValidationGroup="Room"
                            ErrorMessage="Min capacity is required." CssClass="text-danger" Display="Dynamic"></asp:RequiredFieldValidator>
                        <asp:RangeValidator runat="server" ControlToValidate="txtMin" ValidationGroup="Room" Type="Integer" MinimumValue="1" MaximumValue="100"
                            ErrorMessage="Min capacity must be a whole number from 1 to 100." CssClass="text-danger" Display="Dynamic"></asp:RangeValidator>
                    </div>
                    <div class="col-6 mb-3">
                        <asp:Label runat="server" AssociatedControlID="txtMax" Text="Max Capacity" CssClass="form-label"></asp:Label>
                        <asp:TextBox ID="txtMax" runat="server" CssClass="form-control" MaxLength="3" autocomplete="off"></asp:TextBox>
                        <asp:RequiredFieldValidator runat="server" ControlToValidate="txtMax" ValidationGroup="Room"
                            ErrorMessage="Max capacity is required." CssClass="text-danger" Display="Dynamic"></asp:RequiredFieldValidator>
                        <asp:RangeValidator runat="server" ControlToValidate="txtMax" ValidationGroup="Room" Type="Integer" MinimumValue="1" MaximumValue="100"
                            ErrorMessage="Max capacity must be a whole number from 1 to 100." CssClass="text-danger" Display="Dynamic"></asp:RangeValidator>
                        <asp:CompareValidator runat="server" ControlToValidate="txtMax" ControlToCompare="txtMin" ValidationGroup="Room"
                            Operator="GreaterThanEqual" Type="Integer"
                            ErrorMessage="Max capacity must be at least the min capacity." CssClass="text-danger" Display="Dynamic"></asp:CompareValidator>
                    </div>
                </div>

                <div class="form-check mb-3">
                    <asp:CheckBox ID="chkActive" runat="server" Text="Active (available for booking)" Checked="true" />
                </div>
            </div>

            <div class="qcu-form-foot">
                <button type="button" class="btn btn-outline-secondary" data-qcu-close>Cancel</button>
                <asp:Button ID="btnSave" runat="server" Text="Add Room" CssClass="btn btn-primary"
                    ValidationGroup="Room" UseSubmitBehavior="false" data-qcu-gate="Room"
                    OnClientClick="if (typeof(Page_ClientValidate) == 'function' && !Page_ClientValidate('Room')) { return false; } this.disabled = true; this.value = 'Please wait...';"
                    OnClick="btnSave_Click" />
            </div>
        </asp:Panel>
    </div>

</ContentTemplate>
</asp:UpdatePanel>
</asp:Content>
