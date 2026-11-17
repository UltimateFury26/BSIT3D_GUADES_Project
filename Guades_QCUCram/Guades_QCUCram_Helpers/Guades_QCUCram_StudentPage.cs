namespace Guades_QCUCram.Helpers
{
    // Pages for Student / Faculty / Visitor accounts (all share Role = "Student").
    public class Guades_QCUCram_StudentPage : Guades_QCUCram_SecurePage
    {
        protected override string RequiredRole
        {
            get { return RoleStudent; }
        }
    }
}