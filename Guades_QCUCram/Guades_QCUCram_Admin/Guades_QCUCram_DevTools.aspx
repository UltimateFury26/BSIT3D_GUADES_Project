<%@ Page Title="Dev Tools" Language="C#" MasterPageFile="~/Guades_QCUCram_Site.Master" AutoEventWireup="true" CodeBehind="Guades_QCUCram_DevTools.aspx.cs" Inherits="Guades_QCUCram.Guades_QCUCram_Admin.Guades_QCUCram_DevTools" %>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div class="qcu-page-head">
        <h2>Dev Tools</h2>
        <p>Testing tools - not for normal use</p>
    </div>

    <div class="alert alert-warning">
        Testing tools only. Moving the clock changes how every page decides what is "now".
        <strong>Reset the clock when you finish testing.</strong>
    </div>

    <asp:Label ID="lblMessage" runat="server" Visible="false"></asp:Label>

    <%-- ================= Clock ================= --%>
    <h5 class="qcu-subhead">System clock</h5>
    <asp:Literal ID="litClock" runat="server"></asp:Literal>

    <div class="mb-4">
        <asp:Button runat="server" Text="+15 minutes" CssClass="btn btn-outline-primary"
            CommandName="Shift" CommandArgument="15" OnCommand="btnShift_Command" />
        <asp:Button runat="server" Text="+1 hour" CssClass="btn btn-outline-primary"
            CommandName="Shift" CommandArgument="60" OnCommand="btnShift_Command" />
        <asp:Button runat="server" Text="+1 day" CssClass="btn btn-outline-primary"
            CommandName="Shift" CommandArgument="1440" OnCommand="btnShift_Command" />
        <asp:Button runat="server" Text="+1 week" CssClass="btn btn-outline-primary"
            CommandName="Shift" CommandArgument="10080" OnCommand="btnShift_Command" />
        <asp:Button runat="server" Text="Reset clock" CssClass="btn btn-danger"
            CommandName="Shift" CommandArgument="reset" OnCommand="btnShift_Command" />
    </div>

    <%-- ================= Sweep ================= --%>
    <h5 class="qcu-subhead">Automatic rules</h5>
    <p class="text-muted small">
        Runs: expire unreviewed pending requests, forfeit unclaimed bookings (adds strikes and restrictions),
        complete finished sessions, and lift expired restrictions.
    </p>
    <asp:Button ID="btnSweep" runat="server" Text="Run sweep now" CssClass="btn btn-primary" OnClick="btnSweep_Click" />
    <div class="mt-3 mb-4">
        <asp:Literal ID="litSweepResult" runat="server"></asp:Literal>
    </div>

    <%-- ================= Sample data ================= --%>
    <h5 class="qcu-subhead">Sample data</h5>
    <asp:Literal ID="litCounts" runat="server"></asp:Literal>

    <asp:Literal ID="litSampleResult" runat="server" EnableViewState="false"></asp:Literal>

    <div class="mb-3">
        <asp:Button ID="btnGenStudents" runat="server" Text="Generate 10 sample students" CssClass="btn btn-outline-primary"
            OnClick="btnGenStudents_Click" />
        <span class="text-muted small ms-2">
            Each click adds 10 <strong>new</strong> students (no limit). IDs look real: <code>24-0153</code>
            (Faculty <code>FAC-0123</code>, Visitor <code>VIS-0123</code>). Password for all: <code>Test1234!</code>
        </span>
        <div class="text-muted small mt-1">
            Every batch of 10: 4 clean &middot; 3 one strike away &middot; 2 restricted now &middot; 1 expired restriction
            (the strike limit and restriction days come from Settings).
        </div>
    </div>

    <div class="qcu-toolbar row g-2 align-items-end mb-3">
        <div class="col-auto">
            <asp:Label runat="server" AssociatedControlID="ddlDays" Text="History to generate" CssClass="form-label"></asp:Label>
            <asp:DropDownList ID="ddlDays" runat="server" CssClass="form-select">
                <asp:ListItem Value="14" Text="Last 14 days"></asp:ListItem>
                <asp:ListItem Value="30" Text="Last 30 days" Selected="True"></asp:ListItem>
                <asp:ListItem Value="60" Text="Last 60 days"></asp:ListItem>
            </asp:DropDownList>
        </div>
        <div class="col-auto">
            <asp:Label runat="server" AssociatedControlID="ddlAhead" Text="Days ahead" CssClass="form-label"></asp:Label>
            <asp:DropDownList ID="ddlAhead" runat="server" CssClass="form-select">
                <asp:ListItem Value="14" Text="Next 14 days" Selected="True"></asp:ListItem>
                <asp:ListItem Value="21" Text="Next 21 days"></asp:ListItem>
                <asp:ListItem Value="30" Text="Next 30 days"></asp:ListItem>
            </asp:DropDownList>
        </div>
        <div class="col-auto">
            <asp:Button ID="btnGenRes" runat="server" Text="Generate sample reservations" CssClass="btn btn-outline-primary"
                OnClick="btnGenRes_Click" />
        </div>
    </div>
    <div class="text-muted small mb-3">
        Covers all 8 statuses, every room, past days and at least 14 days ahead. They follow the automatic rules, so the sweep leaves them alone
        (a <em>CheckedIn</em> one needs a slot that is running right now &mdash; use the clock buttons above to move into library hours).
    </div>

    <div class="mb-4">
        <asp:Button ID="btnClearSampleRes" runat="server" Text="Clear sample reservations" CssClass="btn btn-outline-secondary"
            OnClick="btnClearSampleRes_Click"
            OnClientClick="return qcuConfirm(this, { tone: 'warning', title: 'Clear sample reservations?', message: 'Every SAMPLE reservation is deleted. Your own bookings are kept.', confirmText: 'Clear' });" />
        <asp:Button ID="btnDeleteSampleStudents" runat="server" Text="Delete sample students and their data" CssClass="btn btn-outline-secondary"
            OnClick="btnDeleteSampleStudents_Click"
            OnClientClick="return qcuConfirm(this, { tone: 'danger', title: 'Delete sample students?', message: 'Every SAMPLE student and everything that belongs to them is deleted.', confirmText: 'Delete' });" />
    </div>

    <%-- ================= Danger zone ================= --%>
    <div class="card border-danger mb-5">
        <div class="card-header text-danger"><strong>Danger zone</strong></div>
        <div class="card-body">
            <p class="mb-2">
                Deletes <strong>every</strong> reservation (with members and history) and <strong>every</strong> notification,
                and resets all students' strikes and restrictions. Accounts are kept.
            </p>
            <div class="row g-2 align-items-end">
                <div class="col-auto">
                    <asp:Label runat="server" AssociatedControlID="txtConfirmAll" Text="Type DELETE to confirm" CssClass="form-label"></asp:Label>
                    <asp:TextBox ID="txtConfirmAll" runat="server" CssClass="form-control" autocomplete="off"></asp:TextBox>
                </div>
                <div class="col-auto">
                    <asp:Button ID="btnClearAll" runat="server" Text="Clear ALL reservations and notifications" CssClass="btn btn-danger"
                        OnClick="btnClearAll_Click"
                        OnClientClick="return qcuConfirm(this, { tone: 'danger', title: 'Clear ALL reservations?', message: 'This deletes every reservation and every notification, and resets all strikes and restrictions. Accounts are kept.', confirmText: 'Clear everything', requireCheck: 'I understand this cannot be undone.' });" />
                </div>
            </div>
        </div>
    </div>
</asp:Content>