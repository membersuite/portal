﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using MemberSuite.SDK;
using MemberSuite.SDK.Constants;
using MemberSuite.SDK.Searching;
using MemberSuite.SDK.Searching.Operations;
using MemberSuite.SDK.Types;

public partial class exhibits_RegisterForBoothTypes : PortalPage 
{
    public msExhibitorRegistrationWindow targetWindow;
    public msEntity targetEntity;
    public msExhibitShow targetShow;

    protected override void InitializeTargetObject()
    {
        base.InitializeTargetObject();

        targetWindow = LoadObjectFromAPI<msExhibitorRegistrationWindow>(ContextID);
        if (targetWindow == null) GoToMissingRecordPage();
        targetEntity = LoadObjectFromAPI<msEntity>(Request.QueryString["entityID"]);
        if (targetEntity == null) GoToMissingRecordPage();

        targetShow = LoadObjectFromAPI<msExhibitShow>(targetWindow.Show);
    }

    protected override bool CheckSecurity()
    {
        if (!base.CheckSecurity()) return false;

        using (var api = GetServiceAPIProxy())
        {
            var ps =
                api.GetAvailableExhibitorRegistrationWindows(targetWindow.Show, targetEntity.ID).ResultValue.Permissions;
            if (ps.Count == 0 || ps[0].RegistrationMode != ExhibitorRegistrationMode.PurchaseBoothsByType )
                return false;
        }

        return true;
    }

    protected override void InitializePage()
    {
        base.InitializePage();
        lMainInstructions.Text = targetShow.RegistrationInstructions;
        lRegistrationWindowInstructions.Text = targetWindow.RegistrationInstructions;

        if (targetShow.ShowFloor != null)
            lShowFloor.NavigateUrl = GetImageUrl(targetShow.ShowFloor);
        else
            lShowFloor.Visible = false;

        Search s = new Search(msExhibitBoothTypeProduct.CLASS_NAME);
        s.AddCriteria(Expr.Equals("Show", targetShow.ID));
        s.AddCriteria(Expr.Equals("IsActive", true));
        s.AddCriteria(Expr.Equals("SellOnline", true));
        s.AddSortColumn("DisplayOrder");
        s.AddSortColumn("Name");

        List<string> products = new List<string>();
        foreach (System.Data.DataRow dr in ExecuteSearch(s, 0, null).Table.Rows)
            products.Add(Convert.ToString(dr["ID"]));
        using (var api = GetServiceAPIProxy())
        {
            var describedProducts = api.DescribeProducts(targetEntity.ID, products).ResultValue;

            foreach (var pi in describedProducts)
            {

                string name = string.Format("{0} - <font color=green>{1}</font>",
                                            pi.ProductName, pi.DisplayPriceAs ?? pi.Price.ToString("C"));
                rblBoothTypes.Items.Add(new ListItem(name, pi.ProductID));
            }
        }

        bindExhibitorMerchandise();
    }

 

    

    private msOrder unbindOrder(List<string> booths )
    {
        // ok, let's create our order
        msOrder o = new msOrder();
        o.ShipTo = o.BillTo = targetEntity.ID;
        o.LineItems = new List<msOrderLineItem>();

        // add the primary booth

        var oli = new msOrderLineItem {Product = rblBoothTypes.SelectedValue, Quantity = 1};
        oli.Options = new List<NameValueStringPair>();
        oli.Options.Add(new NameValueStringPair { Name = OrderLineItemOptions.Exhibits.SpecialRequests, Value = tbSpecialRequest.Text });

        // now, the preferences
        string prefs = "";
        foreach (string s in booths )
            prefs += s + "|";

        oli.Options.Add(new NameValueStringPair(OrderLineItemOptions.Exhibits.BoothPreferences, prefs));
        o.LineItems.Add(oli);

         unbindMerchandise(o);

        return o;
    }




    private List<ExhibitBoothInfo> openBooths;


    private void bindExhibitorMerchandise()
    {
        Search s = new Search(msExhibitorMerchandise.CLASS_NAME);
        s.AddCriteria(Expr.Equals("Show", targetShow.ID));
        s.AddCriteria(Expr.Equals("IsActive", true));
        s.AddCriteria(Expr.Equals("SellOnline", true));

        s.AddSortColumn("DisplayOrder");
        s.AddSortColumn("Name");

        List<string> products = new List<string>();
        foreach (System.Data.DataRow dr in ExecuteSearch(s, 0, null).Table.Rows)
            products.Add(Convert.ToString(dr["ID"]));

        if (products.Count > 0)
        {
            divOtherProducts.Visible = true;
            using (var api = GetServiceAPIProxy())
            {
                var describedProducts = api.DescribeProducts(targetEntity.ID, products).ResultValue;


                rptAdditionalItems.DataSource = describedProducts;
                rptAdditionalItems.DataBind();
            }
        }


    }
 
