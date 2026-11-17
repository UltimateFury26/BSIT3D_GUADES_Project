<%@ Page Title="Register" Language="C#" MasterPageFile="~/Guades_QCUCram_Site.Master" AutoEventWireup="true" CodeBehind="Guades_QCUCram_Register.aspx.cs" Inherits="Guades_QCUCram.Guades_QCUCram_Account.Guades_QCUCram_Register" %>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div class="qcu-auth">
        <div class="qcu-auth-card wide">
            <div class="qcu-auth-head">
                <img src="<%= ResolveUrl("~/Content/images/qcu-logo.png") %>" alt="QCU Seal" />
                <div class="qcu-auth-univ">Quezon City University</div>
                <h2>Register</h2>
                <p>Students, faculty, and visitors can create an account</p>
            </div>
            <div class="qcu-auth-body">

    <asp:Label ID="lblMessage" runat="server" Visible="false"></asp:Label>

    <div>

        <div class="mb-3">
            <asp:Label runat="server" AssociatedControlID="ddlRequesterType" Text="Requester Type" CssClass="form-label"></asp:Label>
            <asp:DropDownList ID="ddlRequesterType" runat="server" CssClass="form-select"></asp:DropDownList>
            <asp:RequiredFieldValidator runat="server" ControlToValidate="ddlRequesterType" InitialValue=""
                ErrorMessage="Please select a requester type." CssClass="text-danger" Display="Dynamic"></asp:RequiredFieldValidator>
        </div>

        <div class="mb-3">
            <label id="lblRegNumber" for="<%= txtStudentNumber.ClientID %>" class="form-label">ID / Student Number</label>
            <asp:TextBox ID="txtStudentNumber" runat="server" CssClass="form-control" MaxLength="20" autocomplete="off"></asp:TextBox>
            <asp:RequiredFieldValidator runat="server" ControlToValidate="txtStudentNumber"
                ErrorMessage="ID / Student Number is required." CssClass="text-danger" Display="Dynamic"></asp:RequiredFieldValidator>
            <asp:CustomValidator runat="server" ControlToValidate="txtStudentNumber" ValidateEmptyText="false"
                ClientValidationFunction="qcuValidateRegId"
                ErrorMessage="Enter a valid ID number." CssClass="text-danger" Display="Dynamic"></asp:CustomValidator>
            <div class="form-text" id="hintRegNumber"></div>
        </div>

        <div class="row">
            <div class="col-md-5 mb-3">
                <asp:Label runat="server" AssociatedControlID="txtLastName" Text="Last Name" CssClass="form-label"></asp:Label>
                <asp:TextBox ID="txtLastName" runat="server" CssClass="form-control" MaxLength="60"></asp:TextBox>
                <asp:RequiredFieldValidator runat="server" ControlToValidate="txtLastName"
                    ErrorMessage="Last name is required." CssClass="text-danger" Display="Dynamic"></asp:RequiredFieldValidator>
            </div>
            <div class="col-md-4 mb-3">
                <asp:Label runat="server" AssociatedControlID="txtFirstName" Text="First Name" CssClass="form-label"></asp:Label>
                <asp:TextBox ID="txtFirstName" runat="server" CssClass="form-control" MaxLength="60"></asp:TextBox>
                <asp:RequiredFieldValidator runat="server" ControlToValidate="txtFirstName"
                    ErrorMessage="First name is required." CssClass="text-danger" Display="Dynamic"></asp:RequiredFieldValidator>
            </div>
            <div class="col-md-3 mb-3">
                <asp:Label runat="server" AssociatedControlID="txtMI" Text="M.I." CssClass="form-label"></asp:Label>
                <asp:TextBox ID="txtMI" runat="server" CssClass="form-control" MaxLength="2" placeholder="optional" autocomplete="off"></asp:TextBox>
            </div>
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

        <div class="mb-3">
            <asp:Label runat="server" AssociatedControlID="txtPassword" Text="Password" CssClass="form-label"></asp:Label>
            <asp:TextBox ID="txtPassword" runat="server" CssClass="form-control" TextMode="Password" MaxLength="100"></asp:TextBox>
            <asp:RequiredFieldValidator runat="server" ControlToValidate="txtPassword"
                ErrorMessage="Password is required." CssClass="text-danger" Display="Dynamic"></asp:RequiredFieldValidator>
            <asp:CustomValidator runat="server" ControlToValidate="txtPassword" ValidateEmptyText="false"
                ClientValidationFunction="qcuValidateStrongPassword"
                ErrorMessage="Password is too weak. Meet every requirement in the checklist below." CssClass="text-danger" Display="Dynamic"></asp:CustomValidator>
            <div data-qcu-strength data-input="<%= txtPassword.ClientID %>"
                data-context="<%= txtStudentNumber.ClientID %>,<%= txtLastName.ClientID %>,<%= txtFirstName.ClientID %>,<%= txtEmail.ClientID %>"></div>
        </div>

        <div class="mb-3">
            <asp:Label runat="server" AssociatedControlID="txtConfirm" Text="Confirm Password" CssClass="form-label"></asp:Label>
            <asp:TextBox ID="txtConfirm" runat="server" CssClass="form-control" TextMode="Password" MaxLength="100"></asp:TextBox>
            <asp:RequiredFieldValidator runat="server" ControlToValidate="txtConfirm"
                ErrorMessage="Please confirm your password." CssClass="text-danger" Display="Dynamic"></asp:RequiredFieldValidator>
            <asp:CompareValidator runat="server" ControlToValidate="txtConfirm" ControlToCompare="txtPassword"
                Operator="Equal" Type="String"
                ErrorMessage="Passwords do not match." CssClass="text-danger" Display="Dynamic"></asp:CompareValidator>
            <div class="qcu-match" data-qcu-match data-a="<%= txtPassword.ClientID %>" data-b="<%= txtConfirm.ClientID %>"></div>
        </div>

        <asp:Button ID="btnRegister" runat="server" Text="Register" CssClass="btn btn-primary"
            UseSubmitBehavior="false"
            OnClientClick="if (typeof(Page_ClientValidate) == 'function' && !Page_ClientValidate()) { return false; } this.disabled = true; this.value = 'Please wait...';"
            OnClick="btnRegister_Click" />

        <a runat="server" href="~/Guades_QCUCram_Account/Guades_QCUCram_Login.aspx" class="btn btn-link">Already have an account? Login</a>
    </div>

            </div>
        </div>
    </div>

    <script>
        var qcuRegType = document.getElementById.bind(document, '<%= ddlRequesterType.ClientID %>');
        var qcuRegNum = document.getElementById.bind(document, '<%= txtStudentNumber.ClientID %>');

        // Client rule for the ID box (server re-checks everything on Register).
        function qcuValidateRegId(source, args) {
            var type = qcuRegType().value;
            var v = (args.Value || '').trim();
            if (type === 'Student') {
                args.IsValid = /^\d{2}-\d{4}$/.test(v);
                source.innerHTML = 'Student number must look like 23-0001.';
            } else {
                args.IsValid = /^[A-Za-z0-9\-]{3,20}$/.test(v);
                source.innerHTML = 'Use 3-20 letters, numbers, or dashes only.';
            }
        }

        document.addEventListener('DOMContentLoaded', function () {
            var type = qcuRegType(), num = qcuRegNum();
            var label = document.getElementById('lblRegNumber');
            var hint = document.getElementById('hintRegNumber');
            var mi = document.getElementById('<%= txtMI.ClientID %>');

            function apply() {
                var t = type.value;
                if (t === 'Student') {
                    label.textContent = 'Student Number';
                    num.placeholder = '23-0001';
                    num.maxLength = 7;
                    num.setAttribute('inputmode', 'numeric');
                    hint.textContent = 'Format: 23-0001 (the dash is added for you).';
                    num.value = Qcu.formatDashed(num.value.replace(/[^\d-]/g, ''));
                } else {
                    label.textContent = (t === 'Faculty') ? 'Faculty ID' : (t === 'Visitor') ? 'ID Number' : 'ID / Student Number';
                    num.placeholder = (t === '') ? '23-0001' : (t === 'Faculty') ? 'Faculty ID' : 'ID number';
                    num.maxLength = 20;
                    num.setAttribute('inputmode', 'text');
                    hint.textContent = (t === '') ? 'Choose a requester type first.' : '';
                }
            }
            type.addEventListener('change', apply);
            Qcu.attachDash(num, {
                enabled: function() { return type.value === 'Student'; },
                digitsOnly: function() { return type.value === 'Student'; }
            });
            apply();

            // Middle initial: letters only, upper-case
            mi.addEventListener('input', function() { mi.value = mi.value.replace(/[^A-Za-z]/g, '').toUpperCase(); });
        });
    </script>
</asp:Content>