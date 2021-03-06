﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using MemberSuite.SDK.Concierge;
using MemberSuite.SDK.Results;
using MemberSuite.SDK.Searching;
using MemberSuite.SDK.Searching.Operations;
using MemberSuite.SDK.Types;

public partial class sections_ManageSectionCommittees : PortalPage
{
    #region Fields

    protected msSection targetSection;
    protected DataView dvCommittees;

    #endregion

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

        targetSection = LoadObjectFromAPI<msSection>(ContextID);
        if (targetSection == null)
        {
            GoToMissingRecordPage();
            return;
        }
    }

    protected override bool CheckSecurity()
    {
        if (!base.CheckSecurity()) return false;

        if (targetSection == null)
            return false;

        if (targetSection.Leaders == null) return false; // no leaders to speak of

        var sectionLeader = targetSection.Leaders.Find(x => x.Individual == CurrentEntity.ID);

        // is it null? It means the person isn't a leader
        return sectionLeader != null && sectionLeader.CanManageCommittees;
    }

    #endregion

    #region Methods

    protected void loadDataFromConcierge()
    {
        Search s = new Search { Type = msCommittee.CLASS_NAME };

        s.AddOutputColumn("ID");
        s.AddOutputColumn("Name");
        s.AddOutputColumn("CurrentMemberCount");
        s.AddCriteria(Expr.Equals("Section.ID", targetSection.ID));
        s.AddCriteria(Expr.Equals("ShowInPortal", true));
        s.AddSortColumn("Name");
        var searchResult = APIExtensions.GetSearchResult(s, 0, null);

        dvCommittees = new DataView(searchResult.Table);
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handles the Load event of the Page control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
    protected override void Page_Load(object sender, EventArgs e)
    {
        base.Page_Load(sender, e);

        loadDataFromConcierge();

        gvCommittees.DataSource = dvCommittees;
        gvCommittees.DataBind();
    }

    protected void gvCommittees_RowCommand(Object sender, GridViewCommandEventArgs e)
    {
        if (e.CommandName.ToLower() == "deletecommittee")
        {
            using(IConciergeAPIService serviceProxy = GetServiceAPIProxy())
            {
                serviceProxy.Delete(e.CommandArgument.ToString());
            }

            QueueBannerMessage("Committee deleted successfully.");
        }
    }

    #endregion
}