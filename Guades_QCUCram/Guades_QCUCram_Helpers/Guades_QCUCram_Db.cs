using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace Guades_QCUCram.Helpers
{
    public static class Guades_QCUCram_Db
    {
        public static string ConnectionString
        {
            get
            {
                return ConfigurationManager.ConnectionStrings["Guades_QCUCram_Connection"].ConnectionString;
            }
        }

        public static SqlConnection GetConnection()
        {
            return new SqlConnection(ConnectionString);
        }

        // Shortcut for building a parameter (null becomes DBNull)
        public static SqlParameter P(string name, object value)
        {
            return new SqlParameter(name, value ?? DBNull.Value);
        }

        // SELECT -> DataTable
        public static DataTable Query(string sql, params SqlParameter[] parameters)
        {
            using (var con = GetConnection())
            using (var cmd = new SqlCommand(sql, con))
            using (var da = new SqlDataAdapter(cmd))
            {
                if (parameters != null) cmd.Parameters.AddRange(parameters);
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        // INSERT / UPDATE / DELETE -> rows affected
        public static int Execute(string sql, params SqlParameter[] parameters)
        {
            using (var con = GetConnection())
            using (var cmd = new SqlCommand(sql, con))
            {
                if (parameters != null) cmd.Parameters.AddRange(parameters);
                con.Open();
                return cmd.ExecuteNonQuery();
            }
        }

        // Single value (first column of first row), null if no rows
        public static object Scalar(string sql, params SqlParameter[] parameters)
        {
            using (var con = GetConnection())
            using (var cmd = new SqlCommand(sql, con))
            {
                if (parameters != null) cmd.Parameters.AddRange(parameters);
                con.Open();
                return cmd.ExecuteScalar();
            }
        }
    }
}