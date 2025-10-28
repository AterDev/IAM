# Entity Reorganization and Client Simplification

## 概述

根据 @niltor 的反馈，对实体结构进行了两项重要改进，以提高代码的可维护性和简洁性。

## 改进 1: 按模块组织实体

### 变更前
所有实体都位于 `Entity` 命名空间的根目录下：
```
src/Definition/Entity/
├── User.cs
├── Client.cs
├── AuditLog.cs
└── ... (所有实体在同一目录)
```

### 变更后
实体按模块分组到子命名空间：
```
src/Definition/Entity/
├── Identity/
│   ├── User.cs
│   ├── Role.cs
│   ├── UserRole.cs
│   ├── UserClaim.cs
│   ├── RoleClaim.cs
│   ├── UserLogin.cs
│   ├── UserToken.cs
│   ├── Organization.cs
│   ├── OrganizationUser.cs
│   └── LoginSession.cs
├── Access/
│   ├── Client.cs
│   ├── ClientScope.cs
│   ├── ApiScope.cs
│   ├── ScopeClaim.cs
│   ├── ApiResource.cs
│   ├── Authorization.cs
│   └── Token.cs
└── Common/
    ├── Tenant.cs
    ├── AuditLog.cs
    ├── SystemSetting.cs
    └── SigningKey.cs
```

### 命名空间映射
- **Identity 模块**: `Entity.Identity`
  - 用户、角色、组织等身份管理实体
- **Access 模块**: `Entity.Access`
  - OAuth/OIDC 客户端、授权、令牌等访问控制实体
- **Common 模块**: `Entity.Common`
  - 租户、审计日志、系统设置等公共实体

### GlobalUsings 更新
所有引用 Entity 的项目都更新了 GlobalUsings：

```csharp
// Entity 项目
global using Entity.Identity;
global using Entity.Access;
global using Entity.Common;

// EntityFramework 项目
global using Entity;
global using Entity.Identity;
global using Entity.Access;
global using Entity.Common;

// IdentityMod 项目
global using Entity;
global using Entity.Identity;
global using Entity.Access;
global using Entity.Common;

// AccessMod 项目
global using Entity;
global using Entity.Identity;
global using Entity.Access;
global using Entity.Common;
```

## 改进 2: 简化 Client 实体的重定向 URI 存储

### 变更前
使用独立的实体表存储重定向 URI：

```csharp
// Client.cs
public class Client : EntityBase
{
    // ...
    public List<ClientRedirectUri> RedirectUris { get; set; } = [];
    public List<ClientPostLogoutRedirectUri> PostLogoutRedirectUris { get; set; } = [];
}

// ClientRedirectUri.cs
public class ClientRedirectUri : EntityBase
{
    public Guid ClientId { get; set; }
    public required string Uri { get; set; }
    public Client Client { get; set; } = null!;
}

// ClientPostLogoutRedirectUri.cs
public class ClientPostLogoutRedirectUri : EntityBase
{
    public Guid ClientId { get; set; }
    public required string Uri { get; set; }
    public Client Client { get; set; } = null!;
}
```

这种方式会创建两个额外的数据库表：
- `ClientRedirectUris`
- `ClientPostLogoutRedirectUris`

### 变更后
使用 JSON 数组直接存储在 Client 表中：

```csharp
// Client.cs
public class Client : EntityBase
{
    // ...
    /// <summary>
    /// Redirect URIs (JSON array)
    /// </summary>
    public List<string> RedirectUris { get; set; } = [];

    /// <summary>
    /// Post logout redirect URIs (JSON array)
    /// </summary>
    public List<string> PostLogoutRedirectUris { get; set; } = [];
}
```

### EF Core 配置
使用 FluentAPI 配置 JSON 序列化：

```csharp
// DefaultDbContext.cs - ConfigureAccessEntities
builder.Entity<Client>(entity =>
{
    entity.HasIndex(e => e.ClientId).IsUnique();
    entity.HasIndex(e => e.TenantId);
    
    // Configure RedirectUris as JSON
    entity.Property(e => e.RedirectUris)
        .HasConversion(
            v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
            v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>()
        );
    
    // Configure PostLogoutRedirectUris as JSON
    entity.Property(e => e.PostLogoutRedirectUris)
        .HasConversion(
            v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
            v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>()
        );
});
```

