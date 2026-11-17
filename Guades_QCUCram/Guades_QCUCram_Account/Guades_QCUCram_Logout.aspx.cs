using System;
using System.Web;
using Guades_QCUCram.Helpers;

namespace Guades_QCUCram.Guades_QCUCram_Account
{
    public partial class Guades_QCUCram_Logout : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Session.Clear();
            Session.Abandon();

            // Remove the session cookie so the old session id can't be reused.
            Response.Cookies.Add(new HttpCookie("ASP.NET_SessionId", "") { Expires = DateTime.Now.AddYears(-1) });

            Response.Redirect(Guades_QCUCram_SecurePage.LoginUrl + "?msg=loggedout");
        }
    }
}