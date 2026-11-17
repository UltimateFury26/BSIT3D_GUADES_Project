<%@ Page Title="Students" Language="C#" MasterPageFile="~/Guades_QCUCram_Site.Master" AutoEventWireup="true" CodeBehind="Guades_QCUCram_Students.aspx.cs" Inherits="Guades_QCUCram.Guades_QCUCram_Admin.Guades_QCUCram_Students" %>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div class="qcu-page-head">
        <h2>Students</h2>
        <p>Manage student accounts</p>
    </div>

<asp:UpdatePanel runat="server">
<ContentTemplate>
    <asp:Label ID="lblMessage" runat="server" Visible="false"></asp:Label>

    <%-- Search box on the left, "+ Add Student" on the right --%>
    <div class="qcu-page-bar qcu-page-bar-search">
        <asp:Panel ID="pnlSearch" runat="server" DefaultButton="btnSearch" CssClass="qcu-toolbar">
            <div class="input-group">
                <asp:TextBox ID="txtSearch" runat="server" CssClass="form-control" MaxLength="100" data-live-btn="btnSearch" autocomplete="off"
                    placeholder="Search by number, name, email, or program"></asp:TextBox>
                <asp:Button ID="btnSearch" runat="server" Text="Search" CssClass="btn btn-primary"
                    CausesValidation="false" OnClick="btnSearch_Click" />
                <asp:Button ID="btnClearSearch" runat="server" Text="Clear" CssClass="btn btn-outline-secondary"
                    CausesValidation="false" OnClick="btnClearSearch_Click" />
            </div>
        </asp:Panel>
        <asp:Button ID="btnAddNew" runat="server" Text="+ Add Student" CssClass="btn btn-primary qcu-add-btn"
            CausesValidation="false" OnClick="btnAddNew_Click" />
    </div>
    <p class="text-muted"><asp:Label ID="lblCount" runat="server"></asp:Label></p>

    <div class="table-responsive">
<asp:GridView ID="gvStudents" runat="server" AutoGenerateColumns="false"
        CssClass="table table-bordered table-striped qcu-admin-table qcu-compact"
        EmptyDataText="No students found."
        AllowPaging="true" PageSize="10"
        OnPageIndexChanging="gvStudents_PageIndexChanging"
        OnRowCommand="gvStudents_RowCommand">
        <PagerStyle CssClass="qcu-pager" HorizontalAlign="Center" />
        <Columns>
            <asp:BoundField DataField="StudentNumber" HeaderText="ID / Student No." />
            <asp:BoundField DataField="FullName" HeaderText="Name" />
            <asp:BoundField DataField="Email" HeaderText="Email" />
            <asp:BoundField DataField="Program" HeaderText="Program" />
            <asp:BoundField DataField="RequesterType" HeaderText="Type" />
            <asp:BoundField DataField="NoShowCount" HeaderText="No-Shows" />
            <asp:TemplateField HeaderText="Restricted">
                <ItemTemplate>
                    <%# FormatRestricted(Eval("IsRestricted"), Eval("RestrictionLiftDate")) %>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:BoundField DataField="ResCount" HeaderText="Reservations" />
            <asp:TemplateField HeaderText="Actions" ItemStyle-CssClass="qcu-actions-cell qcu-actions-narrow">
                <ItemTemplate>
                    <div class="qcu-actions qcu-actions-stack">
                        <asp:LinkButton runat="server" Text="Edit" CommandName="EditStudent"
                            CommandArgument='<%# Eval("StudentID") %>' CausesValidation="false" CssClass="btn btn-sm btn-outline-primary"></asp:LinkButton>
                        <asp:LinkButton runat="server" Text="Reset Password" CommandName="ResetPassword"
                            CommandArgument='<%# Eval("StudentID") %>' CausesValidation="false" CssClass="btn btn-sm btn-outline-warning"
                            OnClientClick="return qcuConfirm(this, { tone: 'warning', title: 'Reset this password?', message: 'A new temporary password will be generated and shown on this page. The student uses it to sign in.', confirmText: 'Reset password' });"></asp:LinkButton>
                        <asp:LinkButton runat="server" Text="Delete" CommandName="DeleteStudent"
                            CommandArgument='<%# Eval("StudentID") %>' CausesValidation="false" CssClass="btn btn-sm btn-outline-danger"
                            Visible='<%# Convert.ToInt32(Eval("ResCount")) == 0 %>'
                            OnClientClick="return qcuConfirm(this, { tone: 'danger', title: 'Delete this student?', message: 'The account is removed permanently. This cannot be undone.', confirmText: 'Delete' });"></asp:LinkButton>
                    </div>
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
    </asp:GridView>
