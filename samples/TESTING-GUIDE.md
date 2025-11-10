# IAM示例项目测试指南

本文档提供详细的步骤来测试IAM示例项目的完整集成。

## 前置准备

### 1. 确保IAM服务器运行

```bash
# 在IAM项目根目录
cd src/AppHost
dotnet run
```

IAM服务器应该运行在 `https://localhost:7001`

### 2. 验证客户端配置

登录IAM管理后台：`https://localhost:7001`
- 用户名: `admin`
- 密码: `MakeDotnetGreatAgain`

#### 验证ApiTest客户端

1. 导航到"应用管理"
2. 查找客户端ID为 `ApiTest` 的客户端
3. 确认配置：
   - 客户端类型: 资源服务器/API
   - 允许的作用域: 根据需要配置

如果不存在，创建新客户端：
- 客户端ID: `ApiTest`
- 客户端名称: API测试
- 应用类型: Web应用/资源服务器
- 保存并记录客户端ID

#### 验证FrontTest客户端

1. 在"应用管理"中查找客户端ID为 `FrontTest` 的客户端
2. 确认配置：
   - 客户端类型: 公共客户端
   - 应用类型: 单页应用(SPA)
   - 授权类型: 授权码
   - 需要PKCE: 是
   - 需要客户端密钥: 否
   - 重定向URI: 必须包含以下URL
     ```
     http://localhost:4200
     http://localhost:4200/
     ```
   - 登出后重定向URI:
     ```
     http://localhost:4200
     http://localhost:4200/
     ```
   - 允许的作用域:
     ```
     openid
     profile
     email
     ApiTest
     ```

如果不存在，创建新客户端：
- 客户端ID: `FrontTest`
- 客户端名称: 前端测试
- 应用类型: 单页应用(SPA)
- 客户端类型: 公共客户端
- 授权类型: 勾选"授权码"
- 需要PKCE: 勾选
- 需要客户端密钥: 不勾选
- 添加上述重定向URI和作用域
- 保存

## 测试步骤

### 阶段1: 测试后端API

#### 1.1 启动后端示例

```bash
cd samples/backend-dotnet
dotnet run
```

API应该运行在 `https://localhost:5001`

#### 1.2 测试公开端点

使用curl或浏览器访问：

```bash
curl https://localhost:5001/api/public
```

预期响应：
```json
{
  "message": "这是一个公开端点，无需认证",
  "timestamp": "2024-XX-XXTXX:XX:XXZ"
}
```

#### 1.3 测试Swagger UI

1. 访问 `https://localhost:5001/swagger`
2. 确认Swagger UI加载成功
3. 查看可用的端点：
   - GET /api/public
   - GET /api/protected
   - GET /api/weatherforecast
   - GET /api/weatherforecast/forecast/{days}

#### 1.4 获取访问令牌

使用密码流程获取令牌（仅用于测试）：

```bash
curl -X POST https://localhost:7001/connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password" \
  -d "client_id=FrontTest" \
  -d "username=admin" \
  -d "password=MakeDotnetGreatAgain" \
  -d "scope=openid profile email ApiTest"
```

保存返回的 `access_token`。

#### 1.5 测试受保护端点

使用获取的令牌调用受保护端点：

```bash
curl -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  https://localhost:5001/api/protected
```

预期响应应包含：
```json
{
  "message": "这是一个受保护端点，需要有效的JWT令牌",
  "user": {
    "isAuthenticated": true,
    "name": "admin",
    "subject": "...",
    "email": "admin@iam.local",
    "claims": [...]
  },
  "timestamp": "..."
}
```

#### 1.6 在Swagger UI中测试

1. 在Swagger UI中，点击右上角的"Authorize"按钮
2. 在弹出的对话框中输入: `Bearer YOUR_ACCESS_TOKEN`
3. 点击"Authorize"，然后"Close"
4. 展开 `/api/protected` 端点
5. 点击"Try it out"
6. 点击"Execute"
7. 确认返回200状态码和用户信息

### 阶段2: 测试前端应用

#### 2.1 启动前端示例

在新终端窗口中：

```bash
cd samples/frontend-angular
npm install
npm start
```

应用应该运行在 `http://localhost:4200`

#### 2.2 访问首页

1. 在浏览器中打开 `http://localhost:4200`
2. 确认页面加载成功
3. 验证显示：
   - 应用标题: "🔐 IAM示例 - Angular应用"
   - 导航菜单: "首页"、"受保护页面"、"登录"按钮
   - 首页内容，包括功能特性和快速开始指南

#### 2.3 测试登录流程

1. 点击导航栏中的"登录"按钮
2. 验证被重定向到IAM登录页面 (`https://localhost:7001/...`)
3. 输入管理员凭据：
   - 用户名: `admin`
   - 密码: `MakeDotnetGreatAgain`
4. 点击登录
5. 验证被重定向回Angular应用 (`http://localhost:4200`)
6. 确认导航栏显示：
   - "登出"按钮
   - 用户名（例如: "👤 admin"）

