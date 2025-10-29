# IAM 快速入门

本指南帮助您快速开始使用IAM系统。

## 初始管理员账号

系统在首次数据库迁移时会自动创建一个默认管理员账号。

**管理员凭据：**
- 用户名：`admin`
- 密码：`MakeDotnetGreatAgain`
- 邮箱：`admin@iam.local`
- 角色：Administrator

## 启动系统

### 1. 运行数据库迁移

首次启动前，需要运行数据库迁移：

```bash
cd src/AppHost
dotnet run
```

这将：
- 创建数据库架构
- 运行所有迁移
- 自动创建管理员账号

### 2. 访问管理门户

浏览器访问：`https://localhost:7001`

### 3. 登录

1. 点击"登录"按钮
2. 输入凭据：
   - 用户名：`admin`
   - 密码：`MakeDotnetGreatAgain`
3. 点击"登录"

## 管理功能

登录后，您可以：

### 用户管理
- 创建新用户
- 分配角色
- 管理用户权限
- 锁定/解锁账户

### 客户端管理
- 注册OAuth客户端
- 配置重定向URI
- 设置允许的作用域
- 管理客户端密钥

### API资源管理
- 定义API资源
- 配置资源作用域
- 设置访问策略

### 角色管理
- 创建角色
- 分配权限
- 管理角色成员

## 集成测试

### 使用示例项目

系统提供了两个示例项目用于测试IAM集成：

#### 后端示例（ASP.NET Core）

```bash
cd samples/backend-dotnet
dotnet run
```

访问：`https://localhost:5001/swagger`

#### 前端示例（Angular）

```bash
cd samples/frontend-angular
npm install
npm start
```

访问：`http://localhost:4200`

### 配置测试客户端

在管理门户中配置Angular示例的客户端：

1. 导航到 **客户端** → **创建新客户端**
2. 配置：
   - 客户端ID：`sample-angular-client`
   - 应用类型：SPA（单页应用）
   - 授权类型：Authorization Code with PKCE
   - 重定向URI：`http://localhost:4200`
   - 允许的作用域：`openid profile email sample-api`

### 测试认证流程

1. 启动IAM服务器
2. 启动Angular示例应用
3. 在浏览器中访问 `http://localhost:4200`
4. 点击"Login"按钮
5. 使用管理员账号登录
6. 验证成功登录并获取令牌

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
- IAM服务器正在运行
- URL配置正确（默认：https://localhost:7001）
- CORS配置允许示例应用的源
- 客户端已在IAM中注册

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

- [完整文档](README.md)
- [集成测试指南](docs/integration-testing.md)
- [示例项目文档](samples/README.md)
- [API文档](docs/api-documentation.md)

## 获取帮助

如有问题或需要帮助：
1. 查看文档和FAQ
2. 检查GitHub Issues
3. 提交新的Issue
