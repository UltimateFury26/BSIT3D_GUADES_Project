<%@ Page Title="Review Queue" Language="C#" MasterPageFile="~/Guades_QCUCram_Site.Master" AutoEventWireup="true" CodeBehind="Guades_QCUCram_Queue.aspx.cs" Inherits="Guades_QCUCram.Guades_QCUCram_Admin.Guades_QCUCram_Queue" %>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div class="qcu-page-head">
        <h2>Review Queue</h2>
        <p>Approve, reassign or decline pending room requests</p>
    </div>

    <asp:Label ID="lblMessage" runat="server" Visible="false"></asp:Label>

    <h5 class="qcu-subhead">Pending requests</h5>
    <p><asp:Literal ID="litCount" runat="server"></asp:Literal></p>

    <div class="table-responsive">
<asp:GridView ID="gvQueue" runat="server" AutoGenerateColumns="false"
        CssClass="table table-bordered table-striped align-middle qcu-admin-table"
        DataKeyNames="ReservationID"
        OnRowCommand="gvQueue_RowCommand"
        EmptyDataText="No pending requests. All caught up.">
        <Columns>
            <asp:BoundField DataField="ReservationID" HeaderText="#" />
            <asp:BoundField DataField="SubmittedText" HeaderText="Submitted" />
            <asp:TemplateField HeaderText="Requester">
                <ItemTemplate><%# Eval("RequesterHtml") %></ItemTemplate>
            </asp:TemplateField>
            <asp:BoundField DataField="RoomName" HeaderText="Room" />
            <asp:TemplateField HeaderText="Date / Time">
                <ItemTemplate><%# Eval("WhenHtml") %></ItemTemplate>
            </asp:TemplateField>
            <asp:BoundField DataField="NumberOfMembers" HeaderText="Members" />
            <asp:TemplateField HeaderText="Purpose">
                <ItemTemplate><%#: Eval("Purpose") %></ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Actions" ItemStyle-CssClass="qcu-actions-cell">
                <ItemTemplate>
                    <div class="qcu-actions">
                    <asp:Button ID="btnApproveRow" runat="server" Text="Approve" CssClass="btn btn-sm btn-success"
                        CommandName="ApproveRes" CommandArgument='<%# Eval("ReservationID") %>'
                        Visible='<%# !(bool)Eval("Overdue") %>' CausesValidation="false"
                        OnClientClick="return qcuConfirm(this, { tone: 'success', title: 'Approve this request?', message: 'The booking is confirmed and the requester is notified.', confirmText: 'Approve' });" />
                    <asp:Button ID="btnReassignRow" runat="server" Text="Reassign" CssClass="btn btn-sm btn-outline-warning"
                        CommandName="ReassignRes" CommandArgument='<%# Eval("ReservationID") %>'
                        Visible='<%# !(bool)Eval("Overdue") %>' CausesValidation="false" />
                    <asp:Button ID="btnReviewRow" runat="server" Text="Review / Decline" CssClass="btn btn-sm btn-outline-primary"
                        CommandName="ReviewRes" CommandArgument='<%# Eval("ReservationID") %>' CausesValidation="false" />
                    </div>
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
    </asp:GridView>
