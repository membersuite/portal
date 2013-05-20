﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;
using MemberSuite.SDK.Concierge;
using MemberSuite.SDK.Results;
using MemberSuite.SDK.Searching;
using MemberSuite.SDK.Searching.Operations;
using MemberSuite.SDK.Types;

/// <summary>
/// Summary description for DiscussionsPage
/// </summary>
public class DiscussionsPage : PortalPage
{
    #region Fields
    private msDiscussionBoard targetDiscussionBoard;

    protected msForum targetForum;
    protected msDiscussionTopic targetDiscussionTopic;
    protected msDiscussionPost targetDiscussionPost;
    
    protected msChapter targetChapter;
    protected msSection targetSection;
    protected msCommittee targetCommittee;
    protected msOrganizationalLayer targetOrganizationalLayer;
    protected msEvent targetEvent;

    protected DataRow drMembership;
    protected bool editMode;

    protected msMembershipLeader leader;
    protected DataRow drLastPostPendingApproval;
    protected int numberOfPostsPendingApproval;
    protected bool hasLeaderSearchBeenRun;
    protected DataRow drSubscription;

    #endregion

    #region Properties

    protected msDiscussionBoard TargetDiscussionBoard
    {
        get { return targetDiscussionBoard; }
        set
        {
            if(value != null)
            {
                switch(value.ClassType)
                {
                    case msChapterDiscussionBoard.CLASS_NAME:
                        msChapterDiscussionBoard cdb = value.ConvertTo<msChapterDiscussionBoard>();
                        targetChapter = LoadObjectFromAPI<msChapter>(cdb.Chapter);
                        break;
                    case msSectionDiscussionBoard.CLASS_NAME:
                        msSectionDiscussionBoard sdb = value.ConvertTo<msSectionDiscussionBoard>();
                        targetSection = LoadObjectFromAPI<msSection>(sdb.Section);
                        break;
                    case msCommitteeDiscussionBoard.CLASS_NAME:
                        msCommitteeDiscussionBoard committeedb = value.ConvertTo<msCommitteeDiscussionBoard>();
                        targetCommittee = LoadObjectFromAPI<msCommittee>(committeedb.Committee);
                        break;
                    case msOrganizationalLayerDiscussionBoard.CLASS_NAME:
                        msOrganizationalLayerDiscussionBoard oldb = value.ConvertTo<msOrganizationalLayerDiscussionBoard>();
                        targetOrganizationalLayer = LoadObjectFromAPI<msOrganizationalLayer>(oldb.OrganizationalLayer);
                        break;
                    case msEventDiscussionBoard.CLASS_NAME:
                        msEventDiscussionBoard edb = value.ConvertTo<msEventDiscussionBoard>();
                        targetEvent = LoadObjectFromAPI<msEvent>(edb.Event);
                        break;
                }

            }

            targetDiscussionBoard = value;
        }
    }

    #endregion

    #region Initialization

    protected override bool CheckSecurity()
    {
        if(!base.CheckSecurity())
            return false;

        if(targetForum != null && (!targetForum.IsActive || (targetForum.MembersOnly && !isActiveMember())))
            return false;

        if (editMode && targetDiscussionPost != null && targetDiscussionTopic.PostedBy != ConciergeAPI.CurrentEntity.ID)
            return false;

        return true;
    }

    #endregion

    #region Methods

    protected virtual void loadDataFromConcierge(IConciergeAPIService proxy)
    {
        List<Search> searches = new List<Search>();

        Search sMembership = new Search(msEntity.CLASS_NAME){ID=msMembership.CLASS_NAME};
        sMembership.AddOutputColumn("ID");
        sMembership.AddOutputColumn("Membership");
        sMembership.AddOutputColumn("Membership.ReceivesMemberBenefits");
        sMembership.AddOutputColumn("Membership.TerminationDate");
        sMembership.AddCriteria(Expr.Equals("ID", ConciergeAPI.CurrentEntity.ID));
        sMembership.AddSortColumn("ID");

        searches.Add(sMembership);

        List<SearchResult> searchResults = ExecuteSearches(proxy, searches, 0, 1);

        SearchResult srMembership = searchResults.Single(x => x.ID == msMembership.CLASS_NAME);
        drMembership = srMembership != null && srMembership.Table != null &&
                       srMembership.Table.Rows.Count > 0
                           ? srMembership.Table.Rows[0]
                           : null;
    }

