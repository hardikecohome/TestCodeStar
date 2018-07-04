using System.Collections.Generic;

namespace DealnetPortal.Utilities.DataAccess
{
    /// <summary>
    /// Get sql query by name
    /// </summary>
    public interface IQueriesStorage
    {
        Dictionary<string, string> Queries { get; }
        string GetQuery(string queryName);
    }
}
