<%@ Page Title="Daily Schedule" Language="C#" MasterPageFile="~/Guades_QCUCram_Site.Master" AutoEventWireup="true" CodeBehind="Guades_QCUCram_DailySchedule.aspx.cs" Inherits="Guades_QCUCram.Guades_QCUCram_Reports.Guades_QCUCram_DailySchedule" %>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">

    <style>
        @media print {
            @page { size: landscape; margin: 12mm; }
        }
    </style>

    <div class="qcu-page-head no-print">
        <h2>Daily Schedule</h2>
        <p>Confirmed bookings for the day, ready to print for the front desk</p>
    </div>

<asp:UpdatePanel runat="server">
<ContentTemplate>
    <asp:Label ID="lblMessage" runat="server" Visible="false"></asp:Label>

    <%-- ================= Date picker ================= --%>
    <asp:Panel ID="pnlFilter" runat="server" DefaultButton="btnShow" CssClass="qcu-toolbar mb-3 no-print">
        <div class="row g-3 align-items-end">
            <div class="col-md-3">
                <asp:Label runat="server" AssociatedControlID="txtDate" Text="Date" CssClass="form-label"></asp:Label>
                <asp:TextBox ID="txtDate" runat="server" TextMode="Date" CssClass="form-control" data-live-btn="btnShow"></asp:TextBox>
            </div>
            <div class="col-md-9 d-flex flex-wrap gap-2">
                <asp:Button ID="btnShow" runat="server" Text="Show" CssClass="btn btn-primary"
                    CausesValidation="false" OnClick="btnShow_Click" />
                <asp:Button ID="btnPrev" runat="server" Text="&laquo; Previous day" CssClass="btn btn-outline-secondary"
                    CausesValidation="false" OnClick="btnPrev_Click" />
                <asp:Button ID="btnNext" runat="server" Text="Next day &raquo;" CssClass="btn btn-outline-secondary"
                    CausesValidation="false" OnClick="btnNext_Click" />
                <asp:Button ID="btnToday" runat="server" Text="Today" CssClass="btn btn-outline-secondary"
                    CausesValidation="false" OnClick="btnToday_Click" />
                <asp:Button ID="btnTomorrow" runat="server" Text="Tomorrow" CssClass="btn btn-outline-secondary"
                    CausesValidation="false" OnClick="btnTomorrow_Click" />
                <button type="button" class="btn btn-success" onclick="window.print();">Print</button>
            </div>
        </div>
    </asp:Panel>

    <asp:Panel ID="pnlResults" runat="server">

        <%-- Header that only appears on paper --%>
        <div class="print-only qcu-print-head">
            <img src="<%= ResolveUrl("~/Content/images/qcu-logo.png") %>" alt="" />
            <div>
                <h4>Quezon City University</h4>
                <div>QCU Library &mdash; Collaboration Room Reservation</div>
                <div><strong>Daily Room Schedule</strong></div>
            </div>
        </div>

        <h4 class="qcu-day-title"><asp:Label ID="lblDateTitle" runat="server"></asp:Label></h4>
        <p class="text-muted">
            <asp:Label ID="lblCount" runat="server"></asp:Label>
            <asp:Label ID="lblPending" runat="server" CssClass="no-print"></asp:Label>
        </p>

        <div class="table-responsive">
            <asp:GridView ID="gvSchedule" runat="server" AutoGenerateColumns="false"
                CssClass="table table-bordered table-striped align-middle qcu-admin-table"
                EmptyDataText="No confirmed bookings for this date.">
                <Columns>
                    <asp:BoundField DataField="TimeText" HeaderText="Time" />
                    <asp:BoundField DataField="RoomName" HeaderText="Room" />
                    <asp:TemplateField HeaderText="Requester">
                        <ItemTemplate><%# Eval("RequesterHtml") %></ItemTemplate>
                    </asp:TemplateField>
                    <asp:BoundField DataField="Program" HeaderText="Program" />
                    <asp:BoundField DataField="NumberOfMembers" HeaderText="Members" />
                    <asp:BoundField DataField="Purpose" HeaderText="Purpose" />
                    <asp:TemplateField HeaderText="Status">
                        <ItemTemplate><%# StatusBadge(Eval("Status")) %></ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
        </div>

        <div class="print-only qcu-sign">Library Assistant: ______________________________</div>
        <p class="print-only small text-muted mt-2"><asp:Literal ID="litPrintMeta" runat="server"></asp:Literal></p>
    </asp:Panel>
</ContentTemplate>
</asp:UpdatePanel>
</asp:Content>