### 查询逻辑简化

**变更前**：
```csharp
// AuthorizationManager.cs
var client = await _dbContext.Clients
    .Include(c => c.RedirectUris)  // 需要 Include 关联表
    .Include(c => c.ClientScopes)
        .ThenInclude(cs => cs.Scope)
    .FirstOrDefaultAsync(c => c.ClientId == request.ClientId);

// 验证逻辑
var redirectUri = client.RedirectUris.FirstOrDefault(r => r.Uri == request.RedirectUri);
if (redirectUri == null)
{
    return (false, "invalid_request", client);
}
```

**变更后**：
```csharp
// AuthorizationManager.cs
var client = await _dbContext.Clients
    // 不需要 Include，RedirectUris 直接在 Client 中
    .Include(c => c.ClientScopes)
        .ThenInclude(cs => cs.Scope)
    .FirstOrDefaultAsync(c => c.ClientId == request.ClientId);

// 验证逻辑更简单
if (!client.RedirectUris.Contains(request.RedirectUri))
{
    return (false, "invalid_request", client);
}
```

## 优势

### 实体模块化组织
1. **更好的代码组织**: 实体按业务模块分组，更容易定位
2. **清晰的依赖关系**: 命名空间明确表明实体所属模块
3. **更好的可维护性**: 相关实体集中在一起

### Client 简化
1. **减少数据库表**: 从 3 个表减少到 1 个表
2. **简化查询**: 不需要 Include 关联表
3. **更好的性能**: 减少 JOIN 操作
4. **代码更简洁**: 验证逻辑更直观

## 影响范围

### 已更新的文件
1. **实体文件** (23 个)
   - 移动到模块子目录
   - 命名空间更新

2. **删除的文件** (2 个)
   - `ClientRedirectUri.cs`
   - `ClientPostLogoutRedirectUri.cs`

3. **配置文件**
   - `Entity/GlobalUsings.cs`
   - `EntityFramework/GlobalUsings.cs`
   - `IdentityMod/GlobalUsings.cs`
   - `AccessMod/GlobalUsings.cs`
   - `EntityFramework/DBProvider/DefaultDbContext.cs`

4. **Manager 文件**
   - `IdentityMod/Managers/AuthorizationManager.cs`

### 需要后续操作
1. **数据库迁移**: 需要创建新的 migration 来：
   - 删除 `ClientRedirectUris` 表
   - 删除 `ClientPostLogoutRedirectUris` 表
   - 在 `Clients` 表中添加 JSON 列

2. **数据迁移**: 如果有现有数据，需要迁移：
   ```sql
   -- 示例：将关联表数据迁移到 JSON
   UPDATE Clients
   SET RedirectUris = (
       SELECT JSON_ARRAYAGG(Uri)
       FROM ClientRedirectUris
       WHERE ClientId = Clients.Id
   )
   ```

## 兼容性

### DTO 兼容性
AccessMod 的 DTOs 已经使用 `List<string>`，因此无需修改：

```csharp
// ClientDetailDto.cs - 已兼容
public class ClientDetailDto
{
    // ...
    public List<string> RedirectUris { get; set; } = [];
    public List<string> PostLogoutRedirectUris { get; set; } = [];
    // ...
}
```

## 提交历史

1. **a6cc7ed** - Reorganize entities by module and simplify Client redirect URIs
   - 实体文件移动和重命名
   - Client 实体简化
   - DbContext 配置更新
   - AuthorizationManager 更新

2. **84f107e** - Update GlobalUsings to include entity subnamespaces
   - 所有项目的 GlobalUsings 更新

## 总结

这些改进使代码结构更清晰，减少了不必要的复杂性：
- ✅ 实体按模块组织，提高可维护性
- ✅ 简化 Client 实体，减少数据库表
- ✅ 查询逻辑更简单，性能更好
- ✅ 符合单一职责原则
- ✅ 为未来扩展奠定良好基础
