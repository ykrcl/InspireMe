using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Dapper;
using Microsoft.AspNetCore.Identity;
using System.Data;
namespace InspireMe.Data
{
    public class DapperSqlDateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
    {
        public override void SetValue(IDbDataParameter parameter, DateOnly date)
            => parameter.Value = date.ToDateTime(new TimeOnly(0, 0));

        public override DateOnly Parse(object value)
            => DateOnly.FromDateTime((DateTime)value);
    }
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
        [Range(0, 23)]
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
            const string sql = "INSERT INTO Bookings (id, date, hour, isended, isstarted, customerid, supervisorid, isverified)" +
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
        public virtual async Task<bool> DeleteAsync(Guid objId, string SupervisorId)
        {
            const string sql = "DELETE " +
                               "FROM Bookings " +
                               "WHERE Id = @Id AND SupervisorId = @SupervisorId AND (Date>current_date - 1);";
            var rowsDeleted = await DbConnection.ExecuteAsync(sql, new { Id = objId, SupervisorId= SupervisorId });
            return rowsDeleted == 1;
        }

        /// <inheritdoc/>
        public virtual async Task<IEnumerable<Booking>> GetSupervisorBookingsAsync(string SupervisorId)
        {
            const string sql = "SELECT * " +
                               "FROM Bookings b " +
                               "inner join AspNetUsers customers on customers.Id = b.CustomerId " +
                               "WHERE SupervisorId = @SupervisorId AND (Date>current_date - 1);";
            var bookings = await DbConnection.QueryAsync<Booking, IdentityUser, Booking>(sql, (booking, user) => { booking.Customer = user; return booking; }, new { SupervisorId = SupervisorId });
            return bookings;
        }

        public virtual async Task<IEnumerable<Booking>> GetUpcomingMeetings()
        {
            const string sql = "SELECT * " +
                               "FROM Bookings b " +
                               "inner join AspNetUsers customers on customers.Id = b.CustomerId " +
                               "inner join AspNetUsers supervisors on supervisors.Id = b.SupervisorId " +
                               "WHERE (Date = current_date AND Hour =@Hour) OR (Date = current_date + 1 AND Hour = 0);";
            var bookings = await DbConnection.QueryAsync<Booking, IdentityUser, IdentityUser, Booking>(sql, (booking, user, user2) => { booking.Customer = user; booking.Supervisor = user2; return booking; }, new { Hour = DateTime.Now.Hour + 1 });
            return bookings;
        }

        public virtual async Task<IEnumerable<Booking>> GetCustomerBookingsAsync(string CustomerId)
        {
            const string sql = "SELECT * " +
                               "FROM Bookings b " +
                               "inner join AspNetUsers supervisors on supervisors.Id = b.SupervisorId " +
                               "WHERE CustomerId = @CustomerId AND (Date>current_date - 3) AND (IsEnded=FALSE OR (Date=current_date));";
            var bookings = await DbConnection.QueryAsync<Booking, IdentityUser, Booking>(sql, (booking, user) => { booking.Supervisor = user; return booking; }, new { CustomerId = CustomerId });
            return bookings;
        }

