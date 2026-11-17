<%@ Page Title="Booking Report" Language="C#" MasterPageFile="~/Guades_QCUCram_Site.Master" AutoEventWireup="true" CodeBehind="Guades_QCUCram_PrintBooking.aspx.cs" Inherits="Guades_QCUCram.Guades_QCUCram_Reports.Guades_QCUCram_PrintBooking" %>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">

    <style>
        @media print {
            @page { margin: 14mm; }
        }
    </style>

    <asp:Label ID="lblMessage" runat="server" Visible="false"></asp:Label>

    <asp:Panel ID="pnlActions" runat="server" CssClass="no-print mb-3">
        <button type="button" class="btn btn-success" onclick="window.print();">Print</button>
        <asp:HyperLink ID="hlBack" runat="server" CssClass="btn btn-outline-secondary">Back</asp:HyperLink>
    </asp:Panel>

    <asp:Literal ID="litReport" runat="server"></asp:Literal>
</asp:Content>