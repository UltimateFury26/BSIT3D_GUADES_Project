<%@ Page Title="Notifications" Language="C#" MasterPageFile="~/Guades_QCUCram_Site.Master" AutoEventWireup="true" CodeBehind="Guades_QCUCram_Notifications.aspx.cs" Inherits="Guades_QCUCram.Guades_QCUCram_Student.Guades_QCUCram_Notifications" %>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div class="qcu-page-head">
        <h2>Notifications</h2>
        <p>Updates about your reservations</p>
    </div>

    <asp:Label ID="lblMessage" runat="server" Visible="false"></asp:Label>

    <p><asp:Literal ID="litSummary" runat="server"></asp:Literal></p>

    <div class="qcu-toolbar row g-2 align-items-end mb-3">
        <div class="col-auto">
            <asp:Label runat="server" AssociatedControlID="ddlShow" Text="Show" CssClass="form-label"></asp:Label>
            <asp:DropDownList ID="ddlShow" runat="server" CssClass="form-select" AutoPostBack="true"
                OnSelectedIndexChanged="ddlShow_SelectedIndexChanged">
                <asp:ListItem Value="all" Text="All notifications"></asp:ListItem>
                <asp:ListItem Value="unread" Text="Unread only"></asp:ListItem>
            </asp:DropDownList>
        </div>
        <div class="col-auto">
            <asp:Button ID="btnMarkAll" runat="server" Text="Mark all as read" CssClass="btn btn-outline-primary"
                OnClick="btnMarkAll_Click" CausesValidation="false" />
        </div>
    </div>

    <div class="table-responsive">
    <asp:GridView ID="gvNotifications" runat="server" AutoGenerateColumns="false"
        CssClass="table table-bordered table-striped align-middle"
        DataKeyNames="NotificationID"
        AllowPaging="true" PageSize="15"
        OnPageIndexChanging="gvNotifications_PageIndexChanging"
        OnRowCommand="gvNotifications_RowCommand"
        OnRowDataBound="gvNotifications_RowDataBound"
        EmptyDataText="You have no notifications.">
        <PagerStyle CssClass="qcu-pager" HorizontalAlign="Center" />
        <Columns>
            <asp:TemplateField HeaderText="">
                <ItemTemplate>
                    <%# (bool)Eval("IsRead") ? "<span class=\"badge text-bg-secondary\">Read</span>" : "<span class=\"badge text-bg-primary\">New</span>" %>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Message">
                <ItemTemplate><%#: Eval("Message") %></ItemTemplate>
            </asp:TemplateField>
            <asp:BoundField DataField="WhenText" HeaderText="Received" />
            <asp:TemplateField HeaderText="Actions">
                <ItemTemplate>
                    <asp:Button ID="btnRead" runat="server" Text="Mark as read" CssClass="btn btn-sm btn-outline-secondary"
                        CommandName="MarkRead" CommandArgument='<%# Eval("NotificationID") %>'
                        Visible='<%# !(bool)Eval("IsRead") %>' CausesValidation="false" />
                    <asp:HyperLink ID="lnkView" runat="server" Text="View my reservations" CssClass="btn btn-sm btn-outline-primary"
                        NavigateUrl="~/Guades_QCUCram_Student/Guades_QCUCram_MyReservations.aspx"
                        Visible='<%# Eval("ReservationID") != DBNull.Value %>'></asp:HyperLink>
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
    </asp:GridView>
    </div>
</asp:Content>