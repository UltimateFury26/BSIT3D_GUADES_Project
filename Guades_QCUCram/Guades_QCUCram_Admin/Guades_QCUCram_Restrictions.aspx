<%@ Page Title="Restrictions" Language="C#" MasterPageFile="~/Guades_QCUCram_Site.Master" AutoEventWireup="true" CodeBehind="Guades_QCUCram_Restrictions.aspx.cs" Inherits="Guades_QCUCram.Guades_QCUCram_Admin.Guades_QCUCram_Restrictions" %>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">

    <style>
        .print-only { display: none; }

        @media print {
            nav.navbar, footer, hr, .no-print, form > .alert { display: none !important; }
            .print-only { display: block !important; }
            body { font-size: 11pt; background: #fff; }
            .container, .body-content { max-width: 100% !important; width: 100% !important; padding: 0 !important; margin: 0 !important; }
            table tr { page-break-inside: avoid; }
            @page { margin: 14mm; }
        }
    </style>

    <%-- Header that only appears on paper --%>
    <div class="print-only text-center mb-3">
        <h4 class="mb-0">QCU Library &mdash; Collaboration Room Reservation</h4>
        <div>Restricted Students List</div>
        <div class="small">Printed <%= Guades_QCUCram.Helpers.Guades_QCUCram_Clock.Now.ToString("MMMM d, yyyy h:mm tt", System.Globalization.CultureInfo.InvariantCulture) %></div>
    </div>

    <div class="d-flex justify-content-between align-items-start flex-wrap gap-2 no-print">
        <div class="qcu-page-head">
            <h2>Restrictions</h2>
            <p>Students barred from booking, and those one strike away</p>
        </div>
        <button type="button" class="btn btn-outline-primary" onclick="window.print();">Print list</button>
    </div>

    <asp:Label ID="lblMessage" runat="server" Visible="false"></asp:Label>

    <p class="text-muted"><asp:Literal ID="litRule" runat="server"></asp:Literal></p>

    <h5 class="qcu-subhead">Currently restricted students</h5>
    <div class="table-responsive">
<asp:GridView ID="gvRestricted" runat="server" AutoGenerateColumns="false"
        CssClass="table table-bordered table-striped align-middle qcu-admin-table"
        DataKeyNames="StudentID"
        OnRowCommand="gvRestricted_RowCommand"
        EmptyDataText="No students are currently restricted.">
        <Columns>
            <asp:TemplateField HeaderText="Student">
                <ItemTemplate><%# Eval("StudentHtml") %></ItemTemplate>
            </asp:TemplateField>
            <asp:BoundField DataField="NoShowCount" HeaderText="Strikes" />
            <asp:BoundField DataField="UntilText" HeaderText="Restricted until" />
            <asp:BoundField DataField="RemainingText" HeaderText="Time remaining" />
            <asp:TemplateField HeaderText="Action" HeaderStyle-CssClass="no-print" ItemStyle-CssClass="no-print qcu-actions-cell">
                <ItemTemplate>
                    <div class="qcu-actions">
                    <asp:Button ID="btnLift" runat="server" Text="Lift now" CssClass="btn btn-sm btn-outline-danger"
                        CommandName="LiftRes" CommandArgument='<%# Eval("StudentID") %>' CausesValidation="false" />
                    </div>
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
    </asp:GridView>
</div>

    <div class="qcu-form-overlay<%= LiftModalCss %>">
        <asp:Panel ID="pnlLift" runat="server" CssClass="qcu-form-modal"
            role="dialog" aria-modal="true" aria-labelledby="liftModalTitle" TabIndex="-1">

            <div class="qcu-form-head">
                <div class="qcu-form-title" id="liftModalTitle">Lift restriction</div>
                <button type="button" class="qcu-form-close" data-qcu-close aria-label="Close">&times;</button>
            </div>

            <div class="qcu-form-body">
                <asp:Literal ID="litLiftInfo" runat="server"></asp:Literal>
                <div class="mb-3 mt-3">
                    <asp:Label runat="server" AssociatedControlID="txtNote" Text="Note for the student (optional)" CssClass="form-label"></asp:Label>
                    <asp:TextBox ID="txtNote" runat="server" MaxLength="150" CssClass="form-control"></asp:TextBox>
                    <div class="form-text">Lifting resets the student's strike count to 0 and sends them a notification.</div>
                </div>
            </div>

            <div class="qcu-form-foot">
                <asp:Button ID="btnCancelLift" runat="server" Text="Cancel" CssClass="btn btn-outline-secondary"
                    OnClick="btnCancelLift_Click" CausesValidation="false" />
                <asp:Button ID="btnConfirmLift" runat="server" Text="Confirm - lift restriction" CssClass="btn btn-danger"
                    OnClick="btnConfirmLift_Click" CausesValidation="false"
                    OnClientClick="return qcuConfirm(this, { tone: 'warning', title: 'Lift this restriction?', message: 'The student strike count resets to 0 and they are notified that they can book again.', confirmText: 'Lift restriction' });" />
            </div>
        </asp:Panel>
    </div>

    <h5 class="qcu-subhead mt-5">One strike away</h5>
    <p class="text-muted small">Students who are not restricted but will be after their next forfeited booking.</p>
    <div class="table-responsive">
<asp:GridView ID="gvWarning" runat="server" AutoGenerateColumns="false"
        CssClass="table table-bordered table-striped align-middle qcu-admin-table"
        EmptyDataText="Nobody is one strike away from a restriction.">
        <Columns>
            <asp:TemplateField HeaderText="Student">
                <ItemTemplate><%# Eval("StudentHtml") %></ItemTemplate>
            </asp:TemplateField>
            <asp:BoundField DataField="StrikesText" HeaderText="Strikes" />
            <asp:BoundField DataField="LastText" HeaderText="Last forfeited booking" />
        </Columns>
    </asp:GridView>
</div>

    <div class="print-only" style="margin-top:40px;">Library Assistant: ______________________________</div>
</asp:Content>