</div>

    <%-- ===== Review / decline (pop-up modal) ===== --%>
    <div class="qcu-form-overlay<%= ReviewModalCss %>">
        <asp:Panel ID="pnlDetails" runat="server" CssClass="qcu-form-modal qcu-form-modal-lg"
            role="dialog" aria-modal="true" aria-labelledby="reviewModalTitle" TabIndex="-1">

            <div class="qcu-form-head">
                <div class="qcu-form-title" id="reviewModalTitle">Review request</div>
                <button type="button" class="qcu-form-close" data-qcu-close aria-label="Close">&times;</button>
            </div>

            <div class="qcu-form-body">
                <asp:Literal ID="litDetails" runat="server"></asp:Literal>

                <div class="mb-3 mt-3">
                    <asp:Label runat="server" AssociatedControlID="txtReason" Text="Reason (required when declining)" CssClass="form-label"></asp:Label>
                    <asp:TextBox ID="txtReason" runat="server" TextMode="MultiLine" Rows="2" MaxLength="250" CssClass="form-control"></asp:TextBox>
                    <div class="form-text">The student sees this reason in their notification. Maximum 250 characters.</div>
                </div>
            </div>

            <div class="qcu-form-foot">
                <asp:Button ID="btnClose" runat="server" Text="Close" CssClass="btn btn-outline-secondary"
                    OnClick="btnClose_Click" CausesValidation="false" />
                <asp:Button ID="btnToReassign" runat="server" Text="Reassign instead" CssClass="btn btn-warning"
                    OnClick="btnToReassign_Click" CausesValidation="false" />
                <asp:Button ID="btnDecline" runat="server" Text="Decline" CssClass="btn btn-danger"
                    OnClick="btnDecline_Click" CausesValidation="false"
                    OnClientClick="return qcuConfirm(this, { tone: 'danger', title: 'Decline this request?', message: 'The requester is notified and sees the reason you give.', confirmText: 'Decline', input: { label: 'Reason (required)', target: '[id$=_txtReason]', required: true, maxLength: 250, rows: 3, requiredMessage: 'Please enter a reason before declining.' } });" />
                <asp:Button ID="btnApprove" runat="server" Text="Approve" CssClass="btn btn-success"
                    OnClick="btnApprove_Click" CausesValidation="false"
                    OnClientClick="return qcuConfirm(this, { tone: 'success', title: 'Approve this request?', message: 'The booking is confirmed and the requester is notified.', confirmText: 'Approve' });" />
            </div>
        </asp:Panel>
    </div>

    <%-- ===== Reassign booking (pop-up modal) ===== --%>
    <div class="qcu-form-overlay<%= ReassignModalCss %>">
        <asp:Panel ID="pnlReassign" runat="server" CssClass="qcu-form-modal qcu-form-modal-lg"
            role="dialog" aria-modal="true" aria-labelledby="reassignModalTitle" TabIndex="-1">

            <div class="qcu-form-head">
                <div class="qcu-form-title" id="reassignModalTitle">Reassign booking</div>
                <button type="button" class="qcu-form-close" data-qcu-close aria-label="Close">&times;</button>
            </div>

            <div class="qcu-form-body">
                <asp:Literal ID="litReassignInfo" runat="server"></asp:Literal>

                <div class="mb-3 mt-3">
                    <asp:Label runat="server" AssociatedControlID="ddlTarget" Text="Move to (open options on the same date only)" CssClass="form-label"></asp:Label>
                    <asp:DropDownList ID="ddlTarget" runat="server" CssClass="form-select"></asp:DropDownList>
                </div>
                <div class="mb-3">
                    <asp:Label runat="server" AssociatedControlID="txtReassignReason" Text="Reason (required)" CssClass="form-label"></asp:Label>
                    <asp:TextBox ID="txtReassignReason" runat="server" TextMode="MultiLine" Rows="2" MaxLength="200" CssClass="form-control"></asp:TextBox>
                    <div class="form-text">The student sees the new room/time and this reason. Maximum 200 characters.</div>
                </div>
            </div>

            <div class="qcu-form-foot">
                <asp:Button ID="btnReassignClose" runat="server" Text="Close" CssClass="btn btn-outline-secondary"
                    OnClick="btnReassignClose_Click" CausesValidation="false" />
                <asp:Button ID="btnDoReassign" runat="server" Text="Confirm reassignment" CssClass="btn btn-warning"
                    OnClick="btnDoReassign_Click" CausesValidation="false"
                    data-qcu-needs="[id$=_ddlTarget]|[id$=_txtReassignReason]"
                    data-qcu-needs-hint="Choose the new room and time, and enter a reason, first."
                    OnClientClick="return qcuConfirm(this, { tone: 'warning', title: 'Reassign this booking?', message: 'The requester is notified of the new room and time, and of your reason.', confirmText: 'Reassign' });" />
            </div>
        </asp:Panel>
    </div>

    <%-- ===== Upcoming approved bookings (reassign candidates) ===== --%>
    <h5 class="qcu-subhead mt-5">Upcoming approved bookings</h5>
    <p class="text-muted small">Approved or reassigned bookings whose slot has not started yet.</p>
    <div class="table-responsive">
<asp:GridView ID="gvUpcoming" runat="server" AutoGenerateColumns="false"
        CssClass="table table-bordered table-striped align-middle qcu-admin-table"
        DataKeyNames="ReservationID"
        OnRowCommand="gvUpcoming_RowCommand"
        EmptyDataText="No upcoming approved bookings.">
        <Columns>
            <asp:BoundField DataField="ReservationID" HeaderText="#" />
            <asp:TemplateField HeaderText="Requester">
                <ItemTemplate><%# Eval("RequesterHtml") %></ItemTemplate>
            </asp:TemplateField>
            <asp:BoundField DataField="RoomName" HeaderText="Room" />
            <asp:TemplateField HeaderText="Date / Time">
                <ItemTemplate><%# Eval("WhenHtml") %></ItemTemplate>
            </asp:TemplateField>
            <asp:BoundField DataField="NumberOfMembers" HeaderText="Members" />
            <asp:TemplateField HeaderText="Status">
                <ItemTemplate><%# Eval("StatusHtml") %></ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Actions" ItemStyle-CssClass="qcu-actions-cell">
                <ItemTemplate>
                    <div class="qcu-actions">
                    <asp:Button ID="btnReassignUp" runat="server" Text="Reassign" CssClass="btn btn-sm btn-outline-warning"
                        CommandName="ReassignRes" CommandArgument='<%# Eval("ReservationID") %>' CausesValidation="false" />
                    </div>
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
    </asp:GridView>
</div>
</asp:Content>