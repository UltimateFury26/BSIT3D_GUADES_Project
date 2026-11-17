<%@ Page Title="Admin Dashboard" Language="C#" MasterPageFile="~/Guades_QCUCram_Site.Master" AutoEventWireup="true" CodeBehind="Guades_QCUCram_Dashboard.aspx.cs" Inherits="Guades_QCUCram.Guades_QCUCram_Admin.Guades_QCUCram_Dashboard" %>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div class="qcu-page-head">
        <h2>Admin Dashboard</h2>
        <p>Today at a glance</p>
    </div>

    <asp:Label ID="lblMessage" runat="server" Visible="false"></asp:Label>

    <div class="qcu-welcome mb-4">
        <p class="lead mb-1"><asp:Label ID="lblWelcome" runat="server"></asp:Label></p>
        <p class="mb-0 small qcu-welcome-sub"><asp:Label ID="lblClock" runat="server"></asp:Label></p>
    </div>

    <%-- Six count cards; each one is a link to the page where that work gets done --%>
    <div class="row g-3 mb-4">
        <asp:Literal ID="litCards" runat="server"></asp:Literal>
    </div>

    <div class="card mb-4">
        <div class="card-header"><strong>Next 5 upcoming bookings</strong></div>
        <div class="card-body">
            <asp:Literal ID="litUpcoming" runat="server"></asp:Literal>
            <p class="mt-3 mb-0">
                <a runat="server" href="~/Guades_QCUCram_Admin/Guades_QCUCram_Queue.aspx" class="me-3">Open review queue</a>
                <a runat="server" href="~/Guades_QCUCram_Admin/Guades_QCUCram_CheckIn.aspx">Open check-in</a>
            </p>
        </div>
    </div>
</asp:Content>