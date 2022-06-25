using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Dapper;
using Microsoft.AspNetCore.Identity;
namespace InspireMe.Data
{
    public class Booking
    {
        public Guid Id
        {
            get;
            set;
        }

        [Required(ErrorMessage = "Tarih Boş Olamaz!")]
        public DateOnly Date
        {
            get;
            set;
        }

        public IdentityUser? Customer
        {
            get;
            set;
        }
        public string? CustomerRTCId
        {
            get;
            set;
        }
        public string? SupervisorRTCId
        {
            get;
            set;
        }

        public IdentityUser? Supervisor
        {
            get;
            set;
        }

        [Required(ErrorMessage = "Saat Boş Olamaz!")]
        [Range(0, 24)]
        public int Hour
        {
            get;
            set;
        }

        public bool IsEnded
        {
            get;
            set;
        } = false;
        public bool IsStarted
        {
            get;
            set;
        } = false;
        public bool IsVerified
        {
            get;
            set;
        } = false;
        public string? ChatHistory
        {
            get;
            set;
        }
    }

    public class BookingsTable :
            Table
    {
        public BookingsTable(IDatabaseConnectionFactory dbConnectionFactory) : base(dbConnectionFactory) { }

        /// <inheritdoc/>
        public virtual async Task<bool> CreateAsync(Booking obj, string CustomerId, string SupervisorId)
        {
            const string sql = "INSERT INTO Bookings " +
                               "VALUES (@Id, @Date,  @Hour,  @IsEnded, @IsStarted, @CustomerId, @SupervisorId, @IsVerified);";
            var rowsInserted = await DbConnection.ExecuteAsync(sql, new
            {
                Id = Guid.NewGuid(),
                obj.Date,
                obj.Hour,
                obj.IsEnded,
                obj.IsStarted,
                CustomerId,
                SupervisorId,
                obj.IsVerified
                

            });
            return rowsInserted == 1;
        }

        /// <inheritdoc/>
        public virtual async Task<bool> DeleteAsync(Guid objId)
        {
            const string sql = "DELETE " +
                               "FROM Bookings " +
                               "WHERE Id = @Id;";
            var rowsDeleted = await DbConnection.ExecuteAsync(sql, new { Id = objId });
            return rowsDeleted == 1;
        }

        /// <inheritdoc/>
        public virtual async Task<IEnumerable<Booking>> GetSupervisorBookingsAsync(string SupervisorId)
        {
            const string sql = "SELECT * " +
                               "FROM Bookings b " +
                               "inner join AspNetUsers customers on customers.Id = b.CustomerId " +
                               "WHERE SupervisorId = @SupervisorId;";
            var bookings = await DbConnection.QueryAsync<Booking, IdentityUser, Booking>(sql, (booking, user) => { booking.Customer = user; return booking; }, new { SupervisorId = SupervisorId });
            return bookings;
        }

        public virtual async Task<IEnumerable<Booking>> GetCustomerBookingsAsync(string CustomerId)
        {
            const string sql = "SELECT * " +
                               "FROM Bookings b " +
                               "inner join AspNetUsers supervisors on supervisors.Id = b.SupervisorId " +
                               "WHERE CustomerId = @CustomerId;";
            var bookings = await DbConnection.QueryAsync<Booking, IdentityUser, Booking>(sql, (booking, user) => { booking.Supervisor = user; return booking; }, new { CustomerId = CustomerId });
            return bookings;
        }

        public virtual async Task<bool> CheckAvailabilityExistsAsync(string SupervisorId, DateOnly Date, int Hour)
        {
            const string sql = "SELECT count(1) " +
                               "FROM Bookings " +
                               "WHERE SupervisorId = @SupervisorId AND Date=@Date AND Hour=@Hour;";
            var role = !(await DbConnection.QuerySingleOrDefaultAsync<bool>(sql, new { SupervisorId = SupervisorId, Date = Date, Hour = Hour }));
            return role;
        }

        public virtual async Task<IEnumerable<Booking>> GetOccupiedHoursAsync(string SupervisorId)
        {
            const string sql = "SELECT *" +
                               "FROM Bookings " +
                               "WHERE SupervisorId = @SupervisorId AND( (Date>current_date - 1)  OR (Date=current_date AND Hour>=@Hour));";
            var role = await DbConnection.QueryAsync<Booking>(sql, new { SupervisorId = SupervisorId, Hour = DateTime.Now.Hour });
            return role;
        }


        public virtual async Task<bool> VerifyBooking(Guid objId)
        {
            const string updateRoleSql = "UPDATE Bookings " +
                                         "SET IsVerified= TRUE " +
                                         "WHERE Id = @Id;";
            using (var transaction = DbConnection.BeginTransaction())
            {
                await DbConnection.ExecuteAsync(updateRoleSql, new
                {
                   Id = objId,

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


        /// <inheritdoc/>
        public virtual async Task<bool> UpdateAsync(Booking obj, string CustomerId, string SupervisorId )
        {
            const string updateRoleSql = "UPDATE Bookings " +
                                         "SET Date = @Date, Hour = @Hour, IsEnded = @IsEnded, IsStarted =@IsStarted, CustomerId=@CustomerId, SupervisorId=@SupervisorId,IsVerified=@IsVerified " +
                                         "WHERE Id = @Id;";
            using (var transaction = DbConnection.BeginTransaction())
            {
                await DbConnection.ExecuteAsync(updateRoleSql, new
                {
                    obj.Date,
                    obj.Hour,
                    obj.IsEnded,
                    obj.IsStarted,
                    CustomerId,
                    SupervisorId,
                    obj.IsVerified,
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
