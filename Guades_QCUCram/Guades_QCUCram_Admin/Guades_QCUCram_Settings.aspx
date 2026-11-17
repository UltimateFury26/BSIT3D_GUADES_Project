<%@ Page Title="Settings" Language="C#" MasterPageFile="~/Guades_QCUCram_Site.Master" AutoEventWireup="true" CodeBehind="Guades_QCUCram_Settings.aspx.cs" Inherits="Guades_QCUCram.Guades_QCUCram_Admin.Guades_QCUCram_Settings" %>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div class="qcu-page-head">
        <h2>Settings</h2>
        <p>System rules and limits</p>
    </div>

    <asp:Label ID="lblMessage" runat="server" Visible="false"></asp:Label>

    <div class="table-responsive">
<asp:GridView ID="gvSettings" runat="server" AutoGenerateColumns="false"
        CssClass="table table-bordered table-striped qcu-admin-table"
        EmptyDataText="No settings found. Run the seed script.">
        <Columns>
            <asp:TemplateField HeaderText="Setting">
                <ItemTemplate>
                    <asp:HiddenField ID="hfKey" runat="server" Value='<%# Eval("SettingKey") %>' />
                    <strong><%#: Eval("SettingKey") %></strong>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Value">
                <ItemTemplate>
                    <asp:TextBox ID="txtValue" runat="server" CssClass="form-control" Width="150px" MaxLength="9"
                        Text='<%# Eval("SettingValue") %>'></asp:TextBox>
                    <asp:RequiredFieldValidator runat="server" ControlToValidate="txtValue"
                        ErrorMessage="Required." CssClass="text-danger" Display="Dynamic"></asp:RequiredFieldValidator>
                    <asp:RangeValidator runat="server" ControlToValidate="txtValue" Type="Integer"
                        MinimumValue='<%# GetMin(Eval("SettingKey")) %>' MaximumValue='<%# GetMax(Eval("SettingKey")) %>'
                        ErrorMessage='<%# RangeText(Eval("SettingKey")) %>'
                        CssClass="text-danger" Display="Dynamic"></asp:RangeValidator>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Description">
                <ItemTemplate>
                    <%#: Eval("Description") %>
                    <div class="text-muted small"><%#: RangeText(Eval("SettingKey")) %></div>
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
    </asp:GridView>
</div>

    <asp:Button ID="btnSave" runat="server" Text="Save Settings" CssClass="btn btn-primary"
        UseSubmitBehavior="false"
        OnClientClick="if (typeof(Page_ClientValidate) == 'function' && !Page_ClientValidate()) { return false; } this.disabled = true; this.value = 'Please wait...';"
        OnClick="btnSave_Click" />

    <p class="text-muted mt-3">
        <strong>DevTimeOffsetMinutes</strong> is for testing only: it shifts the system clock so you can test forfeiture
        and restrictions without waiting. Keep it at 0 for normal use. While it is not 0, a red banner shows on every page.
    </p>
</asp:Content>