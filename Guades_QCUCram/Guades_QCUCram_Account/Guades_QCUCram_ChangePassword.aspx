<%@ Page Title="Change Password" Language="C#" MasterPageFile="~/Guades_QCUCram_Site.Master" AutoEventWireup="true" CodeBehind="Guades_QCUCram_ChangePassword.aspx.cs" Inherits="Guades_QCUCram.Guades_QCUCram_Account.Guades_QCUCram_ChangePassword" %>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div class="qcu-auth">
        <div class="qcu-auth-card">
            <div class="qcu-auth-head">
                <img src="<%= ResolveUrl("~/Content/images/qcu-logo.png") %>" alt="QCU Seal" />
                <div class="qcu-auth-univ">Quezon City University</div>
                <h2>Change Password</h2>
                <p>Keep your account secure</p>
            </div>
            <div class="qcu-auth-body">

    <asp:Label ID="lblMessage" runat="server" Visible="false"></asp:Label>

    <div>
        <div class="mb-3">
            <asp:Label runat="server" AssociatedControlID="txtCurrent" Text="Current Password" CssClass="form-label"></asp:Label>
            <asp:TextBox ID="txtCurrent" runat="server" CssClass="form-control" TextMode="Password" MaxLength="100"></asp:TextBox>
            <asp:RequiredFieldValidator runat="server" ControlToValidate="txtCurrent"
                ErrorMessage="Current password is required." CssClass="text-danger" Display="Dynamic"></asp:RequiredFieldValidator>
        </div>

        <div class="mb-3">
            <asp:Label runat="server" AssociatedControlID="txtNew" Text="New Password" CssClass="form-label"></asp:Label>
            <asp:TextBox ID="txtNew" runat="server" CssClass="form-control" TextMode="Password" MaxLength="100"></asp:TextBox>
            <asp:RequiredFieldValidator runat="server" ControlToValidate="txtNew"
                ErrorMessage="New password is required." CssClass="text-danger" Display="Dynamic"></asp:RequiredFieldValidator>
            <asp:CustomValidator runat="server" ControlToValidate="txtNew" ValidateEmptyText="false"
                ClientValidationFunction="qcuValidateStrongPassword"
                ErrorMessage="Password is too weak. Meet every requirement in the checklist below." CssClass="text-danger" Display="Dynamic"></asp:CustomValidator>
            <div data-qcu-strength data-input="<%= txtNew.ClientID %>"></div>
        </div>

        <div class="mb-3">
            <asp:Label runat="server" AssociatedControlID="txtConfirm" Text="Confirm New Password" CssClass="form-label"></asp:Label>
            <asp:TextBox ID="txtConfirm" runat="server" CssClass="form-control" TextMode="Password" MaxLength="100"></asp:TextBox>
            <asp:RequiredFieldValidator runat="server" ControlToValidate="txtConfirm"
                ErrorMessage="Please confirm the new password." CssClass="text-danger" Display="Dynamic"></asp:RequiredFieldValidator>
            <asp:CompareValidator runat="server" ControlToValidate="txtConfirm" ControlToCompare="txtNew"
                Operator="Equal" Type="String"
                ErrorMessage="Passwords do not match." CssClass="text-danger" Display="Dynamic"></asp:CompareValidator>
            <div class="qcu-match" data-qcu-match data-a="<%= txtNew.ClientID %>" data-b="<%= txtConfirm.ClientID %>"></div>
        </div>

        <asp:Button ID="btnChange" runat="server" Text="Change Password" CssClass="btn btn-primary"
            UseSubmitBehavior="false"
            OnClientClick="if (typeof(Page_ClientValidate) == 'function' && !Page_ClientValidate()) { return false; } this.disabled = true; this.value = 'Please wait...';"
            OnClick="btnChange_Click" />
    </div>

            </div>
        </div>
    </div>
</asp:Content>