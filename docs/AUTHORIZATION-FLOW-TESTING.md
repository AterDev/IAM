# 授权流程完整测试指南

本指南详细说明了如何测试完整的OAuth 2.0授权流程，包括用户同意管理功能。

## 前置条件

1. **IAM服务器** 运行在 `https://localhost:7070`
2. **示例后端API** 运行在 `https://localhost:7000`
3. **示例前端应用** 运行在 `http://localhost:4201`
4. 确保数据库已正确迁移和初始化

## 快速启动所有服务

使用 .NET Aspire 一键启动所有服务：

```bash
cd src/AppHost
dotnet run
```

Aspire Dashboard 将自动打开，显示所有服务状态。

或者手动启动各服务：

### 1. 启动IAM服务器

```bash
cd src/Services/ApiService
dotnet run
```

访问: `https://localhost:7070`

### 2. 启动示例后端API

```bash
cd samples/backend-dotnet
dotnet run
```

访问: `https://localhost:7000`

### 3. 启动示例前端应用

```bash
cd samples/frontend-angular
npm install  # 首次运行
npm start
```

访问: `http://localhost:4201`

## 测试场景

### 场景1: 首次授权流程（完整流程）

**目标**: 测试用户首次访问应用时的完整授权流程

**步骤**:

1. **访问前端应用**
   - 打开浏览器访问 `http://localhost:4201`
   - 应该看到首页，显示"未登录"状态

2. **点击"受保护页面"**
   - 点击侧边栏的"受保护页面"链接
   - 由于未登录，自动重定向到IAM登录页面
   - URL应该类似: `https://localhost:7070/Account/Login?returnUrl=...`

3. **登录**
   - 使用默认管理员账号登录:
     - 用户名: `admin`
     - 密码: `MakeDotnetGreatAgain`
   - 登录成功后，自动重定向到授权端点

4. **同意页面**
   - 应该看到授权同意页面，显示:
     - 应用名称: "前端示例客户端"
     - 请求的权限列表（scopes）
     - "记住我的选择"复选框
   - 查看请求的权限:
     - ✓ openid - 您的基本身份标识
     - ✓ profile - 您的基本个人信息
     - ✓ email - 您的电子邮箱地址
     - ✓ offline_access - 在您离线时访问您的数据

5. **授予权限**
   - **临时授权**: 不勾选"记住我的选择"，点击"✓ 允许访问"
     - 授权有效期30天
   - **永久授权**: 勾选"记住我的选择"，点击"✓ 允许访问"
     - 授权永久有效，除非用户手动撤销

6. **重定向回应用**
   - 授权成功后，重定向回前端应用
   - URL包含授权码: `http://localhost:4201?code=xxx&state=xxx`
   - angular-auth-oidc-client 自动处理回调：
     - 提取授权码
     - 发送请求到 `/connect/token` 获取访问令牌
     - 保存访问令牌和刷新令牌

7. **查看用户信息**
   - 应该看到受保护页面显示用户信息:
     - 姓名
     - 邮箱
     - 用户ID (sub)
   - 查看访问令牌详细信息（展开面板）

8. **测试API调用**
   - 点击"调用受保护API"按钮
   - HTTP拦截器自动添加 `Authorization: Bearer {access_token}` 头
   - 应该返回成功响应，包含用户信息和API数据

**预期结果**:
- ✅ 完整的OAuth 2.0授权码流程 + PKCE
- ✅ 同意记录保存到数据库
- ✅ 访问令牌和刷新令牌正确获取
- ✅ API调用成功，token验证通过

---

### 场景2: 已有同意记录（跳过同意页面）

**目标**: 测试用户已经授予权限后，再次登录时跳过同意页面

**前提**: 在场景1中已经授予永久授权（勾选了"记住我的选择"）

**步骤**:

1. **登出**
   - 点击导航栏的"登出"按钮
   - 确认已登出

2. **再次访问受保护页面**
   - 点击"受保护页面"
   - 重定向到IAM登录页面

3. **登录**
   - 使用相同账号登录

4. **自动跳过同意**
   - ✅ **不应该**看到同意页面
   - 直接重定向回应用，并获取授权码
   - 这是因为数据库中已有有效的同意记录

5. **验证授权**
   - 应该直接进入受保护页面
   - 访问令牌正常工作

