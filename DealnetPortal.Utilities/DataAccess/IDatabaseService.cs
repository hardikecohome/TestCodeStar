using System.Data;

namespace DealnetPortal.Utilities.DataAccess
{
    public interface IDatabaseService
    {
        IDataReader ExecuteReader(string query);
    }
}
