<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="ConfigurePayment.ascx.cs" Inherits="Vipps.CommerceManager.Apps.Order.Payments.Plugins.Vipps.ConfigurePayment" %>

 <asp:UpdatePanel UpdateMode="Conditional" ID="ConfigureUpdatePanelContentPanel" runat="server" RenderMode="Inline" ChildrenAsTriggers="true">
        <ContentTemplate>
            <style>
                .vipps-parameters table.DataForm tbody tr td.FormLabelCell { width: 200px; }
                .vipps-parameters h2 { margin-top: 20px }
                .vipps-parameters-url { width: 500px; }
                .vipps-paramaters-message { width: 700px;}
                .vipps-parameters .aspNetDisabled {
                    background-color: #dddddd !important;
                }
            </style>

            <div class="vipps-parameters">

            <h2>Market</h2>

            <table class="DataForm">
                <tbody>
                     <tr>
                        <td class="FormLabelCell">Select a market:</td>
                        <td class="FormFieldCell">
                            <asp:DropDownList runat="server" ID="marketDropDownList" OnSelectedIndexChanged="marketDropDownList_OnSelectedIndexChanged" AutoPostBack="True" />
                        </td>
                    </tr>
                </tbody>
            </table>
                
            <h2>Vipps connection setting</h2>


            <table class="DataForm">
                <tbody>
                     <tr>
                        <td class="FormLabelCell">Client Id:</td>
                        <td class="FormFieldCell">
                            <asp:TextBox runat="server" ID="txtClientId" />
                            <asp:RequiredFieldValidator ID="requiredUsername" runat="server" ControlToValidate="txtClientId" ErrorMessage="Client Id is required." />
                        </td>
                    </tr>
                    <tr>
                        <td class="FormLabelCell">Client Secret:</td>
                        <td class="FormFieldCell">
                            <asp:TextBox runat="server" ID="txtClientSecret"  />
                            <asp:RequiredFieldValidator ID="requiredPassword" runat="server" ControlToValidate="txtClientSecret" ErrorMessage="Client secret is required." />
                        </td>
                    </tr>
                    <tr>
                        <td class="FormLabelCell">Subscription Key:</td>
                        <td class="FormFieldCell">
                            <asp:TextBox runat="server" ID="txtSubscriptionKey"  />
                            <asp:RequiredFieldValidator ID="requiredSubscriptionKey" runat="server" ControlToValidate="txtSubscriptionKey" ErrorMessage="Subscription key is required." />
                        </td>
                    </tr>
                    <tr>
                        <td class="FormLabelCell">Serial Number:</td>
                        <td class="FormFieldCell">
                            <asp:TextBox runat="server" ID="txtSerialNumber"  />
                            <asp:RequiredFieldValidator ID="requiredSerialNumber" runat="server" ControlToValidate="txtSerialNumber" ErrorMessage="Subscription key is required." />
                        </td>
                    </tr>
                    <tr>
                        <td class="FormLabelCell">System name:</td>
                        <td class="FormFieldCell">
                            <asp:TextBox runat="server" ID="txtSystemName"  />
                            <asp:RequiredFieldValidator ID="RequiredFieldValidator1" runat="server" ControlToValidate="txtSystemName" ErrorMessage="System name is required." />
                        </td>
                    </tr>
                    <tr>
                        <td class="FormLabelCell">Api Url:</td>
                        <td class="FormFieldCell">
                            <asp:TextBox runat="server" ID="txtApiUrl" CssClass="vipps-parameters-url" />
                            <asp:RequiredFieldValidator ID="requiredApiUrl" runat="server" ControlToValidate="txtApiUrl" ErrorMessage="Api URL is required." />
                        </td>
                    </tr>
                    <tr>
                        <td class="FormLabelCell">Site Base Url:</td>
                        <td class="FormFieldCell">
                            <asp:TextBox runat="server" ID="txtSiteBaseUrl" CssClass="vipps-parameters-url" />
                            <asp:RequiredFieldValidator ID="requiredSiteBaseUrl" runat="server" ControlToValidate="txtSiteBaseUrl" ErrorMessage="Site base URL is required." />
                        </td>
                    </tr>
                    <tr>
                        <td class="FormLabelCell">Fallback Url:</td>
                        <td class="FormFieldCell">
                            <asp:TextBox runat="server" ID="txtFallbackUrl" CssClass="vipps-parameters-url" />
                            <asp:RequiredFieldValidator ID="requiredFallbackUrl" runat="server" ControlToValidate="txtFallbackUrl" ErrorMessage="Fallback URL is required." />
                        </td>
                    </tr>
                    <tr>
                        <td class="FormLabelCell">Transaction message:</td>
                        <td class="FormFieldCell">
                            <asp:TextBox runat="server" ID="txtTransactionMessage" CssClass="vipps-paramaters-message"/>
                            <asp:RequiredFieldValidator ID="requiredTransactionMessage" runat="server" ControlToValidate="txtTransactionMessage" ErrorMessage="Transaction message is required." />
                        </td>
                    </tr>
                </tbody>
            </table>
                
                
                <h2>LEGACY connection settings (needs to be manually migrated)</h2>
            <table class="DataForm">
                <tbody>
                     <tr>
                        <td class="FormLabelCell">Client Id:</td>
                        <td class="FormFieldCell">
                            <asp:TextBox runat="server" Enabled="false" ID="txtLegacyClientId" />
                        </td>
                    </tr>
                    <tr>
                        <td class="FormLabelCell">Client Secret:</td>
                        <td class="FormFieldCell">
                            <asp:TextBox runat="server" Enabled="false" ID="txtLegacyClientSecret"  />
                        </td>
                    </tr>
                    <tr>
                        <td class="FormLabelCell">Subscription Key:</td>
                        <td class="FormFieldCell">
                            <asp:TextBox runat="server" Enabled="false" ID="txtLegacySubscriptionKey"  />
                        </td>
                    </tr>
                    <tr>
                        <td class="FormLabelCell">Serial Number:</td>
                        <td class="FormFieldCell">
                            <asp:TextBox runat="server" Enabled="false" ID="txtLegacySerialNumber"  />
                        </td>
                    </tr>
                    <tr>
                        <td class="FormLabelCell">Api Url:</td>
                        <td class="FormFieldCell">
                            <asp:TextBox runat="server" Enabled="false" ID="txtLegacyApiUrl" CssClass="vipps-parameters-url" />
                        </td>
                    </tr>
                    <tr>
                        <td class="FormLabelCell">Site Base Url:</td>
                        <td class="FormFieldCell">
                            <asp:TextBox runat="server" Enabled="false" ID="txtLegacySiteBaseUrl" CssClass="vipps-parameters-url" />
                        </td>
                    </tr>
                    <tr>
                        <td class="FormLabelCell">Fallback Url:</td>
                        <td class="FormFieldCell">
                            <asp:TextBox runat="server" Enabled="false" ID="txtLegacyFallbackUrl" CssClass="vipps-parameters-url" />
                        </td>
                    </tr>
                    <tr>
                        <td class="FormLabelCell">Transaction message:</td>
                        <td class="FormFieldCell">
                            <asp:TextBox runat="server" Enabled="false" ID="txtLegacyTransactionMessage" CssClass="vipps-paramaters-message"/>
                        </td>
                    </tr>
                </tbody>
            </table>
        </div>
    </ContentTemplate>
 </asp:UpdatePanel>