**预期结果**:
- ✅ 跳过同意页面
- ✅ 授权流程更流畅
- ✅ 数据库同意记录被正确检查和使用

---

### 场景3: 查看和撤销授权

**目标**: 测试用户查看已授权应用并撤销授权

**步骤**:

1. **确保已登录**
   - 如未登录，先完成场景1或场景2

2. **访问授权管理页面**
   - 点击侧边栏的"授权管理"链接
   - 应该看到授权列表页面

3. **查看授权记录**
   - 应该显示至少一条授权记录:
     - 应用名称: "前端示例客户端"
     - 客户端ID: FrontClient
     - 授权范围: openid, profile, email, offline_access
     - 授权类型: 永久 / 临时
     - 状态: 有效
     - 授权时间
     - 过期时间（如果是临时授权）

4. **撤销授权**
   - 点击某个授权记录的"撤销授权"按钮
   - 确认撤销
   - 应该显示成功消息: "授权已撤销"
   - 授权记录状态变为"已撤销"

5. **验证撤销效果**
   - 登出应用
   - 再次尝试访问受保护页面
   - 重新登录后，**应该再次看到同意页面**
   - 这是因为之前的授权已被撤销

**预期结果**:
- ✅ 能够查看所有授权记录
- ✅ 能够成功撤销授权
- ✅ 撤销后需要重新授权

---

### 场景4: 临时授权过期

**目标**: 测试临时授权（30天）的过期处理

**注意**: 此场景需要修改数据库数据或等待30天

**手动测试步骤**:

1. **创建临时授权**
   - 完成场景1，但不勾选"记住我的选择"
   - 这会创建一个30天有效期的授权

2. **模拟过期** (开发测试)
   - 直接修改数据库中 `Authorizations` 表的记录
   - 将 `ExpirationDate` 设置为过去的时间

3. **重新登录**
   - 登出并重新登录
   - 应该再次看到同意页面
   - 因为授权已过期

**预期结果**:
- ✅ 过期的临时授权不再有效
- ✅ 需要重新授权

---

### 场景5: Token验证和API保护

**目标**: 测试后端API正确验证访问令牌

**步骤**:

1. **确保已登录并有有效token**

2. **调用公开API** (不需要token)
   ```bash
   curl https://localhost:7000/api/public
   ```
   - 应该返回成功响应
   - 不需要Authorization头

3. **调用受保护API** (需要token)
   ```bash
   # 没有token - 应该返回401
   curl https://localhost:7000/api/protected
   
   # 使用token - 应该返回200
   curl -H "Authorization: Bearer {your_access_token}" https://localhost:7000/api/protected
   ```

4. **在前端测试**
   - 在受保护页面点击"调用受保护API"
   - 检查Network面板，确认:
     - ✅ 请求包含 `Authorization: Bearer ...` 头
     - ✅ Token自动由拦截器添加
     - ✅ 返回200状态码和用户数据

5. **测试无效token**
   - 使用过期或无效的token
   - 应该返回401 Unauthorized

**预期结果**:
- ✅ 公开端点无需认证可访问
- ✅ 受保护端点需要有效token
- ✅ Token验证正确（签名、过期时间、audience）
- ✅ HTTP拦截器自动添加token

---

### 场景6: 刷新Token

**目标**: 测试访问令牌过期时自动刷新

**配置**: 在 `app.config.ts` 中已启用:
```typescript
silentRenew: true,
useRefreshToken: true
```

**测试步骤**:

1. **获取初始访问令牌**
   - 完成登录流程
   - 访问令牌默认有效期15分钟

2. **等待令牌接近过期**
   - angular-auth-oidc-client 会在令牌过期前自动刷新
   - 或手动等待15分钟

3. **观察自动刷新**
   - 打开浏览器开发者工具 Network面板
   - 应该看到对 `/connect/token` 的POST请求
   - 请求参数包含:
     ```
     grant_type=refresh_token
     refresh_token=xxx
     ```

4. **验证新token**
   - 刷新后应该获得新的访问令牌
   - 应用继续正常工作，无需重新登录
   - 调用API仍然成功

**预期结果**:
- ✅ Token自动刷新
- ✅ 用户体验无中断
- ✅ 新token正确工作

---

### 场景7: 拒绝授权

**目标**: 测试用户拒绝授权的情况

**步骤**:

1. **触发授权流程**
   - 确保已登出
   - 访问受保护页面
   - 登录

