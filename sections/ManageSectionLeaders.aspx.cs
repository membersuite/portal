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

public partial class sections_ManageSectionLeaders : PortalPage
{
    #region Fields

    protected msSection targetSection;
    protected DataView dvLeaders;
    
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

        if(targetSection == null)
        {
            GoToMissingRecordPage();
            return;
        }
    }

    #endregion

    #region Methods

    protected void loadDataFromConcierge()
    {
        Search sLeaders = new Search("SectionLeader");
        sLeaders.AddOutputColumn("Individual.ID");
        sLeaders.AddOutputColumn("Individual.Name");
        sLeaders.AddCriteria(Expr.Equals("Section.ID", targetSection.ID));
        sLeaders.AddSortColumn("Individual.Name");

        SearchResult srLeaders = ExecuteSearch(sLeaders, 0, null);
        dvLeaders = new DataView(srLeaders.Table);
    }

    private void loadAndBindLeaders()
    {
        loadDataFromConcierge();

        //Bind the leaders gridview
        gvLeaders.DataSource = dvLeaders;
        gvLeaders.DataBind();
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

        if(!IsPostBack)
            loadAndBindLeaders();
    }

    protected void gvLeaders_RowCommand(object sender, GridViewCommandEventArgs e)
    {
        switch(e.CommandName.ToLower())
        {
            case "deleteleader":
                targetSection.Leaders.RemoveAll(
                    x => x.Individual == e.CommandArgument.ToString());
                using(IConciergeAPIService serviceProxy = GetServiceAPIProxy())
                {
                    targetSection = serviceProxy.Save(targetSection).ResultValue.ConvertTo<msSection>();
                }
                loadAndBindLeaders();
                break;
            case "editleader":
                string nextUrl = string.Format("~/sections/EditSectionLeader.aspx?contextID={0}&individualID={1}", ContextID,
                                               e.CommandArgument);
                GoTo(nextUrl);
                break;
        }
        
    }

    #endregion
}