    private void unbindMerchandise(msOrder mso)
    {

        if (!divOtherProducts.Visible)
            return;

        foreach (RepeaterItem ri in rptAdditionalItems.Items)
        {
            TextBox tbQuantity = (TextBox)ri.FindControl("tbQuantity");
            HiddenField hfProductID = (HiddenField)ri.FindControl("hfProductID");

            msOrderLineItem li = new msOrderLineItem();
            li.Quantity = decimal.Parse(tbQuantity.Text);
            if (li.Quantity <= 0)
                continue; // don't add

            li.Product = hfProductID.Value;

            mso.LineItems.Add(li);

        }
    }

    protected void rptAdditionalItems_ItemDataBound(object sender, RepeaterItemEventArgs e)
    {
        ProductInfo pi = (ProductInfo)e.Item.DataItem;

        if (Page.IsPostBack)
            return;				// only do this if there's a postback - otherwise, preserve ViewState

        switch (e.Item.ItemType)
        {
            case ListItemType.Header:
                break;

            case ListItemType.Footer:
                break;

            case ListItemType.AlternatingItem:
                goto case ListItemType.Item;

            case ListItemType.Item:
                TextBox tbQuantity = (TextBox)e.Item.FindControl("tbQuantity");
                CompareValidator cvQuantity = (CompareValidator)e.Item.FindControl("cvQuantity");
                Label lblProductName = (Label)e.Item.FindControl("lblProductName");
                Label lblProductPrice = (Label)e.Item.FindControl("lblProductPrice");
                HiddenField hfProductID = (HiddenField)e.Item.FindControl("hfProductID");

                hfProductID.Value = pi.ProductID;


                cvQuantity.ErrorMessage = string.Format("You have entered an invalid donation amount for {0}", pi.ProductName);
                lblProductName.Text = pi.ProductName;
                lblProductPrice.Text = pi.DisplayPriceAs ?? pi.Price.ToString("C");

                break;
        }
    }

    protected void wzBoothType_Cancel(object sender, EventArgs e)
    {
        GoTo("ViewShow.aspx?contextID=" + targetShow.ID);
    }

    protected void rptChoices_OnItemDataBound(object sender, RepeaterItemEventArgs e)
    {
        
        switch (e.Item.ItemType)
        {
            case ListItemType.Header:
                break;

            case ListItemType.Footer:
                break;

            case ListItemType.AlternatingItem:
                goto case ListItemType.Item;

            case ListItemType.Item:
                Literal lChoiceLabel = (Literal)e.Item.FindControl("lChoiceLabel");
                DropDownList ddlChoice = (DropDownList)e.Item.FindControl("ddlChoice");

                lChoiceLabel.Text = string.Format("Choice #{0}", e.Item.ItemIndex + 1);

                ddlChoice.DataSource = openBooths;
                ddlChoice.DataTextField = "BoothName";
                ddlChoice.DataValueField = "BoothID";
                ddlChoice.DataBind();

                ddlChoice.Items.Insert(0, new ListItem(" --- Select a Booth ---", ""));

                break;
        }
    }

    protected void wzBoothType_Next(object sender, WizardNavigationEventArgs e)
    {

        if (string.IsNullOrWhiteSpace(rblBoothTypes.SelectedValue))
        {
         
            e.Cancel = true;
            cvAtLeastOneBoothType.IsValid = false;
            return;
        }

        using (var api = GetServiceAPIProxy())
            lblBoothType.Text = api.GetName(rblBoothTypes.SelectedValue).ResultValue;

        setupBoothChoices();

    }

    protected void wzBoothType_Finish(object sender, WizardNavigationEventArgs e)
    {
        List<String> booths = new List<string>();
        foreach (RepeaterItem ri in rptChoices.Items)
        {
            DropDownList ddlChoice = (DropDownList)ri.FindControl("ddlChoice");
            if (string.IsNullOrWhiteSpace(ddlChoice.SelectedValue)) continue;

            booths.Add(ddlChoice.SelectedValue);
        }

        if (booths.Count == 0)
        {
            cvAtLeastOneBooth.IsValid = false;
            return;
        }

        var o = unbindOrder( booths );
         

        ExhibitorConfirmationPacket p = new ExhibitorConfirmationPacket();
        p.SpecialRequests = tbSpecialRequest.Text;
        p.BoothPreferences = booths;
        MultiStepWizards.PlaceAnOrder.OrderConfirmaionPacket = p;
        MultiStepWizards.PlaceAnOrder.OrderCompleteUrl = "/exhibits/ViewShow.aspx?contextID=" + targetShow.ID;
        MultiStepWizards.PlaceAnOrder.InitiateOrderProcess(o);

    
     
    }

    
    protected void setupBoothChoices()
    {
         int numberOfChoices = GetNumberOfChoices();

        object[] emptyRows = new object[numberOfChoices];

        using (var api = GetServiceAPIProxy())
            openBooths = api.GetAvaialbleExhibitBooths(targetShow.ID, targetEntity.ID).ResultValue;

        // remove all types that don't match
        var boothProduct = LoadObjectFromAPI<msExhibitBoothTypeProduct>(rblBoothTypes.SelectedValue);
        openBooths.RemoveAll(x => x.BoothTypeID.ToLower() != boothProduct.BoothType.ToLower());

        rptChoices.DataSource = emptyRows;
        rptChoices.DataBind();

         
    }

    private int GetNumberOfChoices()
    {
        return 3;   // for now, though we might make this configruable
    }


}