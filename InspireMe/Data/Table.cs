using System.Data;
using InspireMe.Data;
namespace InspireMe.Data
{
    /// <summary>
    /// A base class for all identity tables.
    /// </summary>
    public class Table : TableBase
    {
        /// <summary>
        /// Creates a new instance of <see cref="Table"/>.
        /// </summary>
        /// <param name="dbConnectionFactory"></param>
        public Table(IDatabaseConnectionFactory dbConnectionFactory) {
            DbConnection = dbConnectionFactory.CreateConnection();
        }

        /// <summary>
        /// The type of the database connection class used to access the store.
        /// </summary>
        protected IDbConnection DbConnection { get; }

        /// <inheritdoc/>
        protected override void OnDispose() {
            if (DbConnection != null) {
                DbConnection.Close();
                DbConnection.Dispose();   
            }
        }
    }
}
