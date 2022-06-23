using System.Data;
using InspireMe.Data;
namespace InspireMe.Identitiy
{
    /// <summary>
    /// A base class for all identity tables.
    /// </summary>
    public class IdentityTable : TableBase
    {
        /// <summary>
        /// Creates a new instance of <see cref="IdentityTable"/>.
        /// </summary>
        /// <param name="dbConnectionFactory"></param>
        public IdentityTable(IDatabaseConnectionFactory dbConnectionFactory) {
            DbConnection = dbConnectionFactory.CreateConnection();
        }

        /// <summary>
        /// The type of the database connection class used to access the store.
        /// </summary>
        protected IDbConnection DbConnection { get; }

        /// <inheritdoc/>
        protected override void OnDispose() {
            if (DbConnection != null) {
                DbConnection.Dispose();
            }
        }
    }
}
