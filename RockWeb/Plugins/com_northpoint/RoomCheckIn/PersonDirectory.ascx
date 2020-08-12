﻿<%@ Control Language="C#" AutoEventWireup="true" CodeFile="PersonDirectory.ascx.cs" Inherits="RockWeb.Plugins.com_northpoint.RoomCheckIn.PersonDirectory" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>

        <script>
            Sys.Application.add_load( function () {
                $("div.photo-round").lazyload({
                    effect: "fadeIn"
                });
            });
        </script>

        <asp:Panel ID="pnlView" runat="server" CssClass="panel panel-block" DefaultButton="lbSearch">
        
            <div class="panel-heading">
                <h1 class="panel-title"><i class="fa fa-users"></i> Directory</h1>
            </div>
            <div class="panel-body">

                <Rock:NotificationBox ID="nbValidation" runat="server" NotificationBoxType="Warning" />

                <div class="row">
                    <div class="col-sm-4">
                        <Rock:PhoneNumberBox ID="tbPhone" runat="server" Label="Phone" />
                    </div>
                    <div class="col-sm-4">
                        <Rock:RockTextBox ID="tbFirstName" runat="server" Label="First Name" />
                    </div>
                    <div class="col-sm-4">
                        <Rock:RockTextBox ID="tbLastName" runat="server" Label="Last Name" />
                    </div>
                    <div class="col-sm-12">
                        <div class="form-group">
                            <label class="control-label">&nbsp;</label>
                            <div class="control-wrapper">
                                <asp:LinkButton ID="lbSearch" runat="server" Text="Search" CssClass="btn btn-primary" OnClick="lbSearch_Click"  />
                                <asp:LinkButton ID="lbClear" runat="server" Text="Clear" CssClass="btn btn-link" OnClick="lbClear_Click" />
                            </div>
                        </div>
                    </div>
                </div>

                <ul class="list-inline directory-letters">
                    <asp:PlaceHolder ID="phLetters" runat="server" />
                </ul>

                <div class="directory-grid">

                    <asp:Repeater ID="rptPeople" runat="server" >
                        <ItemTemplate>
                            <div class="row padding-v-lg">
                                <asp:Literal ID="lPerson" runat="server" />
                                <asp:Literal ID="lAddress" runat="server" />
                                <asp:Literal ID="lPhones" runat="server" />
                                <asp:Literal ID="lEverythingElse" runat="server" />
                            </div>
                        </ItemTemplate>
                    </asp:Repeater>

                    <asp:Repeater ID="rptFamilies" runat="server" >
                        <ItemTemplate>
                            <div class="row padding-v-lg">
                                <div class="col-sm-3 margin-b-lg">
                                    <asp:Literal ID="lFamily" runat="server" />
                                </div>
                                <div class="col-sm-9">
                                    <asp:Repeater ID="rptFamilyPeople" runat="server" >
                                        <ItemTemplate>
                                            <div class="row margin-b-md">
                                                <asp:Literal ID="lPerson" runat="server" />
                                                <asp:Literal ID="lPhones" runat="server" />
                                                <asp:Literal ID="lEverythingElse" runat="server" />
                                            </div>
                                        </ItemTemplate>
                                    </asp:Repeater>
                                </div>
                            </div>
                        </ItemTemplate>
                    </asp:Repeater>

                </div>

                <Rock:BootstrapButton ID="btnNewFamily" runat="server" CssClass="btn btn-block btn-success" Text="Add New Family" OnClick="btnNewFamily_Click" />


            </div>
        
        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>
