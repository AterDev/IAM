# IAM 项目架构规范

## 核心原则

### 1. 分层规范

```
Share 层 (纯业务逻辑)
  ├─ OAuthService - Token 生成、验证等业务逻辑（不包含数据库操作）
  └─ 其他业务服务

Modules 层 (业务管理)
  ├─ SigningKeyManager - 密钥管理（包含数据库操作）
  ├─ TokenManager - Token 管理（包含数据库操作）
  └─ 其他 Manager

Controllers 层 (请求处理)
  └─ 协调多个 Manager，调用 Share 层服务
```

### 2. Manager 规范

#### ✅ 应该做的
- 处理与数据库相关的业务逻辑
- 单一职责：一个 Manager 处理一个聚合根的业务
- 注入 DefaultDbContext 和相应的 Service

#### ❌ 不应该做的
- **Manager 之间不能相互调用** - 避免循环依赖
- 包含纯业务逻辑（应该放在 Share 层 Service）
- 处理 HTTP 请求/响应

### 3. Service 规范 (Share 层)

#### ✅ 应该做的
- 提供纯业务逻辑
- 不使用 DefaultDbContext
- 数据由调用者传入

#### 示例
```csharp
// Share/Services/OAuthService.cs
public class OAuthService
{
    public async Task<string> GenerateTokenAsync(
        IEnumerable<Claim> claims,
        SigningKey signingKey,  // ← 由调用者传入
        int expiresInSeconds
    )
    {
        // 纯业务逻辑：生成 JWT
    }

    public static bool ValidatePkce(
        string codeVerifier,
        string codeChallenge,
        string method
    )
    {
        // 纯业务逻辑：PKCE 验证
    }
}
```

### 4. 初始化规范

#### InitModule 职责
- 直接操作 DefaultDbContext
- 不调用任何 Manager
- 只在应用启动时执行一次

```csharp
// AccessMod/InitModule.cs
public static class InitModule
{
    public static async Task InitializeSigningKeysAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DefaultDbContext>();
        
        // 直接操作 dbContext，不调用 SigningKeyManager
        var existingKey = await dbContext.SigningKeys
            .Where(k => k.IsActive && !k.IsDeleted)
            .FirstOrDefaultAsync();
        
        if (existingKey == null)
        {
            // 创建初始密钥
            var (publicKey, privateKey) = HashCrypto.GenerateRsaKeyPair(2048);
            dbContext.SigningKeys.Add(new SigningKey { ... });
            await dbContext.SaveChangesAsync();
        }
    }
}
```

### 5. Controller 规范

#### 职责
- 协调多个 Manager 完成业务逻辑
- 处理 HTTP 请求/响应
- 调用 Share 层的 Service

#### 示例
```csharp
[ApiController]
public class OAuthController(
    TokenManager tokenManager,
    SigningKeyManager signingKeyManager,
    OAuthService oauthService,
    ILogger<OAuthController> logger
) : ControllerBase
{
    [HttpPost("token")]
    public async Task<ActionResult<TokenResponseDto>> Token([FromForm] TokenRequestDto request)
    {
        // 1. 获取签名密钥 (从 SigningKeyManager)
        var signingKey = await _signingKeyManager.GetActiveSigningKeyAsync();
        if (signingKey == null)
            return BadRequest("No active signing key");

        // 2. 生成 Token (使用 OAuthService 纯业务逻辑)
        var token = await _oauthService.GenerateTokenAsync(
            claims,
            signingKey,  // 明确传递
            3600
        );

        // 3. 保存 Token (使用 TokenManager)
        await _tokenManager.StoreTokenAsync(token);

        return Ok(new { access_token = token });
    }
}
```

### 6. 数据流

```
Controller
  ↓
1. 从 Manager A 获取数据
  ↓
2. 调用 Share Service 处理业务逻辑
  ↓
3. 从 Manager B 获取数据
  ↓
4. 使用其他 Manager 保存结果
  ↓
Response
```

## 依赖关系

### ✅ 允许的依赖
- Controller → Manager (可以依赖多个)
- Controller → Share Service
- Manager → DefaultDbContext
- Manager → Share Service
- Share Service → 工具类

### ❌ 禁止的依赖
- Manager → Manager (容易产生循环引用)
- Manager → Controller
- Share Service → Manager
- Share Service → DefaultDbContext

## 常见错误

### 错误 1: Manager 之间相互调用
```csharp
// ❌ 错误
public class TokenManager
{
    private readonly SigningKeyManager _signingKeyManager;  // 不应该注入
    
    public async Task GenerateToken()
    {
        var key = await _signingKeyManager.GetActiveKeyAsync();  // ❌
    }
}
```

**正确做法**: 在 Controller 中处理
```csharp
// ✅ 正确
[ApiController]
public class OAuthController(
    TokenManager tokenManager,
    SigningKeyManager signingKeyManager
) : ControllerBase
{
    public async Task Token()
    {
        var key = await _signingKeyManager.GetActiveKeyAsync();
        var token = await _tokenManager.GenerateToken(key);
    }
}
```

### 错误 2: Share Service 访问数据库
```csharp
// ❌ 错误
public class OAuthService
{
    private readonly DefaultDbContext _dbContext;  // 不应该在 Share 层
    
    public async Task<string> GenerateTokenAsync()
    {
        var key = await _dbContext.SigningKeys.First();  // ❌
    }
}
```

**正确做法**: 由调用者提供数据
```csharp
// ✅ 正确
public class OAuthService
{
    public async Task<string> GenerateTokenAsync(
        IEnumerable<Claim> claims,
        SigningKey signingKey,  // ← 由 Manager 或 Controller 获取并传递
        int expiresInSeconds
    )
    {
        // 纯业务逻辑
    }
}
```

### 错误 3: InitModule 调用 Manager
```csharp
// ❌ 错误
public static class InitModule
{
    public static async Task Initialize(IServiceProvider sp)
    {
        var keyManager = sp.GetRequiredService<SigningKeyManager>();
        await keyManager.InitializeAsync();  // ❌ 不应该调用 Manager
    }
}
```

**正确做法**: 直接操作 DbContext
```csharp
// ✅ 正确
public static class InitModule
{
    public static async Task Initialize(IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DefaultDbContext>();
        
        // 直接操作 dbContext
        if (!await dbContext.SigningKeys.AnyAsync(k => k.IsActive))
        {
            dbContext.SigningKeys.Add(new SigningKey { ... });
            await dbContext.SaveChangesAsync();
        }
    }
}
```

## 检查清单

项目新增代码时，请确认：

- [ ] Share 层 Service 不依赖 DefaultDbContext
- [ ] Share 层 Service 不调用任何 Manager
- [ ] Manager 不相互调用
- [ ] Manager 只注入 DefaultDbContext 和 Share Service
- [ ] InitModule 只操作 DefaultDbContext，不调用 Manager
- [ ] Controller 是唯一调用多个 Manager 的地方
- [ ] 没有循环项目依赖 (IdentityMod ↔ AccessMod)

