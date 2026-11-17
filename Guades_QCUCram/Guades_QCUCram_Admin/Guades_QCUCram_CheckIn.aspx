<%@ Page Title="Check-In" Language="C#" MasterPageFile="~/Guades_QCUCram_Site.Master" AutoEventWireup="true" CodeBehind="Guades_QCUCram_CheckIn.aspx.cs" Inherits="Guades_QCUCram.Guades_QCUCram_Admin.Guades_QCUCram_CheckIn" %>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div class="qcu-page-head">
        <h2>Check-In</h2>
        <p>Verify IDs and confirm arrivals for today's bookings</p>
    </div>

<asp:UpdatePanel runat="server">
<ContentTemplate>
    <asp:Label ID="lblMessage" runat="server" Visible="false"></asp:Label>

    <div class="alert alert-secondary">
        <strong>Before confirming:</strong> verify the physical QCID of every member of the group.
        <br />
        <asp:Literal ID="litNow" runat="server"></asp:Literal>
    </div>

    <div class="qcu-toolbar row g-2 align-items-end mb-3">
        <div class="col-auto">
            <asp:Label runat="server" AssociatedControlID="txtSearch" Text="Search" CssClass="form-label"></asp:Label>
            <asp:TextBox ID="txtSearch" runat="server" CssClass="form-control" data-live-btn="btnSearch" autocomplete="off" placeholder="Reservation #, student number, or last name"></asp:TextBox>
        </div>
        <div class="col-auto">
            <asp:Button ID="btnSearch" runat="server" Text="Search" CssClass="btn btn-primary" OnClick="btnSearch_Click" CausesValidation="false" />
            <asp:Button ID="btnClear" runat="server" Text="Clear" CssClass="btn btn-outline-secondary" OnClick="btnClear_Click" CausesValidation="false" />
            <asp:Button ID="btnRefresh" runat="server" Text="Refresh" CssClass="btn btn-outline-primary" OnClick="btnRefresh_Click" CausesValidation="false" />
        </div>
    </div>

    <h5 class="qcu-subhead">Today's bookings waiting for check-in</h5>
    <div class="table-responsive">
<asp:GridView ID="gvCheckIn" runat="server" AutoGenerateColumns="false"
        CssClass="table table-bordered table-striped align-middle qcu-admin-table"
        DataKeyNames="ReservationID"
        OnRowCommand="gvCheckIn_RowCommand"
        EmptyDataText="No approved bookings are waiting for check-in.">
        <Columns>
            <asp:BoundField DataField="ReservationID" HeaderText="#" />
            <asp:TemplateField HeaderText="Time / Room">
                <ItemTemplate><%# Eval("WhenHtml") %></ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Requester">
                <ItemTemplate><%# Eval("RequesterHtml") %></ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Members">
                <ItemTemplate><%# Eval("MembersHtml") %></ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="State">
                <ItemTemplate><%# Eval("StateHtml") %></ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Action" ItemStyle-CssClass="qcu-actions-cell">
                <ItemTemplate>
                    <div class="qcu-actions">
                    <asp:Button ID="btnCheckIn" runat="server" Text="Confirm Check-In" CssClass="btn btn-sm btn-success"
                        CommandName="CheckInRes" CommandArgument='<%# Eval("ReservationID") %>'
                        Visible='<%# (bool)Eval("CanCheckIn") %>' CausesValidation="false"
                        OnClientClick="return qcuConfirm(this, { tone: 'success', title: 'Confirm check-in?', message: 'Verify the physical QCID of all members before you continue. The check-in time is recorded.', confirmText: 'Confirm check-in', requireCheck: 'I have verified the physical QCID of every member.' });" />
                    </div>
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
    </asp:GridView>
</div>

    <h5 class="qcu-subhead mt-5">Checked in today</h5>
    <div class="table-responsive">
<asp:GridView ID="gvCheckedIn" runat="server" AutoGenerateColumns="false"
        CssClass="table table-bordered table-striped align-middle qcu-admin-table"
        EmptyDataText="Nobody has checked in yet today.">
        <Columns>
            <asp:BoundField DataField="ReservationID" HeaderText="#" />
            <asp:TemplateField HeaderText="Time / Room">
                <ItemTemplate><%# Eval("WhenHtml") %></ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Requester">
                <ItemTemplate><%# Eval("RequesterHtml") %></ItemTemplate>
            </asp:TemplateField>
            <asp:BoundField DataField="CheckedText" HeaderText="Checked in at" />
        </Columns>
    </asp:GridView>
</div>
</ContentTemplate>
</asp:UpdatePanel>
</asp:Content>