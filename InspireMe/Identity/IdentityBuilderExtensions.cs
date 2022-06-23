using System;
using System.Reflection;
using InspireMe.Identitiy;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using InspireMe.Data;
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods on <see cref="IdentityBuilder"/> class.
    /// </summary>
    public static class IdentityBuilderExtensions
    {
        /// <summary>
        /// Adds a Dapper implementation of ASP.NET Core Identity stores.
        /// </summary>
        /// <param name="builder">Helper functions for configuring identity services.</param>
        /// <param name="configureAction">Delegate for configuring options </param>
        /// <returns>The <see cref="IdentityBuilder"/> instance this method extends.</returns>
        public static IdentityBuilder AddDapperStores(this IdentityBuilder builder, string connectionString) {
            AddStores(builder.Services, builder.UserType, builder.RoleType, connectionString);
            return builder;
        }

        private static void AddStores(IServiceCollection services, Type userType, Type roleType, string connectionString) {
            var identityUserType = FindGenericBaseType(userType, typeof(IdentityUser<>));
            if (identityUserType == null) {
                throw new InvalidOperationException($"Method {nameof(AddDapperStores)} can only be called with a user that derives from IdentityUser<TKey>.");
            }
            var serviceProvider = services.BuildServiceProvider();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
           
            
            
            var keyType = identityUserType.GenericTypeArguments[0];
            services.TryAddScoped(typeof(IDatabaseConnectionFactory), x => {
                var dbConnectionFactoryInstance = new PostgresConnectionFactory(connectionString);
                return dbConnectionFactoryInstance;
            });
            Type userStoreType;
            var userClaimType = typeof(IdentityUserClaim<>).MakeGenericType(keyType);
            var userRoleType = typeof(IdentityUserRole<>).MakeGenericType(keyType);
            var userLoginType = typeof(IdentityUserLogin<>).MakeGenericType(keyType);
            var roleClaimType = typeof(IdentityRoleClaim<>).MakeGenericType(keyType);
            var userTokenType = typeof(IdentityUserToken<>).MakeGenericType(keyType);
            if (roleType != null) {
                var identityRoleType = FindGenericBaseType(roleType, typeof(IdentityRole<>));
                if (identityRoleType == null) {
                    throw new InvalidOperationException($"Method {nameof(AddDapperStores)} can only be called with a role that derives from IdentityRole<TKey>.");
                }
                services.TryAddScoped(
                    typeof(IUsersTable<,,,,,>).MakeGenericType(userType, keyType, userClaimType, userRoleType, userLoginType, userTokenType),
                    typeof(UsersTable<,,,,,>).MakeGenericType(userType, keyType, userClaimType, userRoleType, userLoginType, userTokenType)
                );
                services.TryAddScoped(typeof(IRolesTable<,,>).MakeGenericType(roleType, keyType, roleClaimType), typeof(RolesTable<,,>).MakeGenericType(roleType, keyType, roleClaimType));
                services.TryAddScoped(typeof(IUserRolesTable<,,>).MakeGenericType(roleType, keyType, userRoleType), typeof(UserRolesTable<,,>).MakeGenericType(roleType, keyType, userRoleType));
                services.TryAddScoped(typeof(IRoleClaimsTable<,>).MakeGenericType(keyType, roleClaimType), typeof(RoleClaimsTable<,>).MakeGenericType(keyType, roleClaimType));
                services.TryAddScoped(typeof(IRoleStore<>).MakeGenericType(roleType), typeof(RoleStore<,,,>).MakeGenericType(roleType, keyType, userRoleType, roleClaimType));
                userStoreType = typeof(UserStore<,,,,,,,>).MakeGenericType(userType, roleType, keyType, userClaimType, userRoleType, userLoginType, userTokenType, roleClaimType);
            } else {
                services.TryAddScoped(
                    typeof(IUsersOnlyTable<,,,,>).MakeGenericType(userType, keyType, userClaimType, userLoginType, userTokenType),
                    typeof(UsersTable<,,,,,>).MakeGenericType(userType, keyType, userClaimType, roleType, userLoginType, userTokenType)
                );
                userStoreType = typeof(UserOnlyStore<,,,,>).MakeGenericType(userType, keyType, userClaimType, userLoginType, userTokenType);
            }
            services.TryAddScoped(typeof(IUserClaimsTable<,>).MakeGenericType(keyType, userClaimType), typeof(UserClaimsTable<,>).MakeGenericType(keyType, userClaimType));
            services.TryAddScoped(typeof(IUserLoginsTable<,,>).MakeGenericType(userType, keyType, userLoginType), typeof(UserLoginsTable<,,>).MakeGenericType(userType, keyType, userLoginType));
            services.TryAddScoped(typeof(IUserTokensTable<,>).MakeGenericType(keyType, userTokenType), typeof(UserTokensTable<,>).MakeGenericType(keyType, userTokenType));
            services.TryAddScoped(typeof(IUserStore<>).MakeGenericType(userType), userStoreType);
        }

        private static TypeInfo FindGenericBaseType(Type currentType, Type genericBaseType) {
            var type = currentType;
            while (type != null) {
                var typeInfo = type.GetTypeInfo();
                var genericType = type.IsGenericType ? type.GetGenericTypeDefinition() : null;
                if (genericType != null && genericType == genericBaseType) {
                    return typeInfo;
                }
                type = type.BaseType;
            }
            return null;
        }
    }
}
