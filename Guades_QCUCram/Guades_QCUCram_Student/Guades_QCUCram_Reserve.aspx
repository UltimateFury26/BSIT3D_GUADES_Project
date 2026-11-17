<%@ Page Title="Reserve a Room" Language="C#" MasterPageFile="~/Guades_QCUCram_Site.Master" AutoEventWireup="true" CodeBehind="Guades_QCUCram_Reserve.aspx.cs" Inherits="Guades_QCUCram.Guades_QCUCram_Student.Guades_QCUCram_Reserve" %>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div class="qcu-page-head">
        <h2>Reserve a Room</h2>
        <p>Book a collaboration room for your group</p>
    </div>

    <asp:Label ID="lblMessage" runat="server" Visible="false"></asp:Label>
    <asp:Literal ID="litPreview" runat="server"></asp:Literal>

    <asp:Panel ID="pnlRestricted" runat="server" Visible="false" CssClass="alert alert-danger">
        <asp:Literal ID="litRestricted" runat="server"></asp:Literal>
    </asp:Panel>

    <div class="card qcu-rules mb-3">
        <div class="card-body">
            <h5 class="card-title">Library rules</h5>
            <asp:Literal ID="litRules" runat="server"></asp:Literal>
        </div>
    </div>

    <asp:Panel ID="pnlForm" runat="server" CssClass="qcu-steps">

        <h5 class="qcu-section">Requester</h5>
        <div class="qcu-soft"><asp:Literal ID="litRequester" runat="server"></asp:Literal></div>

        <h5 class="qcu-section mt-4">Reservation details</h5>
        <div class="row g-3 mb-3">
            <div class="col-md-4">
                <asp:Label runat="server" AssociatedControlID="txtDate" Text="Date" CssClass="form-label"></asp:Label>
                <asp:TextBox ID="txtDate" runat="server" TextMode="Date" CssClass="form-control"></asp:TextBox>
            </div>
            <div class="col-md-4">
                <asp:Label runat="server" AssociatedControlID="ddlRoom" Text="Room" CssClass="form-label"></asp:Label>
                <asp:DropDownList ID="ddlRoom" runat="server" CssClass="form-select"></asp:DropDownList>
            </div>
            <div class="col-md-4">
                <asp:Label runat="server" AssociatedControlID="ddlSlot" Text="Time slot" CssClass="form-label"></asp:Label>
                <asp:DropDownList ID="ddlSlot" runat="server" CssClass="form-select"></asp:DropDownList>
            </div>
        </div>
        <p class="small mb-3">
            Not sure which slot is free?
            <a runat="server" href="~/Guades_QCUCram_Availability.aspx">View the availability grid</a>.
        </p>

        <div class="mb-3">
            <asp:Label runat="server" AssociatedControlID="txtPurpose" Text="Purpose" CssClass="form-label"></asp:Label>
            <asp:TextBox ID="txtPurpose" runat="server" TextMode="MultiLine" Rows="3" MaxLength="300" CssClass="form-control"></asp:TextBox>
            <div class="form-text">Maximum 300 characters.</div>
        </div>

        <h5 class="qcu-section mt-4">Group members</h5>
        <div class="mb-3" style="max-width:260px;">
            <asp:Label runat="server" AssociatedControlID="ddlMembers" Text="Number of members (including you)" CssClass="form-label"></asp:Label>
            <asp:DropDownList ID="ddlMembers" runat="server" CssClass="form-select"
                AutoPostBack="true" OnSelectedIndexChanged="ddlMembers_SelectedIndexChanged">
                <asp:ListItem>5</asp:ListItem>
                <asp:ListItem>6</asp:ListItem>
                <asp:ListItem>7</asp:ListItem>
                <asp:ListItem>8</asp:ListItem>
                <asp:ListItem>9</asp:ListItem>
                <asp:ListItem>10</asp:ListItem>
            </asp:DropDownList>
        </div>

        <asp:Repeater ID="rptMembers" runat="server">
            <HeaderTemplate>
                <table class="table table-sm table-bordered align-middle">
                    <thead class="table-light">
                        <tr>
                            <th style="width:50px;">#</th>
                            <th>Last name</th>
                            <th>First name</th>
                            <th style="width:110px;">M.I.</th>
                        </tr>
                    </thead>
                    <tbody>
            </HeaderTemplate>
            <ItemTemplate>
                <tr>
                    <td><%# Eval("Number") %></td>
                    <td><asp:TextBox ID="txtLast" runat="server" CssClass="form-control" MaxLength="60" Text='<%# Eval("LastName") %>'></asp:TextBox></td>
                    <td><asp:TextBox ID="txtFirst" runat="server" CssClass="form-control" MaxLength="60" Text='<%# Eval("FirstName") %>'></asp:TextBox></td>
                    <td><asp:TextBox ID="txtMI" runat="server" CssClass="form-control" MaxLength="5" Text='<%# Eval("MI") %>'></asp:TextBox></td>
                </tr>
            </ItemTemplate>
            <FooterTemplate>
                    </tbody>
                </table>
            </FooterTemplate>
        </asp:Repeater>

        <asp:Button ID="btnSubmit" runat="server" Text="Submit Request" CssClass="btn btn-cta btn-lg" OnClick="btnSubmit_Click" />
        <asp:Button ID="btnReset" runat="server" Text="Reset form" CssClass="btn btn-outline-secondary btn-lg" CausesValidation="false" OnClick="btnReset_Click" />

    </asp:Panel>
</asp:Content>