using System;

namespace Guades_QCUCram.Helpers
{
    public static class Guades_QCUCram_DbSeeder
    {
        // Creates the default library assistant ONLY when AdminUsers is empty.
        // Safe to call on every application start.
        public static void EnsureDefaultAdmin()
        {
            // One statement, so two app instances starting together can't both insert.
            Guades_QCUCram_Db.Execute(
                "IF NOT EXISTS (SELECT 1 FROM AdminUsers) " +
                "INSERT INTO AdminUsers (Username, PasswordHash, FullName, IsActive) " +
                "VALUES (@u, @p, @n, 1)",
                Guades_QCUCram_Db.P("@u", "admin"),
                Guades_QCUCram_Db.P("@p", Guades_QCUCram_PasswordHelper.Hash("admin123")),
                Guades_QCUCram_Db.P("@n", "Library Assistant"));
        }
    }
}