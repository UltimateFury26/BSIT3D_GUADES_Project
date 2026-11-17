namespace Guades_QCUCram.Helpers
{
    // Pages for library assistants (Role = "Admin").
    public class Guades_QCUCram_AdminPage : Guades_QCUCram_SecurePage
    {
        protected override string RequiredRole
        {
            get { return RoleAdmin; }
        }
    }
}