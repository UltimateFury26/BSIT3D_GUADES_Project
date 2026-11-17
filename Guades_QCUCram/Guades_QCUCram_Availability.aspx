<%@ Page Title="Room Availability" Language="C#" MasterPageFile="~/Guades_QCUCram_Site.Master" AutoEventWireup="true" CodeBehind="Guades_QCUCram_Availability.aspx.cs" Inherits="Guades_QCUCram.Guades_QCUCram_Availability" %>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div class="qcu-page-head">
        <h2>Room Availability</h2>
        <p>See which rooms and time slots are open</p>
    </div>

<asp:UpdatePanel runat="server">
<ContentTemplate>
    <asp:Label ID="lblMessage" runat="server" Visible="false"></asp:Label>

    <div class="qcu-toolbar row g-2 align-items-end mb-3">
        <div class="col-auto">
            <asp:Label runat="server" AssociatedControlID="txtDate" Text="Date" CssClass="form-label"></asp:Label>
            <asp:TextBox ID="txtDate" runat="server" TextMode="Date" CssClass="form-control" data-live-btn="btnShow"></asp:TextBox>
        </div>
        <div class="col-auto">
            <asp:Button ID="btnShow" runat="server" Text="Show" CssClass="btn btn-primary" OnClick="btnShow_Click" />
        </div>
        <div class="col-auto">
            <asp:Button ID="btnPrev" runat="server" Text="&laquo; Previous day" CssClass="btn btn-outline-secondary" OnClick="btnPrev_Click" />
            <asp:Button ID="btnNext" runat="server" Text="Next day &raquo;" CssClass="btn btn-outline-secondary" OnClick="btnNext_Click" />
        </div>
        <div class="col-auto">
            <asp:Button ID="btnBookingDay" runat="server" Text="Booking day" CssClass="btn btn-outline-primary" OnClick="btnBookingDay_Click" />
        </div>
        <div class="col-auto ms-md-auto">
            <%-- Plain button (not a postback): opens the rules pop-up below --%>
            <button type="button" class="btn btn-outline-secondary" data-bs-toggle="modal" data-bs-target="#rulesModal">
                <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" style="vertical-align:-3px;margin-right:6px"><circle cx="12" cy="12" r="10"/><line x1="12" y1="11" x2="12" y2="17"/><line x1="12" y1="7.5" x2="12.01" y2="7.5"/></svg>Booking Rules
            </button>
        </div>
    </div>

    <asp:Literal ID="litInfo" runat="server"></asp:Literal>
    <div class="qcu-grid"><asp:Literal ID="litGrid" runat="server"></asp:Literal></div>

    <div class="qcu-legend">
        <strong>Legend:</strong>
        <span><i class="qcu-sw sw-open"></i>Open</span>
        <span><i class="qcu-sw sw-pending"></i>Pending (held, awaiting review)</span>
        <span><i class="qcu-sw sw-taken"></i>Taken</span>
        <span><i class="qcu-sw sw-mine"></i>Your request / booking</span>
        <span><i class="qcu-sw sw-closed"></i>Closed (date is before the booking day)</span>
        <span><i class="qcu-sw sw-off"></i>Unavailable (disabled)</span>
    </div>
</ContentTemplate>
</asp:UpdatePanel>

    <%-- ============ Library Booking Rules pop-up (Bootstrap modal: X, Esc and a click outside all close it) ============ --%>
    <div class="modal fade qcu-rules-modal" id="rulesModal" tabindex="-1" aria-labelledby="rulesModalTitle" aria-hidden="true">
        <div class="modal-dialog modal-dialog-centered modal-dialog-scrollable modal-lg">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title" id="rulesModalTitle">Library Booking Rules</h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                </div>
                <div class="modal-body">
                    <%= RulesHtml %>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-primary" data-bs-dismiss="modal">Got it</button>
                </div>
            </div>
        </div>
    </div>
</asp:Content>