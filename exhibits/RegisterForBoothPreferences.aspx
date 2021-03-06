﻿<%@ Page Title="" Language="C#" MasterPageFile="~/App_Master/GeneralPage.master"
    AutoEventWireup="true" CodeFile="RegisterForBoothPreferences.aspx.cs" Inherits="exhibits_RegisterForBoothPreferences" %>

<%@ Register Assembly="MemberSuite.SDK.Web" Namespace="MemberSuite.SDK.Web.Controls"
    TagPrefix="cc1" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="Server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="TopMenu" runat="Server">
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="BreadcrumbBar" runat="Server">
    <a href="/exhibits/ViewShow.aspx?contextID=<%=targetShow.ID %>">
        <%=targetShow.Name%>
        > </a>
</asp:Content>
<asp:Content ID="Content4" ContentPlaceHolderID="PageTitle" runat="Server">
    <asp:Literal runat="server" ID="CustomTitle"></asp:Literal>
</asp:Content>
<asp:Content ID="Content5" ContentPlaceHolderID="TopRightContent" runat="Server">
</asp:Content>
<asp:Content ID="Content6" ContentPlaceHolderID="PageContent" runat="Server">
    <asp:Literal ID="lPageText" runat="server">During this registration window, you can select your preferences for booths. Booths will
    be assigned to you at a later date.</asp:Literal>
    
    <asp:Literal ID="lMainInstructions" runat="server" /><br />
    <asp:Literal ID="lRegistrationWindowInstructions" runat="server" /><br />
    <asp:HyperLink ID="lShowFloor" runat="server" Text="Download Show Floor Layout<br /><br />" />
    <h2>
        <asp:Literal ID="lSelectBooths" runat="server">Booth Preferences:</asp:Literal></h2>
        <asp:CustomValidator ID="cvAtLeastOneBooth" runat="server" ForeColor=Red ErrorMessage="Error: You must select at least one booth." Display=Dynamic />
    <table>
        <asp:Repeater ID="rptChoices" runat="server" OnItemDataBound="rptChoices_OnItemDataBound">
            <ItemTemplate>
                <tr>
                    <td style="width: 150px">
                        <asp:Literal ID="lChoiceLabel" runat="server">1st Choice:</asp:Literal>
                    </td>
                    <td>
                        <asp:DropDownList ID="ddlChoice" runat="server" />
                    </td>
                </tr>
            </ItemTemplate>
        </asp:Repeater>
    </table>
    <h2>
        Special Requests</h2>
    <asp:Literal ID="lSpecialRequestInstructions" runat="server">Use the space below to enter any special request you have for your exhibit.</asp:Literal>
    <br />
    <asp:TextBox ID="tbSpecialRequest" Columns="125" Rows="10" TextMode="MultiLine" runat="server" />
    <div style="padding-top: 30px">
        <asp:Literal ID="lAfterRegComplete" runat="server">
    After your registration is complete, you will have a chance to upload your booth
    logo and bio.</asp:Literal>
    </div>
    <hr />
    <div align="center">
        <asp:Button ID="btnSave" runat="server" Text="Continue" OnClick="btnContinue_Click" />
        <asp:Button ID="btnCancel" runat="server" Text="Cancel" CausesValidation="False"
            OnClick="btnCancel_Click" />
    </div>
</asp:Content>
<asp:Content ID="Content7" ContentPlaceHolderID="FooterContent" runat="Server">
</asp:Content>