        public virtual async Task<Booking> FindBookingByIdAsync(Guid Id)
        {
            const string sql = "SELECT * " +
                               "FROM Bookings " +
                               "WHERE Id = @Id;";
            var bookings = await DbConnection.QueryFirstOrDefaultAsync<Booking>(sql, new { Id = Id });
            return bookings;
        }
        public virtual async Task<Booking> FindBookingByIdBindCustomerAsync(Guid Id)
        {
            const string sql = "SELECT * " +
                               "FROM Bookings b " +
                               "inner join AspNetUsers customers on customers.Id = b.CustomerId " +
                               "WHERE b.Id = @Id;";
            var bookings = (await DbConnection.QueryAsync<Booking, IdentityUser, Booking>(sql, (booking, user) => { booking.Customer = user; return booking; }, new { Id = Id })).FirstOrDefault();
            return bookings;
        }
        public virtual async Task<Booking> FindBookingByIdBindSupervisorAsync(Guid Id)
        {
            const string sql = "SELECT * " +
                               "FROM Bookings b " +
                               "inner join AspNetUsers supervisors on supervisors.Id = b.SupervisorId " +
                               "WHERE b.Id = @Id;";
            var bookings = (await DbConnection.QueryAsync<Booking, IdentityUser, Booking>(sql, (booking, user) => { booking.Supervisor = user; return booking; }, new { Id = Id })).FirstOrDefault();
            return bookings;
        }
        public virtual async Task<Booking> FindBookingByConnectionId(String Id)
        {
            const string sql = "SELECT * " +
                               "FROM Bookings " +
                               "WHERE CustomerRTCId = @Id OR SupervisorRTCId= @Id;";
            var bookings = await DbConnection.QueryFirstOrDefaultAsync<Booking>(sql, new { Id = Id });
            return bookings;
        }


        public virtual async Task<bool> CheckAvailabilityExistsAsync(string SupervisorId, string CustomerId, DateOnly Date, int Hour)
        {
            const string sql = "SELECT count(1) " +
                               "FROM Bookings " +
                               "WHERE (SupervisorId = @SupervisorId OR CustomerId = @CustomerId) AND Date=@Date AND Hour=@Hour;";
            var role = !(await DbConnection.QuerySingleOrDefaultAsync<bool>(sql, new { SupervisorId = SupervisorId, CustomerId= CustomerId, Date = Date, Hour = Hour }));
            return role;
        }
        public virtual async Task<bool> CheckCustomerHasMeetingAsync(string CustomerId)
        {
            const string sql = "SELECT count(1) " +
                               "FROM Bookings " +
                               "WHERE (CustomerId = @CustomerId) AND ((Date=current_date AND Hour=@Hour) OR (IsStarted=TRUE AND IsEnded=FALSE AND IsVerified=TRUE)) ;";
            var role = (await DbConnection.QuerySingleOrDefaultAsync<bool>(sql, new { Hour = DateTime.Now.Hour, CustomerId= CustomerId }));
            return role;
        }

        public virtual async Task<bool> CheckSupervisorHasMeetingAsync(string SupervisorId)
        {
            const string sql = "SELECT count(1) " +
                               "FROM Bookings " +
                               "WHERE (SupervisorId = @SupervisorId) AND ((Date=current_date AND Hour=@Hour) OR (IsEnded=FALSE AND IsVerified=TRUE)) ;";
            var role = (await DbConnection.QuerySingleOrDefaultAsync<bool>(sql, new { Hour = DateTime.Now.Hour, SupervisorId= SupervisorId }));
            return role;
        }


        public virtual async Task<IEnumerable<Booking>> GetOccupiedHoursAsync(string SupervisorId, string CustomerId)
        {
            const string sql = "SELECT * " +
                               "FROM Bookings " +
                               "WHERE (SupervisorId = @SupervisorId OR CustomerId=@CustomerId) AND( (Date>current_date - 1)  OR (Date=current_date AND Hour>=@Hour));";
            var role = await DbConnection.QueryAsync<Booking>(sql, new { SupervisorId = SupervisorId, Hour = DateTime.Now.Hour, CustomerId= CustomerId });
            return role;
        }


