<%@ Page Title="Dashboard" Language="C#" MasterPageFile="~/Guades_QCUCram_Site.Master" AutoEventWireup="true" CodeBehind="Guades_QCUCram_Dashboard.aspx.cs" Inherits="Guades_QCUCram.Guades_QCUCram_Student.Guades_QCUCram_Dashboard" %>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div class="qcu-page-head">
        <h2>Student Dashboard</h2>
        <p>Your bookings and room availability at a glance</p>
    </div>

    <asp:Label ID="lblMessage" runat="server" Visible="false"></asp:Label>

    <%-- Restriction banner --%>
    <asp:Panel ID="pnlRestricted" runat="server" Visible="false" CssClass="alert alert-danger">
        <asp:Literal ID="litRestricted" runat="server"></asp:Literal>
    </asp:Panel>

    <%-- Welcome + quick actions --%>
    <div class="qcu-welcome d-flex flex-wrap justify-content-between align-items-center mb-4">
        <p class="lead mb-2 mb-md-0"><asp:Label ID="lblWelcome" runat="server"></asp:Label></p>
        <div>
            <asp:HyperLink ID="hlQuickReserve" runat="server" CssClass="btn btn-cta btn-lg me-2"
                Text="Quick Reserve (tomorrow)"></asp:HyperLink>
            <a runat="server" href="~/Guades_QCUCram_Availability.aspx" class="btn btn-outline-light btn-lg">View Availability</a>
        </div>
    </div>

    <div class="row g-4">
        <%-- ================= Left column: bookings ================= --%>
        <div class="col-lg-7">

            <div class="card mb-4">
                <div class="card-header"><strong>Upcoming bookings</strong></div>
                <div class="card-body">
                    <asp:Literal ID="litUpcoming" runat="server"></asp:Literal>
                </div>
            </div>

            <div class="card mb-4">
                <div class="card-header"><strong>Recent bookings</strong> <span class="text-muted">(last 5 submitted)</span></div>
                <div class="card-body">
                    <asp:Literal ID="litRecent" runat="server"></asp:Literal>
                    <p class="mt-3 mb-0">
                        <a runat="server" href="~/Guades_QCUCram_Student/Guades_QCUCram_MyReservations.aspx">View all my reservations</a>
                    </p>
                </div>
            </div>
        </div>

        <%-- ================= Right column: glance + calendar ================= --%>
        <div class="col-lg-5">

            <div class="card mb-4">
                <div class="card-header"><strong><asp:Label ID="lblGlanceTitle" runat="server"></asp:Label></strong></div>
                <div class="card-body">
                    <asp:Literal ID="litGlance" runat="server"></asp:Literal>
                </div>
            </div>

            <div class="card mb-4">
                <div class="card-header"><strong>My booking calendar</strong></div>
                <div class="card-body">
                    <asp:Calendar ID="calBookings" runat="server" SelectionMode="None"
                        CssClass="qcu-cal table table-bordered text-center mb-2" Width="100%"
                        NextPrevFormat="CustomText" PrevMonthText="&laquo;" NextMonthText="&raquo;"
                        TitleFormat="MonthYear" ShowGridLines="false"
                        OnDayRender="calBookings_DayRender">
                        <TitleStyle Font-Bold="true" BackColor="#00509E" ForeColor="White" />
                        <NextPrevStyle ForeColor="White" Font-Bold="true" />
                        <DayHeaderStyle Font-Bold="true" BackColor="#E8EFF7" ForeColor="#00509E" />
                        <OtherMonthDayStyle ForeColor="#ADB5BD" />
                        <TodayDayStyle Font-Bold="true" BorderColor="#F26A0F" BorderStyle="Solid" BorderWidth="2px" />
                    </asp:Calendar>
                    <div class="small qcu-cal-legend">
                        <span class="badge bg-success">Reserved</span> pending / approved / reassigned &nbsp;
                        <span class="badge bg-primary">Ongoing</span> checked in &nbsp;
                        <span class="badge bg-secondary">Done</span> completed
                    </div>
                    <div class="small text-muted mt-1">Today is outlined in orange. Hover a colored day for details.</div>
                </div>
            </div>
        </div>
    </div>
</asp:Content>