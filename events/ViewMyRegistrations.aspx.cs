﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using MemberSuite.SDK.Searching;
using MemberSuite.SDK.Searching.Operations;
using MemberSuite.SDK.Types;

public partial class events_ViewMyRegistrations : PortalPage 
{

    protected override void InitializePage()
    {
        base.InitializePage();

        Search s = new Search(msEventRegistration.CLASS_NAME);
        s.AddCriteria(Expr.Equals("Owner", CurrentEntity.ID));
        s.AddSortColumn("CreatedDate", true);

        s.AddOutputColumn("Event.Name");
        s.AddOutputColumn("Event.StartDate");
        s.AddOutputColumn("CreatedDate");

        var results = ExecuteSearch(s, 0, null);

        gvEvents.DataSource = results.Table;
        gvEvents.DataBind();
    }
}