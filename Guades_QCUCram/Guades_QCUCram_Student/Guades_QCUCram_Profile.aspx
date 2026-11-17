<%@ Page Title="Profile" Language="C#" MasterPageFile="~/Guades_QCUCram_Site.Master" AutoEventWireup="true" CodeBehind="Guades_QCUCram_Profile.aspx.cs" Inherits="Guades_QCUCram.Guades_QCUCram_Student.Guades_QCUCram_Profile" %>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div class="qcu-page-head">
        <h2>Profile</h2>
        <p>Your details, booking standing and history</p>
    </div>

    <asp:Label ID="lblMessage" runat="server" Visible="false"></asp:Label>

    <%-- Booking standing: restricted / strikes / good standing --%>
    <asp:Literal ID="litStanding" runat="server"></asp:Literal>

    <div class="row g-4">
        <%-- ============ My details ============ --%>
        <div class="col-lg-6">
            <div class="card">
                <div class="card-header"><strong>My details</strong></div>
                <div class="card-body">

                    <div class="row mb-3">
                        <div class="col-sm-6">
                            <div class="text-muted small">ID / Student Number</div>
                            <asp:Label ID="lblNumber" runat="server" CssClass="fw-bold"></asp:Label>
                        </div>
                        <div class="col-sm-6">
                            <div class="text-muted small">Requester type</div>
                            <asp:Label ID="lblType" runat="server" CssClass="fw-bold"></asp:Label>
                        </div>
                    </div>
                    <p class="text-muted small">Your ID number and requester type can only be changed by a library assistant.</p>

                    <div class="row">
                        <div class="col-md-6 mb-3">
                            <asp:Label runat="server" AssociatedControlID="txtLast" Text="Last Name" CssClass="form-label"></asp:Label>
                            <asp:TextBox ID="txtLast" runat="server" CssClass="form-control" MaxLength="60"></asp:TextBox>
                            <asp:RequiredFieldValidator runat="server" ControlToValidate="txtLast"
                                ErrorMessage="Last name is required." CssClass="text-danger" Display="Dynamic"></asp:RequiredFieldValidator>
                        </div>
                        <div class="col-md-6 mb-3">
                            <asp:Label runat="server" AssociatedControlID="txtFirst" Text="First Name" CssClass="form-label"></asp:Label>
                            <asp:TextBox ID="txtFirst" runat="server" CssClass="form-control" MaxLength="60"></asp:TextBox>
                            <asp:RequiredFieldValidator runat="server" ControlToValidate="txtFirst"
                                ErrorMessage="First name is required." CssClass="text-danger" Display="Dynamic"></asp:RequiredFieldValidator>
                        </div>
                    </div>

                    <div class="mb-3">
                        <asp:Label runat="server" AssociatedControlID="txtMI" Text="Middle Initial (optional)" CssClass="form-label"></asp:Label>
                        <asp:TextBox ID="txtMI" runat="server" CssClass="form-control" MaxLength="5" Width="100px"></asp:TextBox>
                    </div>

                    <div class="mb-3">
                        <asp:Label runat="server" AssociatedControlID="txtEmail" Text="Email" CssClass="form-label"></asp:Label>
                        <asp:TextBox ID="txtEmail" runat="server" CssClass="form-control" MaxLength="120" TextMode="Email"></asp:TextBox>
                        <asp:RequiredFieldValidator runat="server" ControlToValidate="txtEmail"
                            ErrorMessage="Email is required." CssClass="text-danger" Display="Dynamic"></asp:RequiredFieldValidator>
                        <asp:RegularExpressionValidator runat="server" ControlToValidate="txtEmail"
                            ValidationExpression="^\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$"
                            ErrorMessage="Enter a valid email address." CssClass="text-danger" Display="Dynamic"></asp:RegularExpressionValidator>
                    </div>

                    <div class="mb-3">
                        <asp:Label runat="server" AssociatedControlID="ddlProgram" Text="Program" CssClass="form-label"></asp:Label>
                        <asp:DropDownList ID="ddlProgram" runat="server" CssClass="form-select"></asp:DropDownList>
                        <asp:RequiredFieldValidator runat="server" ControlToValidate="ddlProgram" InitialValue=""
                            ErrorMessage="Please select a program." CssClass="text-danger" Display="Dynamic"></asp:RequiredFieldValidator>
                    </div>

                    <asp:Button ID="btnSave" runat="server" Text="Save Changes" CssClass="btn btn-primary"
                        UseSubmitBehavior="false"
                        OnClientClick="if (typeof(Page_ClientValidate) == 'function' && !Page_ClientValidate()) { return false; } this.disabled = true; this.value = 'Please wait...';"
                        OnClick="btnSave_Click" />
                </div>
            </div>
        </div>

        <%-- ============ Reservation history summary + links ============ --%>
        <div class="col-lg-6">
            <div class="card mb-4">
                <div class="card-header"><strong>My reservation history</strong></div>
                <div class="card-body">
                    <asp:Literal ID="litHistory" runat="server"></asp:Literal>
                </div>
            </div>

            <div class="card">
                <div class="card-header"><strong>Quick links</strong></div>
                <div class="list-group list-group-flush">
                    <a runat="server" class="list-group-item list-group-item-action" href="~/Guades_QCUCram_Student/Guades_QCUCram_MyReservations.aspx">My Reservations</a>
                    <a runat="server" class="list-group-item list-group-item-action" href="~/Guades_QCUCram_Student/Guades_QCUCram_Notifications.aspx">Notifications</a>
                </div>
            </div>

            <%-- Account security: Change Password lives here (not in the top navigation). A confirmation pop-up comes first. --%>
            <div class="card mt-4">
                <div class="card-header"><strong>Account security</strong></div>
                <div class="card-body d-flex flex-wrap align-items-center justify-content-between gap-3">
                    <div>
                        <div class="fw-semibold">Password</div>
                        <div class="text-muted small">Change it regularly and never share it with anyone.</div>
                    </div>
                    <a runat="server" class="btn btn-outline-primary" href="~/Guades_QCUCram_Account/Guades_QCUCram_ChangePassword.aspx"
                        onclick="return qcuConfirm(this, { tone: 'primary', title: 'Change your password?', message: 'You will be taken to the Change Password page.', confirmText: 'Continue' });">Change Password</a>
                </div>
            </div>
        </div>
    </div>
</asp:Content>