    protected bool isActiveMember()
    {
        if (drMembership == null)
            return false;

        //Check if the appropriate fields exist - if they do not then the membership module is inactive
        if (
            !(drMembership.Table.Columns.Contains("Membership") &&
              drMembership.Table.Columns.Contains("Membership.ReceivesMemberBenefits") &&
              drMembership.Table.Columns.Contains("Membership.TerminationDate")))
            return false;

        //Check there is a membership
        if (string.IsNullOrWhiteSpace(Convert.ToString(drMembership["Membership"])))
            return false;

        //Check the membership indicates membership benefits
        if (!drMembership.Field<bool>("Membership.ReceivesMemberBenefits"))
            return false;

        //At this point if the termination date is null the member should be able to see the restricted directory
        DateTime? terminationDate = drMembership.Field<DateTime?>("Membership.TerminationDate");

        if (terminationDate == null)
            return true;

        //There is a termination date so check if it's future dated
        return terminationDate > DateTime.Now;
    }

    protected string FormatDate(object value)
    {
        if (value == null || !(value is DateTime))
            return null;

        DateTime dt = (DateTime)value;

        if (dt.Date == DateTime.Today)
            return "Today " + dt.ToShortTimeString();

        if (dt.Date == DateTime.Today.AddDays(-1))
            return "Yesterday " + dt.ToShortTimeString();

        return dt.ToShortDateString() + " " + dt.ToShortTimeString();
    }

    protected bool isModerator()
    {
        using(IConciergeAPIService proxy = GetConciegeAPIProxy())
        {
            return isModerator(proxy);
        }
    }

    protected bool isModerator(IConciergeAPIService proxy)
    {
        if (hasLeaderSearchBeenRun)
            return leader != null && leader.CanModerateDiscussions;

        if(targetChapter != null)
        {
            Search sChapterLeader = GetChapterLeaderSearch(targetChapter.ID);
            SearchResult srChapterLeader = ExecuteSearch(proxy, sChapterLeader, 0, 1);
            leader = ConvertLeaderSearchResult(srChapterLeader);
            hasLeaderSearchBeenRun = true;
        }

        if (targetSection != null)
        {
            Search sSectionLeader = GetSectionLeaderSearch(targetSection.ID);
            SearchResult srSectionLeader = ExecuteSearch(proxy, sSectionLeader, 0, 1);
            leader = ConvertLeaderSearchResult(srSectionLeader);
            hasLeaderSearchBeenRun = true;
        }

        if (targetOrganizationalLayer != null)
        {
            Search sOrganizationalLayerLeader = GetOrganizationalLayerLeaderSearch(targetOrganizationalLayer.ID);
            SearchResult srOrganizationalLayerLeader = ExecuteSearch(proxy, sOrganizationalLayerLeader, 0, 1);
            leader = ConvertLeaderSearchResult(srOrganizationalLayerLeader);
            hasLeaderSearchBeenRun = true;
        }

        return leader != null && leader.CanModerateDiscussions;
    }

    #endregion

    #region Event Handlers

    #endregion

    protected void loadSubscription()
    {
        using(IConciergeAPIService proxy = GetConciegeAPIProxy())
        {
            loadSubscription(proxy);
        }
    }

    protected void loadSubscription(IConciergeAPIService proxy)
    {
        Search sSubscription = new Search(msDiscussionTopicSubscription.CLASS_NAME);
        sSubscription.AddOutputColumn("ID");
        sSubscription.AddCriteria(Expr.Equals("Topic", targetDiscussionTopic.ID));
        sSubscription.AddCriteria(Expr.Equals("Subscriber", ConciergeAPI.CurrentEntity.ID));
        sSubscription.AddSortColumn("Subscriber");

        SearchResult srSubscription = ExecuteSearch(proxy, sSubscription, 0, 1);
        if (srSubscription.TotalRowCount > 0 && srSubscription.Table != null && srSubscription.Table.Rows.Count > 0)
            drSubscription = srSubscription.Table.Rows[0];
    }
}