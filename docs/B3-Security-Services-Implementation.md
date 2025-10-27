# Common Security Services - B3 Implementation

本文档说明了 IAM 项目中通用安全服务的实现。

## 概述

根据 `tasks/iam-development-plan.md#b3` 的要求，本实现提供了以下核心安全能力:

1. **密码哈希服务** (PasswordHasherService)
2. **JWT 令牌服务** (JwtTokenService)
3. **密钥管理服务** (KeyManagementService)
4. **签名密钥实体** (SigningKey)

## 已实现组件

### 1. SigningKey 实体

位置: `src/Definition/Entity/SigningKey.cs`

用于存储 JWT 签名密钥的元数据。

**字段:**
- `KeyId`: 密钥唯一标识符
- `Algorithm`: 签名算法 (如 RS256)
- `KeyType`: 密钥类型 (RSA, EC 等)
- `PublicKey`: 公钥 (PEM 格式)
- `PrivateKey`: 私钥 (加密的 PEM 格式)
- `Usage`: 密钥用途 (签名, 加密)
- `ActivationDate`: 激活日期
- `ExpirationDate`: 过期日期
- `IsActive`: 是否激活
- `IsRevoked`: 是否撤销

### 2. PasswordHasherService

位置: `src/Definition/Share/Services/PasswordHasherService.cs`

基于 PBKDF2 算法的密码哈希服务。

**功能:**
- `HashPassword(string password)`: 使用 PBKDF2 哈希密码
- `VerifyPassword(string hashedPassword, string providedPassword)`: 验证密码
- `NeedsRehash(string hashedPassword)`: 检查密码是否需要重新哈希

**算法参数:**
- Salt 大小: 16 字节 (128 bits)
- Hash 大小: 32 字节 (256 bits)
- 迭代次数: 100,000
- PRF: HMACSHA256

### 3. JwtTokenService

位置: `src/Definition/Share/Services/JwtTokenService.cs`

JWT 令牌生成和验证服务。

**功能:**
- `GenerateAccessToken(IEnumerable<Claim> claims, int expiresIn)`: 生成访问令牌
- `GenerateIdToken(IEnumerable<Claim> claims, int expiresIn)`: 生成 ID 令牌 (OIDC)
- `ValidateToken(string token)`: 验证令牌并返回 ClaimsPrincipal
- `GetTokenClaims(string token)`: 从令牌提取声明(不验证)

**配置项:**
- `Jwt:Issuer`: 令牌签发者
- `Jwt:Audience`: 令牌受众

### 4. KeyManagementService

位置: `src/Definition/Share/Services/KeyManagementService.cs`

JWT 签名密钥管理服务，支持密钥轮换和 JWK 导出。

**功能:**
- `GetSigningCredentials()`: 获取当前签名凭据
- `GetTokenValidationParameters()`: 获取令牌验证参数
- `RotateKeyAsync()`: 轮换签名密钥
- `GetCurrentKeyId()`: 获取当前密钥 ID
- `GetPublicKeyJwkAsync(string? keyId)`: 获取 JWK 格式的公钥

**特性:**
- 使用 RSA 2048 位密钥
- 支持密钥缓存 (60 分钟)
- 支持密钥轮换
- 导出 JWK 格式公钥

**注意:** 当前实现使用内存存储，生产环境应该使用 SigningKey 实体持久化到数据库。

## 依赖注入

所有服务已在 `CommonMod/ModuleExtensions.cs` 中注册:

```csharp
builder.Services.AddSingleton<IPasswordHasher, PasswordHasherService>();
builder.Services.AddSingleton<IKeyManagementService, KeyManagementService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
```

## 单元测试

位置: `tests/Services/`

### 测试覆盖

1. **PasswordHasherServiceTests** (13 个测试用例)
   - 正常哈希和验证流程
   - 异常输入处理
   - 哈希唯一性
   - 重新哈希检测

2. **JwtTokenServiceTests** (15 个测试用例)
   - 访问令牌生成
   - ID 令牌生成
   - 令牌验证
   - 声明提取
   - 过期令牌处理

3. **KeyManagementServiceTests** (12 个测试用例)
   - 签名凭据获取
   - 验证参数配置
   - 密钥轮换
   - JWK 导出
   - 缓存行为

**总计:** 40 个测试用例，覆盖正常和异常路径。

## 使用示例

### 密码哈希

```csharp
// 注入服务
private readonly IPasswordHasher _passwordHasher;

// 哈希密码
var hashedPassword = _passwordHasher.HashPassword("MyPassword123!");

// 验证密码
bool isValid = _passwordHasher.VerifyPassword(hashedPassword, "MyPassword123!");
```

### JWT 令牌生成

```csharp
// 注入服务
private readonly IJwtTokenService _jwtTokenService;

// 创建声明
var claims = new[]
{
    new Claim(ClaimTypes.NameIdentifier, userId),
    new Claim(ClaimTypes.Name, username),
    new Claim(ClaimTypes.Email, email)
};

// 生成访问令牌 (1小时有效期)
var accessToken = _jwtTokenService.GenerateAccessToken(claims, 3600);

// 验证令牌
var principal = _jwtTokenService.ValidateToken(accessToken);
```

### 密钥管理

```csharp
// 注入服务
private readonly IKeyManagementService _keyManagementService;

// 获取当前密钥 ID
var keyId = _keyManagementService.GetCurrentKeyId();

// 获取 JWK 格式的公钥
var jwk = await _keyManagementService.GetPublicKeyJwkAsync();

// 轮换密钥
var newKeyId = await _keyManagementService.RotateKeyAsync();
```

## 配置

在 `appsettings.json` 中添加:

```json
{
  "Jwt": {
    "Issuer": "IAM",
    "Audience": "IAM"
  }
}
```

## 后续工作

1. **KeyManagementService 数据库集成**
   - 实现 SigningKey 实体的数据库持久化
   - 从数据库加载和保存密钥
   - 支持密钥历史记录

2. **密钥轮换策略**
   - 自动密钥轮换调度
   - 密钥过期管理
   - 旧密钥的平滑过渡

3. **性能优化**
   - 密钥缓存优化
   - 令牌验证缓存

## 安全考虑

1. **密码哈希**
   - 使用 PBKDF2 与 100,000 次迭代
   - 每个密码使用随机盐
   - 恒定时间比较防止时序攻击

2. **JWT 令牌**
   - 使用 RS256 算法签名
   - 支持令牌过期验证
   - 5 分钟时钟偏移容差

3. **密钥管理**
   - RSA 2048 位密钥
   - 私钥应加密存储(生产环境)
   - 支持密钥轮换和撤销

## 依赖项

- `Microsoft.AspNetCore.Cryptography.KeyDerivation` - 密码哈希
- `System.IdentityModel.Tokens.Jwt` - JWT 令牌处理
- `Microsoft.IdentityModel.Tokens` - 安全令牌处理
- `Microsoft.Extensions.Caching.Memory` - 密钥缓存

## 参考文档

- [IAM Development Plan - B3](tasks/iam-development-plan.md)
- [PBKDF2 Specification](https://tools.ietf.org/html/rfc2898)
- [JWT Specification](https://tools.ietf.org/html/rfc7519)
- [JSON Web Key (JWK)](https://tools.ietf.org/html/rfc7517)
