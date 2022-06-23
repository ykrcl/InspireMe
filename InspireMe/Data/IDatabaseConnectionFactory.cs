using System.Data;
using System.Threading.Tasks;

namespace InspireMe.Data
{
    /// <summary>
    /// Responsible for returning database connection
    /// </summary>
    public interface IDatabaseConnectionFactory
    {
        /// <summary>
        /// Returns Database Connection
        /// </summary>
        /// <returns></returns>
        IDbConnection CreateConnection();
        Task<IDbConnection> CreateConnectionAsync();
    }
}
