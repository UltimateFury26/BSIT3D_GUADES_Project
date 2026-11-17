<%@ Page Title="Reservation Records" Language="C#" MasterPageFile="~/Guades_QCUCram_Site.Master" AutoEventWireup="true" CodeBehind="Guades_QCUCram_Records.aspx.cs" Inherits="Guades_QCUCram.Guades_QCUCram_Admin.Guades_QCUCram_Records" %>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div class="qcu-page-head">
        <h2>Reservation Records</h2>
        <p>Search, review and export every reservation</p>
    </div>

<asp:UpdatePanel runat="server">
<ContentTemplate>
    <asp:Label ID="lblMessage" runat="server" Visible="false"></asp:Label>

    <%-- ================= Filters ================= --%>
    <asp:Panel ID="pnlFilter" runat="server" DefaultButton="btnFilter" CssClass="qcu-toolbar mb-3">
        <div class="row g-3">
            <div class="col-md-3">
                <asp:Label runat="server" AssociatedControlID="txtFrom" Text="From date" CssClass="form-label"></asp:Label>
                <asp:TextBox ID="txtFrom" runat="server" TextMode="Date" CssClass="form-control" data-live-btn="btnFilter"></asp:TextBox>
            </div>
            <div class="col-md-3">
                <asp:Label runat="server" AssociatedControlID="txtTo" Text="To date" CssClass="form-label"></asp:Label>
                <asp:TextBox ID="txtTo" runat="server" TextMode="Date" CssClass="form-control" data-live-btn="btnFilter"></asp:TextBox>
            </div>
            <div class="col-md-3">
                <asp:Label runat="server" AssociatedControlID="ddlStatus" Text="Status" CssClass="form-label"></asp:Label>
                <asp:DropDownList ID="ddlStatus" runat="server" CssClass="form-select" AutoPostBack="true" OnSelectedIndexChanged="btnFilter_Click"></asp:DropDownList>
            </div>
            <div class="col-md-3">
                <asp:Label runat="server" AssociatedControlID="ddlRoom" Text="Room" CssClass="form-label"></asp:Label>
                <asp:DropDownList ID="ddlRoom" runat="server" CssClass="form-select" AutoPostBack="true" OnSelectedIndexChanged="btnFilter_Click"></asp:DropDownList>
            </div>
            <div class="col-md-4">
                <asp:Label runat="server" AssociatedControlID="ddlProgram" Text="Program" CssClass="form-label"></asp:Label>
                <asp:DropDownList ID="ddlProgram" runat="server" CssClass="form-select" AutoPostBack="true" OnSelectedIndexChanged="btnFilter_Click"></asp:DropDownList>
            </div>
            <div class="col-md-4">
                <asp:Label runat="server" AssociatedControlID="txtStudent" Text="Student (name or number)" CssClass="form-label"></asp:Label>
                <asp:TextBox ID="txtStudent" runat="server" CssClass="form-control" MaxLength="100" data-live-btn="btnFilter" autocomplete="off"
                    placeholder="e.g. Santos or 24-00123"></asp:TextBox>
            </div>
            <div class="col-md-4 d-flex align-items-end flex-wrap gap-2">
                <asp:Button ID="btnFilter" runat="server" Text="Apply Filters" CssClass="btn btn-primary"
                    CausesValidation="false" OnClick="btnFilter_Click" />
                <asp:Button ID="btnClear" runat="server" Text="Clear" CssClass="btn btn-outline-secondary"
                    CausesValidation="false" OnClick="btnClear_Click" />
                <asp:Button ID="btnExportCsv" runat="server" Text="Export to CSV" CssClass="btn btn-success"
                    CausesValidation="false" OnClick="btnExportCsv_Click" />
            </div>
        </div>
    </asp:Panel>

    <p class="text-muted"><asp:Label ID="lblCount" runat="server"></asp:Label></p>

    <%-- ================= Records grid ================= --%>
    <div class="table-responsive">
        <asp:GridView ID="gvRecords" runat="server" AutoGenerateColumns="false"
            CssClass="table table-bordered table-striped align-middle qcu-admin-table"
            DataKeyNames="ReservationID"
            AllowPaging="true" PageSize="20"
            AllowSorting="true"
            EmptyDataText="No reservations match these filters."
            OnPageIndexChanging="gvRecords_PageIndexChanging"
            OnSorting="gvRecords_Sorting"
            OnRowDataBound="gvRecords_RowDataBound"
            OnRowCommand="gvRecords_RowCommand">
            <PagerStyle CssClass="qcu-pager" HorizontalAlign="Center" />
            <Columns>
                <asp:BoundField DataField="ReservationID" HeaderText="#" SortExpression="ReservationID" />
                <asp:BoundField DataField="DateText" HeaderText="Date" SortExpression="ReservationDate" />
                <asp:BoundField DataField="TimeText" HeaderText="Time" SortExpression="StartTime" />
                <asp:BoundField DataField="RoomName" HeaderText="Room" SortExpression="RoomName" />
                <asp:BoundField DataField="StudentNumber" HeaderText="ID / Student No." />
                <asp:BoundField DataField="Requester" HeaderText="Requester" SortExpression="Requester" />
                <asp:BoundField DataField="Program" HeaderText="Program" SortExpression="Program" />
                <asp:BoundField DataField="NumberOfMembers" HeaderText="Members" SortExpression="NumberOfMembers" />
                <asp:TemplateField HeaderText="Status" SortExpression="Status">
                    <ItemTemplate><%# StatusBadge(Eval("Status")) %></ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="Actions">
                    <ItemTemplate>
                        <asp:LinkButton runat="server" Text="Details" CommandName="ViewRes"
                            CommandArgument='<%# Eval("ReservationID") %>' CausesValidation="false" CssClass="btn btn-sm btn-outline-primary me-2"></asp:LinkButton>
                        <a class="btn btn-sm btn-outline-secondary" data-qcu-load="1"
                            data-qcu-load-title='<%# "Reservation #" + Eval("ReservationID") %>'
                            href='<%# ResolveUrl("~/Guades_QCUCram_Reports/Guades_QCUCram_PrintBooking.aspx") + "?id=" + Eval("ReservationID") %>'>Print</a>
                    </ItemTemplate>
                </asp:TemplateField>
            </Columns>
        </asp:GridView>
    </div>

    <%-- ================= Details: members + status history (pop-up modal) ================= --%>
    <div class="qcu-form-overlay<%= ModalOpenCss %>">
        <asp:Panel ID="pnlDetail" runat="server" CssClass="qcu-form-modal qcu-form-modal-lg"
            role="dialog" aria-modal="true" aria-labelledby="detailModalTitle" TabIndex="-1">

            <div class="qcu-form-head">
                <div class="qcu-form-title" id="detailModalTitle"><asp:Label ID="lblDetailTitle" runat="server"></asp:Label></div>
                <button type="button" class="qcu-form-close" data-qcu-close aria-label="Close">&times;</button>
            </div>

            <div class="qcu-form-body">
                <asp:Literal ID="litDetail" runat="server"></asp:Literal>

                <h6 class="mt-4">Group members</h6>
                <asp:GridView ID="gvMembers" runat="server" AutoGenerateColumns="false"
                    CssClass="table table-sm table-bordered table-striped w-auto"
                    EmptyDataText="No members recorded.">
                    <Columns>
                        <asp:TemplateField HeaderText="No.">
                            <ItemTemplate><%# Container.DataItemIndex + 1 %></ItemTemplate>
                        </asp:TemplateField>
                        <asp:BoundField DataField="MemberName" HeaderText="Name" />
                    </Columns>
                </asp:GridView>

                <h6 class="mt-4">Status history</h6>
                <asp:GridView ID="gvHistory" runat="server" AutoGenerateColumns="false"
                    CssClass="table table-sm table-bordered table-striped"
                    EmptyDataText="No history recorded.">
                    <Columns>
                        <asp:BoundField DataField="WhenText" HeaderText="When" />
                        <asp:BoundField DataField="ChangeText" HeaderText="Change" />
                        <asp:BoundField DataField="ActorText" HeaderText="By" />
                        <asp:BoundField DataField="Notes" HeaderText="Notes" NullDisplayText="-" />
                    </Columns>
                </asp:GridView>
            </div>

            <div class="qcu-form-foot">
                <asp:Button ID="btnCloseDetail" runat="server" Text="Close" CssClass="btn btn-outline-secondary"
                    CausesValidation="false" OnClick="btnCloseDetail_Click" />
                <asp:HyperLink ID="hlPrint" runat="server" Text="Printable report"
                    CssClass="btn btn-primary" data-qcu-load="1"></asp:HyperLink>
            </div>
        </asp:Panel>
    </div>
</ContentTemplate>
<Triggers>
<asp:PostBackTrigger ControlID="btnExportCsv" />
</Triggers>
</asp:UpdatePanel>
</asp:Content>