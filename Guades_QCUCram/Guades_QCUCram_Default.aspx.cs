using System;
using System.Web.UI;
using Guades_QCUCram.Helpers;

namespace Guades_QCUCram
{
    public partial class _Default : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string role = Session[Guades_QCUCram_SecurePage.SessionRole] as string;
            object idValue = Session[Guades_QCUCram_SecurePage.SessionUserId];
            bool loggedIn = (idValue is int) && (int)idValue > 0;

            if (loggedIn && role == Guades_QCUCram_SecurePage.RoleStudent)
                Response.Redirect("~/Guades_QCUCram_Student/Guades_QCUCram_Dashboard.aspx");
            else if (loggedIn && role == Guades_QCUCram_SecurePage.RoleAdmin)
                Response.Redirect("~/Guades_QCUCram_Admin/Guades_QCUCram_Dashboard.aspx");
            else
                Response.Redirect(Guades_QCUCram_SecurePage.LoginUrl);
        }
    }
}