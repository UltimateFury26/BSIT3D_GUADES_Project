using System;
using System.Diagnostics;
using System.Web;
using System.Web.Optimization;
using System.Web.Routing;
using Guades_QCUCram.Helpers;

namespace Guades_QCUCram
{
    public class Global : HttpApplication
    {
        void Application_Start(object sender, EventArgs e)
        {
            // Code that runs on application startup
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            // Make sure a default library assistant exists (admin / admin123).
            // Wrapped so a database problem shows up as a normal page error later,
            // instead of stopping the whole site from starting.
            try
            {
                Guades_QCUCram_DbSeeder.EnsureDefaultAdmin();
            }
            catch (Exception ex)
            {
                Trace.WriteLine("EnsureDefaultAdmin failed: " + ex.Message);
            }

            // Background sweep: forfeits, expiries, completions, restriction lifts (every 60 seconds)
            try
            {
                Guades_QCUCram_MaintenanceService.StartBackgroundTimer(60);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("StartBackgroundTimer failed: " + ex.Message);
            }
        }

        void Application_End(object sender, EventArgs e)
        {
            Guades_QCUCram_MaintenanceService.StopBackgroundTimer();
        }
    }
}