</div>
    <p class="text-muted">
        Students can sign themselves up on the Register page, or you can add an account here (a temporary password is created for you).
        Use this page to correct details, reset a forgotten password, or remove an account.
        An account that has reservations cannot be deleted. Restrictions are lifted on the Restrictions page.
    </p>

    <%-- ============ Add / Edit Student pop-up ============ --%>
    <div class="qcu-form-overlay<%= ModalOpenCss %>">
        <asp:Panel ID="pnlDialog" runat="server" CssClass="qcu-form-modal qcu-form-modal-lg" DefaultButton="btnSave"
            role="dialog" aria-modal="true" aria-labelledby="studentModalTitle" TabIndex="-1">

            <div class="qcu-form-head">
                <div class="qcu-form-title" id="studentModalTitle"><asp:Label ID="lblFormTitle" runat="server" Text="Add Student"></asp:Label></div>
                <button type="button" class="qcu-form-close" data-qcu-close aria-label="Close">&times;</button>
            </div>

            <div class="qcu-form-body">
                <asp:Label ID="lblModalMessage" runat="server" Visible="false"></asp:Label>
                <asp:HiddenField ID="hfStudentId" runat="server" Value="" />

                <div class="row">
                    <div class="col-md-6 mb-3">
                        <asp:Label runat="server" AssociatedControlID="ddlType" Text="Requester Type" CssClass="form-label"></asp:Label>
                        <asp:DropDownList ID="ddlType" runat="server" CssClass="form-select"></asp:DropDownList>
                        <asp:RequiredFieldValidator runat="server" ControlToValidate="ddlType" ValidationGroup="Student" InitialValue=""
                            ErrorMessage="Please select a requester type." CssClass="text-danger" Display="Dynamic"></asp:RequiredFieldValidator>
                    </div>
                    <div class="col-md-6 mb-3">
                        <asp:Label ID="lblNumberCaption" runat="server" AssociatedControlID="txtNumber" Text="ID / Student Number" CssClass="form-label"></asp:Label>
                        <asp:TextBox ID="txtNumber" runat="server" CssClass="form-control" MaxLength="20" autocomplete="off"
                            data-qcu-idstrict="1"></asp:TextBox>
                        <asp:RequiredFieldValidator runat="server" ControlToValidate="txtNumber" ValidationGroup="Student"
                            ErrorMessage="ID / Student Number is required." CssClass="text-danger" Display="Dynamic"></asp:RequiredFieldValidator>
                        <asp:CustomValidator runat="server" ControlToValidate="txtNumber" ValidationGroup="Student" ValidateEmptyText="false"
                            ClientValidationFunction="qcuValidateModalId"
                            ErrorMessage="Enter a valid ID number." CssClass="text-danger" Display="Dynamic"></asp:CustomValidator>
                        <div class="form-text" id="hintStudentNumber"></div>
                    </div>
                </div>

                <div class="row">
                    <div class="col-md-5 mb-3">
                        <asp:Label runat="server" AssociatedControlID="txtLast" Text="Last Name" CssClass="form-label"></asp:Label>
                        <asp:TextBox ID="txtLast" runat="server" CssClass="form-control" MaxLength="60"></asp:TextBox>
                        <asp:RequiredFieldValidator runat="server" ControlToValidate="txtLast" ValidationGroup="Student"
                            ErrorMessage="Last name is required." CssClass="text-danger" Display="Dynamic"></asp:RequiredFieldValidator>
                    </div>
                    <div class="col-md-5 mb-3">
                        <asp:Label runat="server" AssociatedControlID="txtFirst" Text="First Name" CssClass="form-label"></asp:Label>
                        <asp:TextBox ID="txtFirst" runat="server" CssClass="form-control" MaxLength="60"></asp:TextBox>
                        <asp:RequiredFieldValidator runat="server" ControlToValidate="txtFirst" ValidationGroup="Student"
                            ErrorMessage="First name is required." CssClass="text-danger" Display="Dynamic"></asp:RequiredFieldValidator>
                    </div>
                    <div class="col-md-2 mb-3">
                        <asp:Label runat="server" AssociatedControlID="txtMI" Text="M.I." CssClass="form-label"></asp:Label>
                        <asp:TextBox ID="txtMI" runat="server" CssClass="form-control" MaxLength="2" placeholder="optional" autocomplete="off"
                            data-qcu-mi="1"></asp:TextBox>
                    </div>
                </div>

                <div class="mb-3">
                    <asp:Label runat="server" AssociatedControlID="txtEmail" Text="Email" CssClass="form-label"></asp:Label>
                    <asp:TextBox ID="txtEmail" runat="server" CssClass="form-control" MaxLength="120" TextMode="Email"></asp:TextBox>
                    <asp:RequiredFieldValidator runat="server" ControlToValidate="txtEmail" ValidationGroup="Student"
                        ErrorMessage="Email is required." CssClass="text-danger" Display="Dynamic"></asp:RequiredFieldValidator>
                    <asp:RegularExpressionValidator runat="server" ControlToValidate="txtEmail" ValidationGroup="Student"
                        ValidationExpression="^\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$"
                        ErrorMessage="Enter a valid email address." CssClass="text-danger" Display="Dynamic"></asp:RegularExpressionValidator>
                </div>

                <div class="mb-3">
                    <asp:Label runat="server" AssociatedControlID="ddlProgram" Text="Program" CssClass="form-label"></asp:Label>
                    <asp:DropDownList ID="ddlProgram" runat="server" CssClass="form-select"></asp:DropDownList>
                    <asp:RequiredFieldValidator runat="server" ControlToValidate="ddlProgram" ValidationGroup="Student" InitialValue=""
                        ErrorMessage="Please select a program." CssClass="text-danger" Display="Dynamic"></asp:RequiredFieldValidator>
                </div>

                <%-- Only shown when ADDING a student. The temporary password is generated automatically. --%>
                <asp:Panel ID="pnlPassword" runat="server">
                    <div class="qcu-temp-note">
                        <strong>Temporary password:</strong> created automatically as <code>student</code> + the last 4 digits of the ID
                        (for <code>23-0001</code> it is <code>student0001</code>).
                        <div class="form-text mb-0">You will see it after saving. Give it to the student and ask them to change it after logging in.</div>
                    </div>
                </asp:Panel>
            </div>

            <div class="qcu-form-foot">
                <button type="button" class="btn btn-outline-secondary" data-qcu-close>Cancel</button>
                <asp:Button ID="btnSave" runat="server" Text="Add Student" CssClass="btn btn-primary"
                    ValidationGroup="Student" UseSubmitBehavior="false" data-qcu-gate="Student"
                    OnClientClick="if (typeof(Page_ClientValidate) == 'function' && !Page_ClientValidate('Student')) { return false; } this.disabled = true; this.value = 'Please wait...';"
                    OnClick="btnSave_Click" />
            </div>
        </asp:Panel>
    </div>
</ContentTemplate>
</asp:UpdatePanel>
</asp:Content>
