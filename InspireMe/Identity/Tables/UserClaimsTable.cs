using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Identity;
using InspireMe.Data;
using System.Security.Claims;

namespace InspireMe.Identity
{
    /// <summary>
    /// The default implementation of <see cref="IUserClaimsTable{TKey, TUserClaim}"/>.
    /// </summary>
    /// <typeparam name="TKey">The type of the primary key for a user.</typeparam>
    /// <typeparam name="TUserClaim">The type representing a claim.</typeparam>
    public class UserClaimsTable<TKey, TUserClaim> :
        Table,
        IUserClaimsTable<TKey, TUserClaim>
        where TKey : IEquatable<TKey>
        where TUserClaim : IdentityUserClaim<TKey>, new()
    {
        /// <summary>
        /// Creates a new instance of <see cref="UserClaimsTable{TKey, TUserClaim}"/>.
        /// </summary>
        /// <param name="dbConnectionFactory">A factory for creating instances of <see cref="IDbConnection"/>.</param>
        public UserClaimsTable(IDatabaseConnectionFactory dbConnectionFactory) : base(dbConnectionFactory) { }

        /// <inheritdoc/>
        public virtual async Task<IEnumerable<TUserClaim>> GetClaimsAsync(TKey userId) {
            const string sql = "SELECT * " +
                               "FROM AspNetUserClaims " +
                               "WHERE UserId = @UserId;";
            var userClaims = await DbConnection.QueryAsync<TUserClaim>(sql, new { UserId = userId });
            return userClaims;
        }
        public virtual async Task<IEnumerable<string>> GetClaimValuesByTypeAsync(string type)
        {
            const string sql = "SELECT DISTINCT ClaimValue " +
                               "FROM AspNetUserClaims " +
                               "WHERE ClaimType = @Type;";
            var userClaims = await DbConnection.QueryAsync<string>(sql, new { Type = type });
            return userClaims;
        }
    }
}
