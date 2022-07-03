using System.ComponentModel.DataAnnotations;
using Dapper;
using InspireMe.Identity;

namespace InspireMe.Data
{

    public class AvailableDate
    {
        public long Id
        {
            get;
            set;
        }

        [Required]
        [Range(0, 6)]
        public int Day
        {
            get;
            set;
        }

        [Required]
        public string UserId
        {
            get;
            set;
        }
        [Required]
        [Range(0, 24)]
        public int Hour
        {
            get;
            set;
        }
        public float Price
        {
            get;
            set;
        }
        public bool IsAvailable
        {
            get;
            set;
        }
        public string? Reason
        {
            get;
            set;
        }
    }

    public class AvailableDatesTable :
            Table
    {
        
        public AvailableDatesTable(IDatabaseConnectionFactory dbConnectionFactory) : base(dbConnectionFactory) { }

        /// <inheritdoc/>
        public virtual async Task<bool> CreateAsync(AvailableDate obj)
        {
            const string sql = "INSERT INTO AvailableDates  (hour, price, isavailable, userid, reason, day)" +
                               "VALUES (@Hour,  @Price, @IsAvailable, @UserId,@Reason, @Day);";
            var rowsInserted = await DbConnection.ExecuteAsync(sql, new
            {
                obj.Day,
                obj.Hour,
                obj.IsAvailable,
                obj.Price,
                obj.UserId,
                obj.Reason,

            });
            return rowsInserted == 1;
        }

        /// <inheritdoc/>
        public virtual async Task<bool> DeleteAsync(long objId)
        {
            const string sql = "DELETE " +
                               "FROM AvailableDates " +
                               "WHERE Id = @Id;";
            var rowsDeleted = await DbConnection.ExecuteAsync(sql, new { Id = objId });
            return rowsDeleted == 1;
        }

        /// <inheritdoc/>
        public virtual async Task<AvailableDate> FindByIdAsync(long objId)
        {
            const string sql = "SELECT * " +
                               "FROM AvailableDates " +
                               "WHERE Id = @Id;";
            var role = await DbConnection.QuerySingleOrDefaultAsync<AvailableDate>(sql, new { Id = objId });
            return role;
        }

        public virtual async Task<bool> CheckAvailabilityExistsAsync(string UserId, int Day, int Hour)
        {
            const string sql = "SELECT count(1) " +
                               "FROM AvailableDates " +
                               "WHERE UserId = @UserId AND Day=@Day AND Hour=@Hour AND IsAvailable = TRUE ;";
            var role = await DbConnection.QuerySingleOrDefaultAsync<bool>(sql, new { UserId = UserId, Day= Day, Hour=Hour });
            return role;
        }


        public virtual async Task<IEnumerable<AvailableDate>> GetUserAvailableDatesAsync(string UserId)
        {
            const string sql = "SELECT * " +
                               "FROM AvailableDates " +
                               "WHERE UserId = @UserId AND IsAvailable = TRUE ;";
            var role = await DbConnection.QueryAsync<AvailableDate>(sql, new { UserId = UserId });
            return role;
        }

        /// <inheritdoc/>
        public virtual async Task<bool> UpdateAsync(AvailableDate obj)
        {
            const string updateRoleSql = "UPDATE AvailableDates " +
                                         "SET Day = @Day, Hour = @Hour, Price = @Price, UserId =@UserId, IsAvailable=@IsAvailable, Reason=@Reason" +
                                         "WHERE Id = @Id;";
            using (var transaction = DbConnection.BeginTransaction())
            {
                await DbConnection.ExecuteAsync(updateRoleSql, new
                {
                    obj.Day,
                    obj.Hour,
                    obj.Price,
                    obj.UserId,
                    obj.IsAvailable,
                    obj.Reason,
                    obj.Id
                   
                }, transaction);
                
                try
                {
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    return false;
                }
            }
            return true;
        }
    }
}
