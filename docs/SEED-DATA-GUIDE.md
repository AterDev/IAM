# IAM 初始化数据指南

本文档说明IAM系统在首次启动时自动创建的初始化数据。

## 概述

当运行 `MigrationService` 进行数据库迁移时，系统会自动创建以下初始化数据：
- 默认管理员账号
- 默认角色
- OAuth标准作用域
- 默认OAuth客户端

这些数据确保系统可以立即使用，无需手动配置。

## 默认管理员账号

**账号信息：**
- 用户名：`admin`
- 密码：`MakeDotnetGreatAgain`
- 邮箱：`admin@iam.local`
- 角色：Administrator（超级管理员）
- 邮箱已确认：是
- 锁定状态：未锁定

**代码位置：** `src/Services/MigrationService/Worker.cs` - `SeedInitialDataAsync` 方法

## OAuth 标准作用域

系统自动创建以下符合OpenID Connect标准的作用域：

| 作用域名称 | 显示名称 | 描述 | 是否必需 |
|-----------|---------|------|---------|
| openid | OpenID | OpenID Connect身份认证 | ✓ |
| profile | Profile | 用户基本信息 | - |
| email | Email | 用户邮箱地址 | - |
| address | Address | 用户地址信息 | - |
| phone | Phone | 用户电话号码 | - |
| offline_access | Offline Access | 离线访问权限(刷新令牌) | - |

**代码位置：** `src/Services/MigrationService/Worker.cs` - `SeedOAuthDataAsync` 方法

## 默认 OAuth 客户端

### FrontClient - 前端应用客户端

用于单页应用（SPA）和Web前端。

**配置：**
- 客户端ID：`FrontClient`
- 显示名称：前端客户端
- 客户端类型：公共客户端（public）
- 应用类型：SPA
- 是否需要PKCE：是
- 同意类型：隐式（implicit）

**授权流程：**
- 授权码流程（Authorization Code）
- 刷新令牌流程（Refresh Token）

**允许的作用域：**
- openid
- profile
- email
- offline_access

**重定向URI：**
- `http://localhost:4200`
- `https://localhost:4200`
- `http://localhost:4201`
- `https://localhost:4201`

**退出后重定向URI：**
- 同重定向URI

**使用场景：**
- IAM管理前端（端口4200）
- 示例前端应用（端口4201）
- 其他基于浏览器的前端应用

### ApiClient - 后端API客户端

用于后端服务和API之间的机器对机器通信。

**配置：**
- 客户端ID：`ApiClient`
- 客户端密钥：`ApiClient_Secret_2025`（已哈希存储）
- 显示名称：API客户端
- 客户端类型：机密客户端（confidential）
- 应用类型：Web
- 是否需要PKCE：否
- 同意类型：隐式（implicit）

**授权流程：**
- 客户端凭证流程（Client Credentials）

**允许的作用域：**
- openid

**使用场景：**
- 示例后端API（端口7000）
- 其他后端服务
- 微服务间通信

**代码位置：** `src/Services/MigrationService/Worker.cs` - `SeedOAuthDataAsync` 方法

## 数据种子执行流程

1. **检查数据是否已存在**
   - 系统会检查每个数据项是否已存在
   - 如果存在，跳过创建
   - 这确保多次运行迁移服务不会重复创建数据

2. **创建顺序**
   1. 管理员角色
   2. 管理员用户
   3. 用户角色关联
   4. OAuth作用域
   5. OAuth客户端
   6. 客户端作用域关联

3. **密码处理**
   - 所有密码使用 `PasswordHasherService` 进行哈希
   - 采用安全的哈希算法
   - 密码不以明文存储

## 修改默认配置

### 修改管理员密码

**方法1：启动后在管理后台修改**
1. 使用默认密码登录
2. 导航到用户管理
3. 修改admin用户的密码

**方法2：修改种子数据代码**
编辑 `src/Services/MigrationService/Worker.cs`：
```csharp
PasswordHash = passwordHasher.HashPassword("你的新密码"),
```

### 修改客户端配置

**添加新的重定向URI：**
编辑 `SeedOAuthDataAsync` 方法中的 `RedirectUris` 列表：
```csharp
RedirectUris = new List<string>
{
    "http://localhost:4200",
    "https://localhost:4200",
    "http://localhost:4201",
    "https://localhost:4201",
    "https://yourdomain.com",  // 添加新URI
}
```

**修改客户端密钥：**
```csharp
var apiClientSecret = "你的新密钥";
```

### 添加自定义作用域

在 `defaultScopes` 列表中添加：
```csharp
var defaultScopes = new List<(string Name, string DisplayName, string Description, bool Required)>
{
    ("openid", "OpenID", "OpenID Connect身份认证", true),
    // ... 其他默认作用域
    ("custom_scope", "自定义作用域", "自定义作用域描述", false),
};
```

### 添加新的默认客户端

在 `SeedOAuthDataAsync` 方法末尾添加新客户端的创建代码，参考现有的 `FrontClient` 或 `ApiClient` 实现。

## 验证种子数据

启动系统后，可以通过以下方式验证：

### 通过管理后台验证

1. 登录管理后台：`http://localhost:4200`
2. 使用默认管理员账号登录
3. 导航到各个管理页面：
   - 用户管理 - 查看admin用户
   - 客户端管理 - 查看FrontClient和ApiClient
   - 作用域管理 - 查看默认作用域

### 通过数据库验证

连接PostgreSQL数据库：
```bash
psql -h localhost -p 15432 -U postgres -d IAM_dev
```

查询数据：
```sql
-- 查看用户
SELECT * FROM "Users" WHERE "UserName" = 'admin';

-- 查看客户端
SELECT * FROM "Clients";

-- 查看作用域
SELECT * FROM "ApiScopes";

-- 查看客户端作用域关联
SELECT * FROM "ClientScopes";
```

## 安全建议

1. **立即修改管理员密码**
   - 默认密码仅用于开发和测试
   - 生产环境必须使用强密码

2. **修改客户端密钥**
   - `ApiClient_Secret_2025` 是公开的默认密钥
   - 生产环境必须使用强随机密钥

3. **限制重定向URI**
   - 仅添加实际使用的URI
   - 不要使用通配符
   - 使用HTTPS

4. **定期轮换密钥**
   - 定期更新客户端密钥
   - 保持密钥更新日志

5. **启用审计日志**
   - 监控管理员操作
   - 跟踪客户端使用情况

## 故障排除

### 种子数据未创建

**可能原因：**
- 数据库连接失败
- 迁移服务未成功运行
- 数据已存在（检查逻辑跳过了创建）

**解决方案：**
1. 查看迁移服务日志
2. 检查数据库连接字符串
3. 确认PostgreSQL容器正在运行
4. 手动清空数据库并重新运行迁移

### 无法使用默认账号登录

**检查：**
- 密码是否正确（区分大小写）
- 用户名是否为小写 `admin`
- 账号是否被锁定
- 数据库中是否存在该用户

### 客户端配置不正确

**解决方案：**
1. 在管理后台查看客户端配置
2. 验证重定向URI是否正确
3. 检查允许的授权类型
4. 确认客户端作用域配置

## 相关文档

- [快速入门指南](quick-start.md)
- [示例项目文档](../samples/README.md)
- [OAuth实现文档](oauth-implementation.md)