        public virtual async Task<bool> VerifyBooking(Guid objId, string SupervisorId)
        {
            const string updateRoleSql = "UPDATE Bookings " +
                                         "SET IsVerified= TRUE " +
                                         "WHERE Id = @Id AND SupervisorId=@SupervisorId;";
            using (var transaction = DbConnection.BeginTransaction())
            {
                await DbConnection.ExecuteAsync(updateRoleSql, new
                {
                   Id = objId,
                    SupervisorId= SupervisorId

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

        public virtual async Task<bool> StartMeetingAsync(Guid Id)
        {
            const string updateRoleSql = "UPDATE Bookings " +
                                         "SET IsStarted = TRUE " +
                                         "WHERE Id = @Id;";
            using (var transaction = DbConnection.BeginTransaction())
            {
                await DbConnection.ExecuteAsync(updateRoleSql, new
                {
                   Id= Id

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

        public virtual async Task<bool> EndOlderMeetings()
        {
            const string updateRoleSql = "UPDATE Bookings " +
                                         "SET IsEnded = TRUE " +
                                         "WHERE IsStarted = TRUE AND SupervisorRTCId = NULL AND CustomerRTCId = NULL AND ( (Date>current_date)  OR (Date=current_date AND Hour<=@Hour));";
            using (var transaction = DbConnection.BeginTransaction())
            {
                await DbConnection.ExecuteAsync(updateRoleSql, new
                {
                    Hour = DateTime.Now.Hour

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
        public virtual async Task<bool> EndMeetingAsync(Guid Id)
        {
            const string updateRoleSql = "UPDATE Bookings " +
                                         "SET IsEnded = TRUE " +
                                         "WHERE Id = @Id;";
            using (var transaction = DbConnection.BeginTransaction())
            {
                await DbConnection.ExecuteAsync(updateRoleSql, new
                {
                    Id = Id

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
        public virtual async Task<bool> ConnectCustomertoMeetingAsync(string CustomerRTCId, Guid Id)
        {
            const string updateRoleSql = "UPDATE Bookings " +
                                         "SET CustomerRTCId = NULL " +
                                         "WHERE CustomerRTCId = @CustomerRTCId; " +
                                         "UPDATE Bookings " +
                                         "SET SupervisorRTCId = NULL " +
                                         "WHERE SupervisorRTCId = @CustomerRTCId; " +
                                         "UPDATE Bookings " +
                                         "SET CustomerRTCId=@CustomerRTCId " +
                                         "WHERE Id = @Id;";
            using (var transaction = DbConnection.BeginTransaction())
            {
                await DbConnection.ExecuteAsync(updateRoleSql, new
                {
                    CustomerRTCId=CustomerRTCId,
                    Id= Id

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

        public virtual async Task<bool> DisconnectConnectfromMeetingAsync(string RTCId)
        {
            const string updateRoleSql = "UPDATE Bookings " +
                                         "SET CustomerRTCId = NULL " +
                                         "WHERE CustomerRTCId = @RTCId; " +
                                         "UPDATE Bookings " +
                                         "SET SupervisorRTCId = NULL " +
                                         "WHERE SupervisorRTCId = @RTCId; ";
                                         
            using (var transaction = DbConnection.BeginTransaction())
            {
                await DbConnection.ExecuteAsync(updateRoleSql, new
                {
                    RTCId = RTCId
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
        public virtual async Task<bool> ConnectSupervisortoMeetingAsync(string SupervisorRTCId, Guid Id)
        {
            const string updateRoleSql = "UPDATE Bookings " +
                                         "SET SupervisorRTCId = NULL " +
                                         "WHERE SupervisorRTCId = @SupervisorRTCId; " +
                                         "UPDATE Bookings " +
                                         "SET CustomerRTCId = NULL " +
                                         "WHERE CustomerRTCId = @SupervisorRTCId; " +
                                         "UPDATE Bookings " +
                                         "SET SupervisorRTCId=@SupervisorRTCId " +
                                         "WHERE Id = @Id;";
            using (var transaction = DbConnection.BeginTransaction())
            {
                await DbConnection.ExecuteAsync(updateRoleSql, new
                {
                    SupervisorRTCId = SupervisorRTCId,
                    Id = Id

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
        public virtual async Task<bool> UpdateChatHistoryMeetingAsync(string ChatHistory, Guid Id)
        {
            const string updateRoleSql = "UPDATE Bookings " +
                                         "SET ChatHistory=@ChatHistory " +
                                         "WHERE Id = @Id;";
            using (var transaction = DbConnection.BeginTransaction())
            {
                await DbConnection.ExecuteAsync(updateRoleSql, new
                {
                    ChatHistory = ChatHistory,
                    Id = Id

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
