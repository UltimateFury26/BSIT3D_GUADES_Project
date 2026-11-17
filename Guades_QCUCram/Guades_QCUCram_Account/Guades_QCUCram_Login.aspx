<%@ Page Title="Login" Language="C#" MasterPageFile="~/Guades_QCUCram_Site.Master" AutoEventWireup="true" CodeBehind="Guades_QCUCram_Login.aspx.cs" Inherits="Guades_QCUCram.Guades_QCUCram_Account.Guades_QCUCram_Login" %>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div class="qcu-auth">
        <div class="qcu-auth-card">
            <div class="qcu-auth-head">
                <img src="<%= ResolveUrl("~/Content/images/qcu-logo.png") %>" alt="QCU Seal" />
                <div class="qcu-auth-univ">Quezon City University</div>
                <h2>Login</h2>
                <p>Sign in to reserve a collaboration room</p>
            </div>
            <div class="qcu-auth-body">

    <asp:Label ID="lblMessage" runat="server" Visible="false"></asp:Label>

    <asp:Panel ID="pnlLogin" runat="server" DefaultButton="btnLogin">

        <div class="mb-3">
            <asp:Label runat="server" Text="Sign in as" CssClass="form-label d-block"></asp:Label>
            <asp:RadioButtonList ID="rblRole" runat="server" RepeatLayout="Flow" CssClass="form-check">
                <asp:ListItem Value="Student" Text="Student / Faculty / Visitor" Selected="True"></asp:ListItem>
                <asp:ListItem Value="Admin" Text="Library Assistant"></asp:ListItem>
            </asp:RadioButtonList>
        </div>

        <div class="mb-3">
            <label id="lblLoginId" for="<%= txtId.ClientID %>" class="form-label"><%: IdLabel %></label>
            <asp:TextBox ID="txtId" runat="server" CssClass="form-control" MaxLength="50" autocomplete="username"></asp:TextBox>
            <asp:RequiredFieldValidator runat="server" ControlToValidate="txtId"
                ErrorMessage="Student number / ID is required." CssClass="text-danger" Display="Dynamic"></asp:RequiredFieldValidator>
        </div>

        <div class="mb-3">
            <asp:Label runat="server" AssociatedControlID="txtPassword" Text="Password" CssClass="form-label"></asp:Label>
            <asp:TextBox ID="txtPassword" runat="server" CssClass="form-control" TextMode="Password" MaxLength="100"></asp:TextBox>
            <asp:RequiredFieldValidator runat="server" ControlToValidate="txtPassword"
                ErrorMessage="Password is required." CssClass="text-danger" Display="Dynamic"></asp:RequiredFieldValidator>
        </div>

        <asp:Button ID="btnLogin" runat="server" Text="Login" CssClass="btn btn-primary"
            UseSubmitBehavior="false"
            OnClientClick="if (typeof(Page_ClientValidate) == 'function' && !Page_ClientValidate()) { return false; } this.disabled = true; this.value = 'Please wait...';"
            OnClick="btnLogin_Click" />

        <a runat="server" href="~/Guades_QCUCram_Account/Guades_QCUCram_Register.aspx" class="btn btn-link">Create an account</a>
    </asp:Panel>

            </div>
        </div>
    </div>

    <script>
        // Role picker changes the first field: "Student Number / ID" (dash auto-format) vs "Username" (plain text).
        document.addEventListener('DOMContentLoaded', function () {
            var radios = document.querySelectorAll('input[type="radio"][name$="rblRole"]');
            var id = document.getElementById('<%= txtId.ClientID %>');
            var label = document.getElementById('lblLoginId');
            var holder = id.closest('.mb-3');
            var msg = holder ? holder.querySelector('span.text-danger') : null;

            function isAdmin() {
                for (var i = 0; i < radios.length; i++) if (radios[i].checked) return radios[i].value === 'Admin';
                return false;
            }
            function apply(clear) {
                var admin = isAdmin();
                label.textContent = admin ? 'Username' : 'Student Number / ID';
                id.placeholder = admin ? 'username' : '23-0001';
                id.setAttribute('autocomplete', 'username');
                if (msg) {
                    var text = admin ? 'Username is required.' : 'Student number / ID is required.';
                    msg.textContent = text;
                    msg.errormessage = text;
                }
                if (clear) id.value = '';          // a half-typed "23-" must not carry over
            }
            for (var i = 0; i < radios.length; i++) radios[i].addEventListener('change', function() { apply(true); });

            Qcu.attachDash(id, { enabled: function() { return !isAdmin(); } });
            apply(false);
        });
    </script>
</asp:Content>