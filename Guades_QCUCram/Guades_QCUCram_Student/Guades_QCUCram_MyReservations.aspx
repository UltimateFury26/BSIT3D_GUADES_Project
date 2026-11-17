<%@ Page Title="My Reservations" Language="C#" MasterPageFile="~/Guades_QCUCram_Site.Master" AutoEventWireup="true" CodeBehind="Guades_QCUCram_MyReservations.aspx.cs" Inherits="Guades_QCUCram.Guades_QCUCram_Student.Guades_QCUCram_MyReservations" %>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div class="qcu-page-head">
        <h2>My Reservations</h2>
        <p>Track, print or cancel your room requests</p>
    </div>

<asp:UpdatePanel runat="server">
<ContentTemplate>
    <asp:Label ID="lblMessage" runat="server" Visible="false"></asp:Label>

    <div class="qcu-toolbar row g-2 align-items-end mb-3">
        <div class="col-auto">
            <asp:Label runat="server" AssociatedControlID="ddlStatus" Text="Filter by status" CssClass="form-label"></asp:Label>
            <asp:DropDownList ID="ddlStatus" runat="server" CssClass="form-select" AutoPostBack="true"
                OnSelectedIndexChanged="ddlStatus_SelectedIndexChanged">
                <asp:ListItem Value="" Text="All statuses"></asp:ListItem>
                <asp:ListItem>Pending</asp:ListItem>
                <asp:ListItem>Approved</asp:ListItem>
                <asp:ListItem>Reassigned</asp:ListItem>
                <asp:ListItem>CheckedIn</asp:ListItem>
                <asp:ListItem>Completed</asp:ListItem>
                <asp:ListItem>Declined</asp:ListItem>
                <asp:ListItem>Cancelled</asp:ListItem>
                <asp:ListItem>Forfeited</asp:ListItem>
            </asp:DropDownList>
        </div>
        <div class="col-auto">
            <a runat="server" class="btn btn-primary" href="~/Guades_QCUCram_Student/Guades_QCUCram_Reserve.aspx">New reservation</a>
        </div>
    </div>

    <div class="table-responsive">
    <asp:GridView ID="gvReservations" runat="server" AutoGenerateColumns="false"
        CssClass="table table-bordered table-striped align-middle"
        DataKeyNames="ReservationID"
        AllowPaging="true" PageSize="10"
        OnPageIndexChanging="gvReservations_PageIndexChanging"
        OnRowCommand="gvReservations_RowCommand"
        EmptyDataText="You have no reservations to show.">
        <PagerStyle CssClass="qcu-pager" HorizontalAlign="Center" />
        <Columns>
            <asp:BoundField DataField="ReservationID" HeaderText="#" />
            <asp:BoundField DataField="DateText" HeaderText="Date" />
            <asp:BoundField DataField="TimeText" HeaderText="Time" />
            <asp:BoundField DataField="RoomName" HeaderText="Room" />
            <asp:BoundField DataField="NumberOfMembers" HeaderText="Members" />
            <asp:TemplateField HeaderText="Purpose">
                <ItemTemplate><%#: Eval("Purpose") %></ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Status">
                <ItemTemplate><%# StatusBadge(Eval("Status")) %></ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Actions">
                <ItemTemplate>
                    <asp:Button ID="btnCancel" runat="server" Text="Cancel" CssClass="btn btn-sm btn-outline-danger"
                        CommandName="CancelRes" CommandArgument='<%# Eval("ReservationID") %>'
                        Visible='<%# (bool)Eval("CanCancel") %>' CausesValidation="false"
                        OnClientClick="return qcuConfirm(this, { tone: 'warning', title: 'Cancel this reservation?', message: 'The room and time slot are released. This cannot be undone.', confirmText: 'Cancel reservation', cancelText: 'Keep reservation' });" />
                    <a class="btn btn-sm btn-outline-secondary" data-qcu-load="1"
                       data-qcu-load-title='<%# "Reservation #" + Eval("ReservationID") %>'
                       href='<%# ResolveUrl("~/Guades_QCUCram_Reports/Guades_QCUCram_PrintBooking.aspx") + "?id=" + Eval("ReservationID") %>'>Print</a>
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
    </asp:GridView>
    </div>
</ContentTemplate>
</asp:UpdatePanel>
</asp:Content>