using System.Data;
using System.Data.SqlClient;

namespace DealnetPortal.Utilities.DataAccess
{
    public class MsSqlDatabaseService : IDatabaseService
    {
        private readonly string _connectionString;

        public MsSqlDatabaseService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IDataReader ExecuteReader(string query)
        {
            var connection = GetConnection();
            connection.Open();
            var command = new SqlCommand(query, connection);
            return command.ExecuteReader();
        }

        private SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}
