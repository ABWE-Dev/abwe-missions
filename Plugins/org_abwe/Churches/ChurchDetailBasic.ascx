﻿<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ChurchDetailBasic.ascx.cs" Inherits="RockWeb.Plugins.org_abwe.RockMissions.ChurchDetail"%>

<asp:UpdatePanel ID="upnlChurches" runat="server">
    <ContentTemplate>
        <asp:Panel ID="pnlDetails" runat="server">

            <div class="panel panel-block">
                <div class="panel-heading">
                    <h1 class="panel-title"><i class="fa fa-church"></i>
                        <asp:Literal ID="lTitle" runat="server" /></h1>
                    <div class="panel-labels">
                        <Rock:HighlightLabel ID="hlStatus" runat="server" />
                    </div>
                </div>
                <Rock:PanelDrawer ID="pdAuditDetails" runat="server"></Rock:PanelDrawer>
                <div class="panel-body">
                    <Rock:NotificationBox ID="nbErrorMessage" runat="server" NotificationBoxType="Danger" />
                    <div id="pnlEditDetails" runat="server">
                        <asp:ValidationSummary ID="valSummary" runat="server" HeaderText="Please correct the following:" CssClass="alert alert-validation" />
                        <asp:HiddenField ID="hfChurchId" runat="server" />
                        <Rock:NotificationBox ID="nbWarningMessage" runat="server" NotificationBoxType="Warning" />

                        <div class="row">
                            <div class="col-md-3">
                                <fieldset>
                                    <Rock:DefinedValuePicker ID="dvpRecordStatus" runat="server" Label="Record Status" AutoPostBack="true" OnSelectedIndexChanged="ddlRecordStatus_SelectedIndexChanged"/>
                                    <Rock:DefinedValuePicker ID="dvpReason" runat="server" Label="Reason" Visible="false"></Rock:DefinedValuePicker>
                                </fieldset>
                            </div>
                            <div class="col-md-9">

                                <Rock:DataTextBox ID="tbChurchName" runat="server" Label="Name" SourceTypeName="Rock.Model.Person, Rock" PropertyName="FirstName" Required="true"/>

                                <Rock:AddressControl ID="acAddress" runat="server" UseStateAbbreviation="true" UseCountryAbbreviation="false" />

                                <div class="row">
                                    <div class="col-sm-6">
                                        <Rock:PhoneNumberBox ID="pnbPhone" runat="server" Label="Phone Number" CountryCode='<%# Eval("CountryCode") %>' Number='<%# Eval("NumberFormatted")  %>' />
                                    </div>
                                    <div class="col-sm-6">
                                        <div class="row">
                                            <div class="col-xs-6">
                                                <Rock:RockCheckBox ID="cbSms" runat="server" Text="SMS" Label="&nbsp;" Checked='<%# (bool)Eval("IsMessagingEnabled") %>' />
                                            </div>
                                            <div class="col-xs-6">
                                                <Rock:RockCheckBox ID="cbUnlisted" runat="server" Text="Unlisted" Label="&nbsp;" Checked='<%# (bool)Eval("IsUnlisted") %>' />
                                            </div>
                                        </div>
                                    </div>
                                </div>

                                <Rock:EmailBox ID="tbEmail" runat="server" Label="Email Address" />

                                <Rock:RockRadioButtonList ID="rblEmailPreference" runat="server" RepeatDirection="Horizontal" Label="Email Preference">
                                    <asp:ListItem Text="Email Allowed" Value="EmailAllowed" Selected="True" />
                                    <asp:ListItem Text="No Mass Emails" Value="NoMassEmails" />
                                    <asp:ListItem Text="Do Not Email" Value="DoNotEmail" />
                                </Rock:RockRadioButtonList>

                                <div class="actions">
                                    <asp:LinkButton ID="lbSave" runat="server" Text="Save" CssClass="btn btn-primary" OnClick="lbSave_Click" />
                                    <asp:LinkButton ID="lbCancel" runat="server" Text="Cancel" CssClass="btn btn-link" CausesValidation="false" OnClick="lbCancel_Click" />
                                </div>
                            </div>
                        </div>
                    </div>

                    <fieldset id="fieldsetViewSummary" runat="server">
                        <div class="row">
                            <div class="col-md-12">
                                <Rock:NotificationBox ID="nbEditModeMessage" runat="server" NotificationBoxType="Info" />
                            </div>
                        </div>
                        <div class="row">
                            <div class="col-md-6">
                                <asp:Literal ID="lDetailsLeft" runat="server" />
                            </div>
                            <div class="col-md-6">
                                <asp:Literal ID="lDetailsRight" runat="server" />
                            </div>
                        </div>
                        <div class="actions">
                            <asp:LinkButton ID="lbEdit" runat="server" Text="Edit" CssClass="btn btn-primary" CausesValidation="false" OnClick="lbEdit_Click" />
                        </div>
                    </fieldset>
                </div>
            </div>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
