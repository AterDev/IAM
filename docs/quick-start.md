# IAM 快速入门

本指南帮助您快速开始使用IAM系统。

## 使用 .NET Aspire 快速启动（推荐）

最简单的方式是使用 .NET Aspire 一键启动所有服务。

### 环境要求

- .NET 9.0 SDK 或更高版本
- Docker Desktop（用于数据库和缓存容器）
- Node.js 20+ 和 pnpm（前端开发）

### 启动所有服务

```bash
cd src/AppHost
dotnet run
```

Aspire 将自动启动：
- PostgreSQL 数据库容器
- Redis 缓存容器
- 数据库迁移服务（自动创建表和种子数据）
- IAM API 服务 (https://localhost:7070)
- IAM 管理前端 (http://localhost:4200)
- 示例 API 服务 (https://localhost:7000)
- 示例前端应用 (http://localhost:4201)

Aspire Dashboard 将在浏览器中自动打开，显示所有服务的状态。

## 初始配置

系统在首次启动时会自动创建以下默认配置：

### 管理员账号

- 用户名：`admin`
- 密码：`MakeDotnetGreatAgain`
- 邮箱：`admin@iam.local`
- 角色：Administrator

### 默认 OAuth 作用域

- `openid` - OpenID Connect身份认证（必需）
- `profile` - 用户基本信息
- `email` - 用户邮箱地址
- `address` - 用户地址信息
- `phone` - 用户电话号码
- `offline_access` - 离线访问权限（刷新令牌）

### 默认 OAuth 客户端

#### FrontClient（前端应用客户端）
- 客户端ID：`FrontClient`
- 类型：公共客户端（SPA）
- 授权流程：授权码 + PKCE
- 支持的重定向URI：localhost:4200, localhost:4201

#### ApiClient（后端API客户端）
- 客户端ID：`ApiClient`
- 客户端密钥：`ApiClient_Secret_2025`
- 类型：机密客户端
- 授权流程：客户端凭证流程

## 访问系统

### 访问管理门户

浏览器访问：`http://localhost:4200`

### 登录管理门户

1. 点击"登录"按钮
2. 输入凭据：
   - 用户名：`admin`
   - 密码：`MakeDotnetGreatAgain`
3. 点击"登录"

登录成功后，您将看到管理仪表板。

### 访问示例应用

浏览器访问：`http://localhost:4201`

这是一个完整的示例前端应用，展示如何集成IAM进行身份认证。

## 管理功能

登录后，您可以在管理门户中：

### 用户管理
- 创建新用户
- 分配角色
- 管理用户权限
- 锁定/解锁账户

### 客户端管理
- 查看默认客户端（FrontClient, ApiClient）
- 注册新的OAuth客户端
- 配置重定向URI
- 设置允许的作用域
- 管理客户端密钥

### 作用域管理
- 查看默认作用域
- 创建自定义作用域
- 配置作用域声明
- 设置作用域权限

### API资源管理
- 定义API资源
- 配置资源作用域
- 设置访问策略

### 角色管理
- 创建角色
- 分配权限
- 管理角色成员

## 测试集成

### 使用示例项目

Aspire 已经自动启动了示例项目，您可以直接测试：

#### 测试前端集成

1. 访问示例前端：`http://localhost:4201`
2. 点击"登录"按钮
3. 您将被重定向到IAM登录页面
4. 输入管理员凭据登录
5. 登录成功后返回示例应用
6. 查看用户信息和受保护页面

#### 测试后端API

示例API已在 `https://localhost:7000` 运行。

测试端点：
- 公开端点：`GET https://localhost:7000/api/public`
- 受保护端点：`GET https://localhost:7000/api/protected`（需要令牌）

### 完整的认证流程测试

1. 在示例前端 (4201) 登录
2. 登录成功后获取访问令牌
3. 点击"调用受保护API"按钮
4. 应用会使用访问令牌调用示例API (7000)
5. 查看返回的用户信息

这展示了完整的OAuth 2.0授权码流程 + PKCE。

## 手动启动（高级选项）

如果不使用Aspire，也可以手动启动各个服务：

### 启动数据库

确保PostgreSQL和Redis正在运行。

### 运行数据库迁移

```bash
cd src/Services/MigrationService
dotnet run
```

### 启动IAM API

```bash
cd src/Services/ApiService
dotnet run
```

API将在 `https://localhost:7070` 启动。

### 启动IAM前端

```bash
cd src/ClientApp/WebApp
pnpm install
pnpm start
```

前端将在 `http://localhost:4200` 启动。

### 启动示例后端

```bash
cd samples/backend-dotnet
dotnet run
```

将在 `https://localhost:7000` 启动。

### 启动示例前端

```bash
cd samples/frontend-angular
pnpm install
pnpm start
```

将在 `http://localhost:4201` 启动。

## 服务端口总览

| 服务 | 端口 | 说明 |
|------|------|------|
| IAM API | 7070 | 身份认证和授权服务 |
| IAM 管理前端 | 4200 | 管理后台界面 |
| 示例 API | 7000 | 示例后端服务 |
| 示例前端 | 4201 | 示例前端应用 |
| PostgreSQL | 15432 | 数据库 |
| Redis | 16379 | 缓存 |

## 安全注意事项

⚠️ **重要提示**

默认管理员密码仅用于开发和测试！

**生产环境部署前，必须：**

1. 立即更改管理员密码
2. 使用强密码（至少16个字符）
3. 启用双因素认证
4. 实施账户锁定策略
5. 定期审计管理员操作
6. 限制管理员访问的IP范围

### 修改管理员密码

登录后：
1. 导航到 **用户** → 找到admin用户
2. 点击编辑
3. 修改密码
4. 保存更改

## 常见问题

### Q: 无法使用管理员账号登录？

**检查：**
- 确认用户名为小写 `admin`
- 密码为 `MakeDotnetGreatAgain`（区分大小写）
- 确认数据库迁移已成功运行

### Q: 示例应用无法连接到IAM？

**检查：**
- IAM服务器正在运行（端口7070）
- URL配置正确：`https://localhost:7070`
- CORS配置允许示例应用的源
- 默认客户端（FrontClient, ApiClient）已自动创建

### Q: 端口被占用怎么办？

**解决方案：**
- 检查并停止占用端口的服务
- 或修改 `src/AppHost/Program.cs` 中的端口配置
- 同时需要更新示例项目的配置文件

### Q: 如何创建新用户？

1. 使用管理员登录
2. 导航到 **用户** → **创建新用户**
3. 填写用户信息
4. 设置初始密码
5. 分配角色（可选）
6. 保存

### Q: 如何重置用户密码？

1. 使用管理员登录
2. 导航到 **用户** → 找到目标用户
3. 点击编辑
4. 修改密码
5. 保存更改

## 更多资源

- [完整文档](../README.md)
- [集成测试指南](integration-testing.md)
- [示例项目文档](../samples/README.md)
- [API文档](api-documentation.md)

## 获取帮助

如有问题或需要帮助：
1. 查看文档和FAQ
2. 检查GitHub Issues
3. 提交新的Issue
