using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;
using Perigon.AspNetCore.Constants;
using Share.Implement;

namespace Tests.IAMMod.Managers;

internal static class ManagerTestFactory
{
    public static TManager Create<TManager, TEntity>(
        DefaultDbContext dbContext,
        IReadOnlyDictionary<string, object?>? managerFields = null
    )
        where TManager : ManagerBase<DefaultDbContext, TEntity>
        where TEntity : class, IEntityBase
    {
        var manager = (TManager)RuntimeHelpers.GetUninitializedObject(typeof(TManager));
        var baseType = typeof(ManagerBase<DefaultDbContext, TEntity>);

        SetField(baseType, manager, "_logger", NullLogger<TManager>.Instance);
        SetField(baseType, manager, "_dbContext", dbContext);
        SetField(baseType, manager, "_dbSet", dbContext.Set<TEntity>());
        SetField(baseType, manager, "_userContext", CreateAdminUserContext());
        SetField(baseType, manager, "_isMultiTenant", false);

        var queryableProperty = baseType.GetProperty("Queryable", BindingFlags.Instance | BindingFlags.NonPublic);
        queryableProperty?.SetValue(manager, dbContext.Set<TEntity>().AsNoTracking().AsQueryable());

        if (managerFields is not null)
        {
            foreach (var (fieldName, value) in managerFields)
            {
                SetField(typeof(TManager), manager, fieldName, value);
            }
        }

        return manager;
    }

    private static IUserContext CreateAdminUserContext()
    {
        var userId = Guid.CreateVersion7();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, "resource-admin"),
            new Claim(ClaimTypes.Role, WebConst.AdminUser),
        };

        return new UserContext(new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
            }
        });
    }

    private static void SetField(Type declaringType, object target, string fieldName, object? value)
    {
        var field = declaringType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Field '{fieldName}' was not found on '{declaringType.FullName}'.");

        field.SetValue(target, value);
    }
}
