﻿<%@ Page Title="" Language="C#" MasterPageFile="~/App_Master/GeneralPage.master" AutoEventWireup="true" CodeFile="CreateEditFolder.aspx.cs" Inherits="documents_CreateEditFolder" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="Server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="TopMenu" runat="Server">
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="BreadcrumbBar" runat="Server">
    <asp:HyperLink ID="hlFolderContext" runat="server" />
    <asp:Repeater ID="rptParentFolders" runat="server">
        <ItemTemplate>
            &gt; <a href="BrowseFileFolder.aspx?contextID=<%#DataBinder.Eval(Container.DataItem,"FolderID") %>">
                <%#DataBinder.Eval(Container.DataItem,"FolderName") %></a>
        </ItemTemplate>
    </asp:Repeater>
</asp:Content>
<asp:Content ID="Content4" ContentPlaceHolderID="PageTitle" runat="Server">
    Create/Edit a Folder
</asp:Content>
<asp:Content ID="Content5" ContentPlaceHolderID="TopRightContent" runat="Server">
</asp:Content>
<asp:Content ID="Content6" ContentPlaceHolderID="PageContent" runat="Server">
    <asp:Literal ID="lPageText" runat="server" />
    <table>
      <tr>
            <td style="width: 150px" class="columnHeader">
                Folder Type: <span class="requiredField">*</span>
                
            </td>
            <td>
              <asp:RadioButtonList ID="rblType" runat="server">
               <asp:ListItem Text="Private - Only people with access to the folder can download files" Value="Private"  Selected=True />
               <asp:ListItem Text="Public - Anyone with the file URL can download the file" Value="Public" />
              </asp:RadioButtonList>
              
            </td>
        </tr>
        <tr>
            <td style="width: 150px" class="columnHeader">
                Name: <span class="requiredField">*</span>
                
            </td>
            <td>
              <asp:TextBox ID="tbName" runat="server" />
                <asp:RequiredFieldValidator ID="RequiredFieldValidator1" runat="server" ControlToValidate="tbName" ErrorMessage="ERROR: Please specify a file name."
                    Display="Dynamic" ForeColor=Red />
            </td>
        </tr>
    </table>
    <asp:CheckBox ID="cbAdmin" Text="Only allow leaders/administrators to make changes or upload files to this folder" runat="server" />
    <h2>
        Description</h2>
    <asp:TextBox ID="tbDescription" runat="server" TextMode="MultiLine" Rows="10" Columns="100" />
    <hr />
    <div align="center">
        <asp:Button ID="btnUpload" Text="Save Changes" runat="server" OnClick="btnSaveChanges_Click" />
        <asp:Button ID="btnCancel" Text="Cancel" CausesValidation="false" runat="server"
            OnClick="btnCancel_Click" />
    </div>
</asp:Content>
<asp:Content ID="Content7" ContentPlaceHolderID="FooterContent" runat="Server">
</asp:Content>