2. **在同意页面点击"拒绝"**
   - 应该重定向回应用
   - URL包含错误参数: `?error=access_denied&error_description=...`

3. **验证错误处理**
   - 应用应该显示错误消息或未授权页面
   - 用户仍然未登录
   - 不应该获得访问令牌

**预期结果**:
- ✅ 正确处理授权拒绝
- ✅ 返回适当的错误信息
- ✅ 不泄露敏感信息

---

## 数据库验证

在测试过程中，可以查看数据库来验证数据正确性：

### 查看授权记录

```sql
-- 查看所有授权记录
SELECT 
    a.Id,
    a.SubjectId,
    c.ClientId,
    c.DisplayName,
    a.Type,
    a.Status,
    a.Scopes,
    a.CreationDate,
    a.ExpirationDate
FROM Authorizations a
INNER JOIN Clients c ON a.ClientId = c.Id
WHERE a.Type IN ('permanent', 'ad_hoc')
ORDER BY a.CreationDate DESC;
```

### 查看Token记录

```sql
-- 查看token记录
SELECT 
    t.Id,
    t.Type,
    t.Status,
    t.SubjectId,
    t.CreationDate,
    t.ExpirationDate,
    t.RedemptionDate
FROM Tokens t
WHERE t.SubjectId = 'xxx'  -- 替换为实际用户ID
ORDER BY t.CreationDate DESC;
```

---

## 常见问题排查

### 问题1: 重定向URI不匹配

**错误**: `invalid_request: redirect_uri does not match`

**解决**:
- 检查IAM中客户端配置的重定向URI
- 确保包含 `http://localhost:4201` 和 `http://localhost:4201/`

### 问题2: CORS错误

**错误**: `CORS policy: No 'Access-Control-Allow-Origin' header`

**解决**:
- 检查后端API的CORS配置
- 确保 `appsettings.Development.json` 包含前端URL
- 重启后端服务

### 问题3: Token验证失败

**错误**: `401 Unauthorized` 当调用受保护API

**解决**:
- 检查token是否包含在请求头中
- 验证后端的 `Authority` 配置指向正确的IAM服务器
- 检查 `Audience` 配置是否正确
- 确认token未过期

### 问题4: 同意页面不显示

**可能原因**:
1. 数据库中已有有效的同意记录 - 这是正常的！
2. 检查 `Authorizations` 表确认

**解决**: 如需重新看到同意页面，撤销现有授权

### 问题5: PKCE验证失败

**错误**: `invalid_grant: PKCE validation failed`

**解决**:
- 确认客户端配置中启用了PKCE
- 检查 `code_challenge` 和 `code_verifier` 是否正确传递
- 查看浏览器控制台和网络请求日志

---

## 安全性检查清单

测试时请验证以下安全措施：

- [ ] 客户端密钥（如有）不在前端暴露
- [ ] 访问令牌在15分钟后过期
- [ ] PKCE code_challenge 使用 S256 方法
- [ ] 授权码只能使用一次
- [ ] 同意记录正确关联用户和客户端
- [ ] 撤销的授权不能继续使用
- [ ] 过期的授权不能继续使用
- [ ] Token在浏览器中安全存储（session storage）
- [ ] HTTPS用于所有敏感通信（生产环境）

---

## 性能监控

建议在测试时监控：

1. **Aspire Dashboard**
   - 查看所有服务的健康状态
   - 监控请求追踪
   - 查看日志输出

2. **浏览器开发者工具**
   - Network面板：查看所有HTTP请求
   - Console面板：查看OIDC库的调试日志
   - Application面板：查看存储的token

3. **数据库查询**
   - 验证授权记录正确保存
   - 检查token状态更新

---

## 总结

完成所有测试场景后，您应该验证了：

✅ **完整的授权流程**: 登录 → 同意 → 获取code → 换取token → 调用API
✅ **同意管理**: 首次授权、跳过同意、查看授权、撤销授权
✅ **临时vs永久授权**: 两种授权类型的不同行为
✅ **Token管理**: 自动刷新、过期处理
✅ **API保护**: 后端正确验证token
✅ **安全性**: PKCE、授权码一次性使用、同意记录检查

如有任何问题，请查看：
- Aspire Dashboard 日志
- 浏览器控制台日志
- 数据库授权记录
- 本文档的"常见问题排查"部分
