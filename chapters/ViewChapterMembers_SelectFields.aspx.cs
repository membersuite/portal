﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using MemberSuite.SDK.Concierge;
using MemberSuite.SDK.Manifests.Command;
using MemberSuite.SDK.Manifests.Command.Views;
using MemberSuite.SDK.Manifests.Searching;
using MemberSuite.SDK.Results;
using MemberSuite.SDK.Searching;
using MemberSuite.SDK.Searching.Operations;
using MemberSuite.SDK.Types;
using MemberSuite.SDK.Utilities;
using Telerik.Web.UI;

public partial class chapters_ViewChapterMembers_SelectFields : PortalPage
{
    private const string ColumnHeaderOverridePrefix = "ColumnHeader.";

    protected SearchManifest targetSearchManifest;
    protected msChapter targetChapter;
    protected msMembershipOrganization targetMembershipOrganization;
    protected List<FieldMetadata> targetCriteriaFields;
    protected msMembershipLeader leader;
    private readonly List<string> defaultFieldFullPaths = new List<string> { "Membership.Individual.FirstName", "Membership.Individual.LastName", "Membership.Individual.EmailAddress" };

    protected bool Download
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Request.QueryString["download"]))
                return false;

            bool result;
            if (!bool.TryParse(Request.QueryString["download"], out result))
                return false;

            return result;
        }
    }

    protected bool Continue
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Request.QueryString["continue"]))
                return false;

            bool result;
            if (!bool.TryParse(Request.QueryString["continue"], out result))
                return false;

            return result;
        }
    }

    protected string Filter
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Request.QueryString["filter"]))
                return null;

            return Request.QueryString["filter"].ToLower();
        }
    }

    #region Initialization

    /// <summary>
    /// Initializes the target object for the page
    /// </summary>
    /// <remarks>Many pages have "target" objects that the page operates on. For instance, when viewing
    /// an event, the target object is an event. When looking up a directory, that's the target
    /// object. This method is intended to be overriden to initialize the target object for
    /// each page that needs it.</remarks>
    protected override void InitializeTargetObject()
    {
        base.InitializeTargetObject();

        targetChapter = LoadObjectFromAPI<msChapter>(ContextID);
        
        if (targetChapter == null)
        {
            GoToMissingRecordPage();
            return;
        }

        targetMembershipOrganization = LoadObjectFromAPI<msMembershipOrganization>(targetChapter.MembershipOrganization);

        if (targetMembershipOrganization == null)
        {
            GoToMissingRecordPage();
            return;
        }

        targetSearchManifest = MultiStepWizards.ViewChapterMembers.SearchManifest;

        if (targetSearchManifest == null || targetSearchManifest.DefaultSelectedFields.Count == 0)
        {
            targetSearchManifest = buildSearchManifest();
            MultiStepWizards.ViewChapterMembers.SearchManifest = targetSearchManifest;
        }

        using(IConciergeAPIService proxy = GetConciegeAPIProxy())
        {
            List<string> availableFields = defaultFieldFullPaths;

            //If the Membership Organization has defined criteria fields available to leaders use them but prefix with "Membership." because the settings are based on the Membership search
            //while this is a chapter membership search
            if (targetMembershipOrganization.LeaderSearchFields != null)
                availableFields = (from field in targetMembershipOrganization.LeaderSearchFields select "Membership." + field).ToList();

            targetCriteriaFields = proxy.GetSearchFieldMetadataFromFullPath(msChapterMembership.CLASS_NAME, null, availableFields).ResultValue;
        }

        foreach (var f in targetCriteriaFields)
        {
            var labelOverride = PortalConfiguration.GetOverrideFor(
                Request.Url.LocalPath, ColumnHeaderOverridePrefix + f.Name, "Text");

            if (labelOverride != null)
            {
                f.Label = labelOverride.Value;
            }
        }

        foreach (var f in targetSearchManifest.Fields)
        {
            var labelOverride = PortalConfiguration.GetOverrideFor(
                Request.Url.LocalPath, ColumnHeaderOverridePrefix + f.Name, "Text");

            if (labelOverride != null)
            {
                f.Label = labelOverride.Value;
            }
        }

        //Has to be in the InitializeTargetObject to have the leader before running the CheckSecurity
        loadDataFromConcierge();
    }

    /// <summary>
    /// Initializes the page.
    /// </summary>
    /// <remarks>This method runs on the first load of the page, and does NOT
    /// run on postbacks. If you want to run a method on PostBacks, override the
    /// Page_Load event</remarks>
    protected override void InitializePage()
    {
        base.InitializePage();

        populateAvailableOutputFields();
        bindSearchOutputsToPage();

        CustomTitle.Text = string.Format("{0} Members", targetChapter.Name);
    }

    protected override void Page_Load(object sender, EventArgs e)
    {
        base.Page_Load(sender, e);

        //Dynamically render the search criteria fields - this has to happen in the page load not initialize page in order for the Harvest to work properly
        var classMetadata = createClassMetadata(targetCriteriaFields);

        //Change any checkbox fields to a drop down list
        foreach (var fieldMetadata in classMetadata.Fields.Where(fieldMetadata => fieldMetadata.DisplayType == FieldDisplayType.CheckBox))
        {
            fieldMetadata.DisplayType = FieldDisplayType.DropDownList;
            fieldMetadata.PickListEntries.Clear();
            fieldMetadata.PickListEntries.Add(new PickListEntry("True", "true"));
            fieldMetadata.PickListEntries.Add(new PickListEntry("False", "false"));
            fieldMetadata.DefaultValue = null;
        }

        cfsSearchCriteria.Metadata = classMetadata;
        cfsSearchCriteria.PageLayout = createViewMetadata(classMetadata.Fields);
        cfsSearchCriteria.MemberSuiteObject = new MemberSuiteObject();
        cfsSearchCriteria.Render();

        rblOutputFormat.Visible = leader.CanDownloadRoster;
        OutputFormatSection.Visible = leader.CanDownloadRoster;

        if (Continue)
            goToResults();
    }

    protected override bool CheckSecurity()
    {
        if (!base.CheckSecurity()) return false;

        return leader != null && leader.CanViewMembers;
    }

    #endregion

    #region Data Binding


    private void populateAvailableOutputFields()
    {
        if (targetSearchManifest.Fields == null) return;

        foreach (var field in targetSearchManifest.Fields)
        {
            var rliSource = dlbOutputFields.Source.FindItemByValue(field.Name);
            var rliDestination = dlbOutputFields.Destination.FindItemByValue(field.Name);

            if (field.Displayable && rliSource == null && rliDestination == null)
                dlbOutputFields.Source.Items.Add(new RadListBoxItem(_getLabelWithNamespace(field), field.Name));
                    // add it
        }
    
    }

    protected void bindSearchOutputsToPage()
    {
        foreach (var f in targetSearchManifest.Fields)
        {
            if (f.Name == null)
                continue;

            //See if it's already in the destination 
            var rliDestination = dlbOutputFields.Destination.FindItemByValue(f.Name);
            if (rliDestination != null)
                continue;

            // ok, is it in the source column?
            var rli = dlbOutputFields.Source.FindItemByValue(f.Name);

            if (rli == null)  // nope
            {
                rli = new RadListBoxItem(f.Label, f.Name);
                dlbOutputFields.Source.Items.Add(rli);
            }

            // ok, move it over
            dlbOutputFields.Source.Transfer(rli, dlbOutputFields.Source, dlbOutputFields.Destination);

            if (f.Label != null)
                rli.Text = f.Label;
        }
    }

    #endregion

    #region Methods

    protected void loadDataFromConcierge()
    {
        Search sLeaders = GetChapterLeaderSearch(targetChapter.ID);
        SearchResult srLeaders = APIExtensions.GetSearchResult(sLeaders, 0, 1);

        leader = ConvertLeaderSearchResult(srLeaders);
    }

    private SearchManifest buildSearchManifest()
    {
        SearchManifest result;
        List<string> availableFields = defaultFieldFullPaths;

        //If the Membership Organization has defined results fields available to leaders use them but prefix with "Membership." because the settings are based on the Membership search
        //while this is a chapter membership search
        if (targetMembershipOrganization.LeaderSearchResultsFields != null)
            availableFields =
                (from field in targetMembershipOrganization.LeaderSearchResultsFields select "Membership." + field).
                    ToList();

        using (IConciergeAPIService proxy = GetServiceAPIProxy())
        {
            result = proxy.DescribeSearch(msChapterMembership.CLASS_NAME, null).ResultValue;
            result.Fields.Clear();
            result.Fields =
                proxy.GetSearchFieldMetadataFromFullPath(msChapterMembership.CLASS_NAME, null, availableFields).
                    ResultValue;
        }

        //Always add the ID as an output column and sort on name
        result.DefaultSelectedFields = new List<SearchOutputColumn>
                                           {
                                               new SearchOutputColumn
                                                   {Name = "Membership.ID", DisplayName = "Membership ID"}
                                           };
        result.DefaultSortFieds = new List<SearchSortColumn> {new SearchSortColumn {Name = "Membership.Owner.SortName"}, new SearchSortColumn {Name = "Membership.Owner.Name"}};

        result.DefaultSelectedFields.AddRange(
            from field in result.Fields
            select new SearchOutputColumn {Name = field.Name, DisplayName = field.Label});

        return result;
    }

    private static string _getLabelWithNamespace(FieldMetadata f)
    {
        if (String.IsNullOrEmpty(f.Namespace)) return f.Label;
        return string.Format("{0}::{1}", f.Namespace, f.Label);
    }

    protected void goToResults()
    {
        //Set the output fields
        MultiStepWizards.ViewChapterMembers.SearchManifest.DefaultSelectedFields =
            (from field in dlbOutputFields.Destination.Items
             join manifestField in targetSearchManifest.Fields on field.Value equals manifestField.Name
             select new SearchOutputColumn { Name = manifestField.Name, DisplayName = manifestField.Label }
             ).ToList();


        //Create a new search builder to define the criteria
        MultiStepWizards.ViewChapterMembers.SearchBuilder = new SearchBuilder(Search.FromManifest(targetSearchManifest));

        //Add the criteria using the search builder
        cfsSearchCriteria.Harvest();
        MemberSuiteObject mso = cfsSearchCriteria.MemberSuiteObject;

        ParseSearchCriteria(targetCriteriaFields, mso, MultiStepWizards.ViewChapterMembers.SearchBuilder);

        string nextUrl = rblOutputFormat.SelectedValue == "download"
                             ? string.Format("~/chapters/ViewChapterMembers_Results.aspx?contextID={0}&download=true", ContextID)
                             : string.Format("~/chapters/ViewChapterMembers_Results.aspx?contextID={0}",
                                             ContextID);

        if (!string.IsNullOrWhiteSpace(Filter))
            nextUrl += "&filter=" + Filter;

        GoTo(nextUrl);
    }

    protected override void AddCustomOverrideEligibleControls(List<msPortalControlPropertyOverride> eligibleControls)
    {
        base.AddCustomOverrideEligibleControls(eligibleControls);

        // Add in all the fields from the Criteria List
        foreach (var targetCriteriaField in targetCriteriaFields)
        {
            eligibleControls.Add(new msPortalControlPropertyOverride
            {
                PageName = Request.Url.LocalPath,
                ControlName = ColumnHeaderOverridePrefix + targetCriteriaField.Name,
                PropertyName = "Text",
                Value = Convert.ToString(targetCriteriaField.Label)
            });
        }

        // Also add in the Column Names if they are not already included
        foreach (var column in targetSearchManifest.Fields)
        {
            if (eligibleControls.All(o => o.ControlName != ColumnHeaderOverridePrefix + column.Name))
            {
                eligibleControls.Add(new msPortalControlPropertyOverride
                {
                    PageName = Request.Url.LocalPath,
                    ControlName = ColumnHeaderOverridePrefix + column.Name,
                    PropertyName = "Text",
                    Value = Convert.ToString(column.Label)
                });
            }
        }
    }

    #endregion

    #region Event Handlers

    protected void btnSearch_Click(object sender, EventArgs e)
    {
        goToResults();
    }

    #endregion
}