#### 2.4 测试受保护页面

1. 点击导航栏中的"受保护页面"
2. 验证页面加载并显示用户信息：
   - 姓名
   - 邮箱
   - 用户ID
3. 展开"🔑 查看访问令牌信息"
4. 确认显示完整的令牌声明

#### 2.5 测试API调用

在受保护页面：

1. 点击"调用公开API"按钮
   - 验证显示成功响应
   - 确认响应包含公开端点的消息

2. 点击"调用受保护API"按钮
   - 验证显示成功响应
   - 确认响应包含用户信息和声明

3. 点击"获取天气预报"按钮
   - 验证显示成功响应
   - 确认响应包含天气预报数据和用户信息

#### 2.6 测试登出流程

1. 点击导航栏中的"登出"按钮
2. 验证被重定向到IAM登出页面
3. 验证返回应用时显示"登录"按钮
4. 确认无法访问受保护页面（会重定向到登录）

### 阶段3: 完整集成测试

#### 3.1 同时运行所有服务

确保以下服务都在运行：
- IAM服务器: `https://localhost:7001`
- 后端API: `https://localhost:5001`
- 前端应用: `http://localhost:4200`

#### 3.2 端到端测试流程

1. **启动状态**: 打开 `http://localhost:4200`，未登录
2. **尝试访问受保护资源**: 点击"受保护页面"
3. **自动重定向**: 重定向到IAM登录
4. **用户认证**: 输入凭据并登录
5. **返回应用**: 自动返回到受保护页面
6. **查看用户信息**: 确认显示正确的用户信息
7. **调用API**: 测试所有API调用按钮
8. **验证令牌**: 确认令牌自动添加到请求头
9. **登出**: 点击登出并验证清理会话

#### 3.3 浏览器开发工具检查

打开浏览器开发工具（F12）：

1. **Console标签**:
   - 查看是否有JavaScript错误
   - OIDC库应该输出调试日志

2. **Network标签**:
   - 调用API时，检查请求头
   - 确认包含 `Authorization: Bearer ...` 头
   - 验证API返回200状态码

3. **Application标签** (Chrome) 或 **Storage标签** (Firefox):
   - 查看Session Storage
   - 确认存储了OIDC相关数据

## 常见问题排查

### 问题1: 前端无法连接到IAM

**症状**: 点击登录后没有反应或出现网络错误

**检查**:
- IAM服务器是否运行
- 浏览器控制台是否有CORS错误
- FrontTest客户端配置是否正确

**解决**:
```bash
# 确认IAM运行
curl https://localhost:7001/.well-known/openid-configuration

# 检查响应是否包含正确的端点
```

### 问题2: API返回401 Unauthorized

**症状**: 调用受保护API时返回401错误

**检查**:
- 访问令牌是否有效
- 令牌是否包含正确的audience
- API配置的Authority是否正确

**解决**:
1. 在浏览器控制台查看网络请求
2. 检查Authorization头是否存在
3. 使用 https://jwt.io 解码令牌，检查audience和过期时间

### 问题3: CORS错误

**症状**: 浏览器控制台显示CORS策略阻止请求

**检查**:
- 后端API的CORS配置
- IAM的CORS配置
- FrontTest客户端的允许源配置

**解决**:
在backend-dotnet的appsettings.json中确认：
```json
{
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:4200",
      "https://localhost:4200"
    ]
  }
}
```

### 问题4: 重定向URI不匹配

**症状**: 登录后出现"redirect_uri_mismatch"错误

**检查**:
- FrontTest客户端的重定向URI配置
- 注意尾部斜杠

**解决**:
确保IAM中FrontTest客户端包含：
- `http://localhost:4200`
- `http://localhost:4200/`

## 性能验证

### 令牌刷新测试

1. 登录后等待访问令牌即将过期（默认15分钟）
2. 观察是否自动刷新令牌
3. 验证API调用继续成功

### 并发请求测试

1. 在受保护页面快速点击多个API调用按钮
2. 验证所有请求都成功
3. 确认令牌正确添加到每个请求

## 清理

测试完成后：

```bash
# 停止所有服务 (Ctrl+C)
# IAM服务器
# 后端API
# 前端应用
```

## 总结检查清单

- [ ] IAM服务器成功启动
- [ ] ApiTest客户端存在并配置正确
- [ ] FrontTest客户端存在并配置正确
- [ ] 后端API成功启动并响应
- [ ] Swagger UI可访问
- [ ] 公开端点无需认证即可访问
- [ ] 使用令牌可以访问受保护端点
- [ ] 前端应用成功启动
- [ ] 登录流程成功完成
- [ ] 用户信息正确显示
- [ ] API调用包含正确的令牌
- [ ] 所有API测试按钮正常工作
- [ ] 登出流程正常

如果所有项目都通过，说明IAM示例项目集成成功！
