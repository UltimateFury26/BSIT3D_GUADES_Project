<%@ Page Title="Analytics" Language="C#" MasterPageFile="~/Guades_QCUCram_Site.Master" AutoEventWireup="true" CodeBehind="Guades_QCUCram_Analytics.aspx.cs" Inherits="Guades_QCUCram.Guades_QCUCram_Reports.Guades_QCUCram_Analytics" %>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">

    <div class="qcu-page-head">
        <h2>Analytics</h2>
        <p>Booking trends, room utilization and no-show statistics</p>
    </div>

<asp:UpdatePanel runat="server">
<ContentTemplate>
    <asp:Label ID="lblMessage" runat="server" Visible="false"></asp:Label>

    <%-- ================= Date range filter ================= --%>
    <asp:Panel ID="pnlFilter" runat="server" DefaultButton="btnApply" CssClass="qcu-toolbar mb-3">
        <div class="row g-3 align-items-end">
            <div class="col-md-3">
                <asp:Label runat="server" AssociatedControlID="txtFrom" Text="From date" CssClass="form-label"></asp:Label>
                <asp:TextBox ID="txtFrom" runat="server" TextMode="Date" CssClass="form-control" data-live-btn="btnApply"></asp:TextBox>
            </div>
            <div class="col-md-3">
                <asp:Label runat="server" AssociatedControlID="txtTo" Text="To date" CssClass="form-label"></asp:Label>
                <asp:TextBox ID="txtTo" runat="server" TextMode="Date" CssClass="form-control" data-live-btn="btnApply"></asp:TextBox>
            </div>
            <div class="col-md-6">
                <asp:Button ID="btnApply" runat="server" Text="Apply" CssClass="btn btn-primary me-2"
                    CausesValidation="false" OnClick="btnApply_Click" />
                <asp:Button ID="btnLast30" runat="server" Text="Last 30 days" CssClass="btn btn-outline-secondary me-1"
                    CausesValidation="false" OnClick="btnLast30_Click" />
                <asp:Button ID="btnThisMonth" runat="server" Text="This month" CssClass="btn btn-outline-secondary me-1"
                    CausesValidation="false" OnClick="btnThisMonth_Click" />
                <asp:Button ID="btnAllTime" runat="server" Text="All time" CssClass="btn btn-outline-secondary"
                    CausesValidation="false" OnClick="btnAllTime_Click" />
            </div>
        </div>
    </asp:Panel>

    <asp:Panel ID="pnlResults" runat="server">

        <p class="qcu-range">Showing reservations dated <strong><asp:Label ID="lblRange" runat="server"></asp:Label></strong></p>

        <%-- ================= Summary cards ================= --%>
        <div class="row g-3 mb-4">
            <div class="col-6 col-lg-3">
                <div class="card card-body qcu-stat qcu-stat-blue h-100">
                    <div class="stat-label">Total bookings</div>
                    <div class="stat-value"><asp:Label ID="lblTotal" runat="server"></asp:Label></div>
                </div>
            </div>
            <div class="col-6 col-lg-3">
                <div class="card card-body qcu-stat qcu-stat-orange h-100">
                    <div class="stat-label">No-show rate</div>
                    <div class="stat-value"><asp:Label ID="lblNoShowPct" runat="server"></asp:Label></div>
                    <div class="stat-note"><asp:Label ID="lblNoShowDetail" runat="server"></asp:Label></div>
                </div>
            </div>
            <div class="col-6 col-lg-3">
                <div class="card card-body qcu-stat qcu-stat-red h-100">
                    <div class="stat-label">Currently restricted</div>
                    <div class="stat-value"><asp:Label ID="lblRestricted" runat="server"></asp:Label></div>
                    <div class="stat-note">Right now (not affected by the date range)</div>
                </div>
            </div>
            <div class="col-6 col-lg-3">
                <div class="card card-body qcu-stat qcu-stat-green h-100">
                    <div class="stat-label">Average group size</div>
                    <div class="stat-value"><asp:Label ID="lblAvgGroup" runat="server"></asp:Label></div>
                </div>
            </div>
            <div class="col-md-4">
                <div class="card card-body qcu-stat qcu-stat-blue h-100">
                    <div class="stat-label">Most booked room</div>
                    <div class="stat-value"><asp:Label ID="lblMostRoom" runat="server"></asp:Label></div>
                </div>
            </div>
            <div class="col-md-4">
                <div class="card card-body qcu-stat qcu-stat-grey h-100">
                    <div class="stat-label">Least booked room</div>
                    <div class="stat-value"><asp:Label ID="lblLeastRoom" runat="server"></asp:Label></div>
                </div>
            </div>
            <div class="col-md-4">
                <div class="card card-body qcu-stat qcu-stat-orange h-100">
                    <div class="stat-label">Peak time slot</div>
                    <div class="stat-value"><asp:Label ID="lblPeakSlot" runat="server"></asp:Label></div>
                </div>
            </div>
        </div>

        <%-- ================= Tables with bars ================= --%>
        <div class="row">
            <div class="col-lg-6">
                <div class="card mb-3">
                    <div class="card-header">Status breakdown</div>
                    <div class="card-body p-2">
                        <div class="table-responsive">
                        <asp:GridView ID="gvStatus" runat="server" AutoGenerateColumns="false"
                            CssClass="table table-sm table-bordered align-middle mb-0"
                            EmptyDataText="No reservations in this range." OnRowDataBound="gvBar_RowDataBound">
                            <Columns>
                                <asp:BoundField DataField="Status" HeaderText="Status" />
                                <asp:BoundField DataField="Total" HeaderText="Total" ItemStyle-Width="70px" />
                                <asp:TemplateField HeaderText="Share">
                                    <ItemTemplate><asp:Literal ID="litBar" runat="server"></asp:Literal></ItemTemplate>
                                </asp:TemplateField>
                            </Columns>
                        </asp:GridView>
                        </div>
                    </div>
                </div>
            </div>

            <div class="col-lg-6">
                <div class="card mb-3">
                    <div class="card-header">Bookings per month</div>
                    <div class="card-body p-2">
                        <div class="table-responsive">
                        <asp:GridView ID="gvMonth" runat="server" AutoGenerateColumns="false"
                            CssClass="table table-sm table-bordered align-middle mb-0"
                            EmptyDataText="No reservations in this range." OnRowDataBound="gvBar_RowDataBound">
                            <Columns>
                                <asp:BoundField DataField="Month" HeaderText="Month" />
                                <asp:BoundField DataField="Total" HeaderText="Total" ItemStyle-Width="70px" />
                                <asp:TemplateField HeaderText="Trend">
                                    <ItemTemplate><asp:Literal ID="litBar" runat="server"></asp:Literal></ItemTemplate>
                                </asp:TemplateField>
                            </Columns>
                        </asp:GridView>
                        </div>
                    </div>
                </div>
            </div>

            <div class="col-lg-6">
                <div class="card mb-3">
                    <div class="card-header">Bookings per weekday</div>
                    <div class="card-body p-2">
                        <div class="table-responsive">
                        <asp:GridView ID="gvWeekday" runat="server" AutoGenerateColumns="false"
                            CssClass="table table-sm table-bordered align-middle mb-0"
                            EmptyDataText="No reservations in this range." OnRowDataBound="gvBar_RowDataBound">
                            <Columns>
                                <asp:BoundField DataField="Day" HeaderText="Day" />
                                <asp:BoundField DataField="Total" HeaderText="Total" ItemStyle-Width="70px" />
                                <asp:TemplateField HeaderText="Trend">
                                    <ItemTemplate><asp:Literal ID="litBar" runat="server"></asp:Literal></ItemTemplate>
                                </asp:TemplateField>
                            </Columns>
                        </asp:GridView>
                        </div>
                    </div>
                </div>
            </div>

            <div class="col-lg-6">
                <div class="card mb-3">
                    <div class="card-header">Peak time slots (booked or forfeited)</div>
                    <div class="card-body p-2">
                        <div class="table-responsive">
                        <asp:GridView ID="gvSlot" runat="server" AutoGenerateColumns="false"
                            CssClass="table table-sm table-bordered align-middle mb-0"
                            EmptyDataText="No time slots found." OnRowDataBound="gvBar_RowDataBound">
                            <Columns>
                                <asp:BoundField DataField="Slot" HeaderText="Time slot" />
                                <asp:BoundField DataField="Total" HeaderText="Total" ItemStyle-Width="70px" />
                                <asp:TemplateField HeaderText="Demand">
                                    <ItemTemplate><asp:Literal ID="litBar" runat="server"></asp:Literal></ItemTemplate>
                                </asp:TemplateField>
                            </Columns>
                        </asp:GridView>
                        </div>
                    </div>
                </div>
            </div>

            <div class="col-12">
                <div class="card mb-3">
                    <div class="card-header">Room utilization (confirmed bookings vs. available slots)</div>
                    <div class="card-body p-2">
                        <div class="table-responsive">
                        <asp:GridView ID="gvRoom" runat="server" AutoGenerateColumns="false"
                            CssClass="table table-sm table-bordered align-middle mb-0"
                            EmptyDataText="No rooms found." OnRowDataBound="gvBar_RowDataBound">
                            <Columns>
                                <asp:BoundField DataField="RoomName" HeaderText="Room" />
                                <asp:BoundField DataField="Booked" HeaderText="Booked" ItemStyle-Width="80px" />
                                <asp:BoundField DataField="Available" HeaderText="Available" ItemStyle-Width="90px" />
                                <asp:BoundField DataField="UtilizationPct" HeaderText="Utilization %" DataFormatString="{0:0.0}%"
                                    NullDisplayText="&mdash;" ItemStyle-Width="110px" />
                                <asp:TemplateField HeaderText="">
                                    <ItemTemplate><asp:Literal ID="litBar" runat="server"></asp:Literal></ItemTemplate>
                                </asp:TemplateField>
                            </Columns>
                        </asp:GridView>
                        </div>
                    </div>
                </div>
            </div>

            <div class="col-lg-6">
                <div class="card mb-3">
                    <div class="card-header">Bookings per program</div>
                    <div class="card-body p-2">
                        <div class="table-responsive">
                        <asp:GridView ID="gvProgram" runat="server" AutoGenerateColumns="false"
                            CssClass="table table-sm table-bordered align-middle mb-0"
                            EmptyDataText="No reservations in this range." OnRowDataBound="gvBar_RowDataBound">
                            <Columns>
                                <asp:BoundField DataField="Program" HeaderText="Program" />
                                <asp:BoundField DataField="Total" HeaderText="Total" ItemStyle-Width="70px" />
                                <asp:TemplateField HeaderText="Share">
                                    <ItemTemplate><asp:Literal ID="litBar" runat="server"></asp:Literal></ItemTemplate>
                                </asp:TemplateField>
                            </Columns>
                        </asp:GridView>
                        </div>
                    </div>
                </div>
            </div>

            <div class="col-lg-6">
                <div class="card mb-3">
                    <div class="card-header">Library assistant decisions</div>
                    <div class="card-body p-2">
                        <div class="table-responsive">
                        <asp:GridView ID="gvDecisions" runat="server" AutoGenerateColumns="false"
                            CssClass="table table-sm table-bordered align-middle mb-0"
                            EmptyDataText="No assistant decisions in this range." OnRowDataBound="gvBar_RowDataBound">
                            <Columns>
                                <asp:BoundField DataField="NewStatus" HeaderText="Decision" />
                                <asp:BoundField DataField="Total" HeaderText="Total" ItemStyle-Width="70px" />
                                <asp:TemplateField HeaderText="Share">
                                    <ItemTemplate><asp:Literal ID="litBar" runat="server"></asp:Literal></ItemTemplate>
                                </asp:TemplateField>
                            </Columns>
                        </asp:GridView>
                        </div>
                    </div>
                </div>
            </div>

            <%-- ================= Plain tables ================= --%>
            <div class="col-lg-6">
                <div class="card mb-3">
                    <div class="card-header">No-shows per student</div>
                    <div class="card-body p-2">
                        <div class="table-responsive">
                        <asp:GridView ID="gvNoShowStudents" runat="server" AutoGenerateColumns="false"
                            CssClass="table table-sm table-bordered table-striped mb-0"
                            EmptyDataText="No forfeited reservations in this range.">
                            <Columns>
                                <asp:BoundField DataField="StudentNumber" HeaderText="ID / Student No." />
                                <asp:BoundField DataField="Student" HeaderText="Name" />
                                <asp:BoundField DataField="Forfeitures" HeaderText="Forfeitures" ItemStyle-Width="100px" />
                            </Columns>
                        </asp:GridView>
                        </div>
                    </div>
                </div>
            </div>

            <div class="col-lg-6">
                <div class="card mb-3">
                    <div class="card-header">Top repeat bookers (top 10)</div>
                    <div class="card-body p-2">
                        <div class="table-responsive">
                        <asp:GridView ID="gvTopBookers" runat="server" AutoGenerateColumns="false"
                            CssClass="table table-sm table-bordered table-striped mb-0"
                            EmptyDataText="No reservations in this range.">
                            <Columns>
                                <asp:BoundField DataField="StudentNumber" HeaderText="ID / Student No." />
                                <asp:BoundField DataField="Student" HeaderText="Name" />
                                <asp:BoundField DataField="Bookings" HeaderText="Bookings" ItemStyle-Width="100px" />
                            </Columns>
                        </asp:GridView>
                        </div>
                    </div>
                </div>
            </div>
        </div>

    </asp:Panel>
</ContentTemplate>
</asp:UpdatePanel>
</asp:Content>