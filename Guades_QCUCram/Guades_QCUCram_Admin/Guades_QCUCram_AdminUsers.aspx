<%@ Page Title="Admin Accounts" Language="C#" MasterPageFile="~/Guades_QCUCram_Site.Master" AutoEventWireup="true" CodeBehind="Guades_QCUCram_AdminUsers.aspx.cs" Inherits="Guades_QCUCram.Guades_QCUCram_Admin.Guades_QCUCram_AdminUsers" %>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
<asp:UpdatePanel runat="server">
<ContentTemplate>

    <div class="qcu-page-bar">
        <div class="qcu-page-head">
            <h2>Admin Accounts</h2>
            <p>Library assistant accounts</p>
        </div>
        <asp:Button ID="btnAddNew" runat="server" Text="+ Add Admin Account" CssClass="btn btn-primary qcu-add-btn"
            CausesValidation="false" OnClick="btnAddNew_Click" />
    </div>

    <asp:Label ID="lblMessage" runat="server" Visible="false"></asp:Label>

    <div class="table-responsive">
<asp:GridView ID="gvAdmins" runat="server" AutoGenerateColumns="false"
        CssClass="table table-bordered table-striped qcu-admin-table"
        EmptyDataText="No admin accounts found."
        OnRowCommand="gvAdmins_RowCommand">
        <Columns>
            <asp:TemplateField HeaderText="Username">
                <ItemTemplate>
                    <%# FormatUsername(Eval("Username"), Eval("AdminID")) %>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:BoundField DataField="FullName" HeaderText="Full Name" />
            <asp:TemplateField HeaderText="Status">
                <ItemTemplate>
                    <%# Convert.ToBoolean(Eval("IsActive")) ? "<span class=\"badge text-bg-success\">Active</span>" : "<span class=\"badge text-bg-secondary\">Disabled</span>" %>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Actions" ItemStyle-CssClass="qcu-actions-cell">
                <ItemTemplate>
                    <div class="qcu-actions">
                        <asp:LinkButton runat="server" Text="Edit" CommandName="EditAdmin"
                            CommandArgument='<%# Eval("AdminID") %>' CausesValidation="false" CssClass="btn btn-sm btn-outline-primary"></asp:LinkButton>
                        <asp:LinkButton runat="server" CommandName="ToggleAdmin"
                            Text='<%# Convert.ToBoolean(Eval("IsActive")) ? "Disable" : "Enable" %>'
                            CommandArgument='<%# Eval("AdminID") %>' CausesValidation="false" CssClass="btn btn-sm btn-outline-secondary"
                            Visible='<%# !IsSelf(Eval("AdminID")) %>'
                            OnClientClick="return qcuConfirm(this, { tone: 'warning', title: '{Action} this account?', message: 'A disabled account cannot sign in. You can change this again at any time.', confirmText: '{Action}' });"></asp:LinkButton>
                        <asp:LinkButton runat="server" Text="Reset Password" CommandName="ResetPassword"
                            CommandArgument='<%# Eval("AdminID") %>' CausesValidation="false" CssClass="btn btn-sm btn-outline-warning"
                            Visible='<%# !IsSelf(Eval("AdminID")) %>'
                            OnClientClick="return qcuConfirm(this, { tone: 'warning', title: 'Reset this password?', message: 'A new temporary password will be generated and shown on this page.', confirmText: 'Reset password' });"></asp:LinkButton>
                    </div>
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
    </asp:GridView>
</div>
    <p class="text-muted">
        You cannot disable your own account, and at least one active admin must always remain.
        Accounts are disabled instead of deleted so the history stays intact. To change your own password, use Change Password.
    </p>

    <%-- ============ Add / Edit Admin pop-up ============ --%>
    <div class="qcu-form-overlay<%= ModalOpenCss %>">
        <asp:Panel ID="pnlDialog" runat="server" CssClass="qcu-form-modal" DefaultButton="btnSave"
            role="dialog" aria-modal="true" aria-labelledby="adminModalTitle" TabIndex="-1">

            <div class="qcu-form-head">
                <div class="qcu-form-title" id="adminModalTitle"><asp:Label ID="lblFormTitle" runat="server" Text="Add Admin Account"></asp:Label></div>
                <button type="button" class="qcu-form-close" data-qcu-close aria-label="Close">&times;</button>
            </div>

            <div class="qcu-form-body">
                <asp:Label ID="lblModalMessage" runat="server" Visible="false"></asp:Label>
                <asp:HiddenField ID="hfAdminId" runat="server" Value="" />

                <%-- Username: only when ADDING (a username cannot be changed afterwards) --%>
                <asp:Panel ID="pnlUsername" runat="server">
                    <div class="mb-3">
                        <asp:Label runat="server" AssociatedControlID="txtUsername" Text="Username" CssClass="form-label"></asp:Label>
                        <asp:TextBox ID="txtUsername" runat="server" CssClass="form-control" MaxLength="50" autocomplete="off"></asp:TextBox>
                        <asp:RequiredFieldValidator runat="server" ControlToValidate="txtUsername" ValidationGroup="Admin"
                            ErrorMessage="Username is required." CssClass="text-danger" Display="Dynamic"></asp:RequiredFieldValidator>
                        <asp:RegularExpressionValidator runat="server" ControlToValidate="txtUsername" ValidationGroup="Admin"
                            ValidationExpression="^[A-Za-z0-9._\-]{3,50}$"
                            ErrorMessage="Use 3-50 letters, numbers, dots, dashes or underscores (no spaces)."
                            CssClass="text-danger" Display="Dynamic"></asp:RegularExpressionValidator>
                    </div>
                </asp:Panel>

                <div class="mb-3">
                    <asp:Label runat="server" AssociatedControlID="txtFullName" Text="Full Name" CssClass="form-label"></asp:Label>
                    <asp:TextBox ID="txtFullName" runat="server" CssClass="form-control" MaxLength="120" autocomplete="off"></asp:TextBox>
                    <asp:RequiredFieldValidator runat="server" ControlToValidate="txtFullName" ValidationGroup="Admin"
                        ErrorMessage="Full name is required." CssClass="text-danger" Display="Dynamic"></asp:RequiredFieldValidator>
                </div>

                <%-- Temporary password: only when ADDING. It is generated automatically (admin1, admin2, ...). --%>
                <asp:Panel ID="pnlPassword" runat="server">
                    <div class="qcu-temp-note">
                        <strong>Temporary password:</strong> <code><%= TempPasswordPreview %></code>
                        <div class="form-text mb-0">Created automatically (admin + the account's number). Give it to the new library assistant and ask them to change it after logging in.</div>
                    </div>
                </asp:Panel>
            </div>

            <div class="qcu-form-foot">
                <button type="button" class="btn btn-outline-secondary" data-qcu-close>Cancel</button>
                <asp:Button ID="btnSave" runat="server" Text="Add Admin" CssClass="btn btn-primary"
                    ValidationGroup="Admin" UseSubmitBehavior="false" data-qcu-gate="Admin"
                    OnClientClick="if (typeof(Page_ClientValidate) == 'function' && !Page_ClientValidate('Admin')) { return false; } this.disabled = true; this.value = 'Please wait...';"
                    OnClick="btnSave_Click" />
            </div>
        </asp:Panel>
    </div>

</ContentTemplate>
</asp:UpdatePanel>
</asp:Content>
