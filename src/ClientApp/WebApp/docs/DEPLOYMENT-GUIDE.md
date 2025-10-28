# IAM 前端部署指南

## 目录

1. [环境要求](#环境要求)
2. [开发环境部署](#开发环境部署)
3. [生产环境部署](#生产环境部署)
4. [Docker 部署](#docker-部署)
5. [Nginx 配置](#nginx-配置)
6. [环境变量配置](#环境变量配置)
7. [性能优化](#性能优化)
8. [故障排查](#故障排查)

## 环境要求

### 硬件要求

**最低配置**：
- CPU: 1 核
- 内存: 512 MB
- 磁盘: 1 GB

**推荐配置**：
- CPU: 2 核
- 内存: 2 GB
- 磁盘: 5 GB

### 软件要求

**开发环境**：
- Node.js: v20.x 或更高
- pnpm: v9.14.2 或更高
- Angular CLI: v20.x

**生产环境**：
- Nginx: v1.24+ 或其他 Web 服务器
- Node.js: v20.x（用于构建）

### 浏览器支持

- Chrome 90+
- Firefox 88+
- Safari 14+
- Edge 90+

## 开发环境部署

### 1. 克隆代码

```bash
git clone https://github.com/AterDev/IAM.git
cd IAM/src/ClientApp/WebApp
```

### 2. 安装依赖

```bash
# 安装 pnpm（如果未安装）
npm install -g pnpm@9.14.2

# 安装项目依赖
pnpm install
```

### 3. 配置环境

创建或修改环境配置文件 `src/environments/environment.development.ts`：

```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5000',  // 后端 API 地址
  clientId: 'admin-portal',
  authority: 'http://localhost:5000',
  redirectUri: 'http://localhost:4200/callback',
  scope: 'openid profile email',
};
```

### 4. 启动开发服务器

```bash
pnpm start
```

访问 `http://localhost:4200`

### 5. 热重载

开发服务器支持热重载，修改代码后浏览器会自动刷新。

## 生产环境部署

### 1. 配置生产环境变量

编辑 `src/environments/environment.ts`：

```typescript
export const environment = {
  production: true,
  apiUrl: 'https://api.example.com',  // 生产环境 API 地址
  clientId: 'admin-portal',
  authority: 'https://auth.example.com',
  redirectUri: 'https://admin.example.com/callback',
  scope: 'openid profile email',
};
```

### 2. 构建生产版本

```bash
pnpm run build
```

构建产物位于 `dist/` 目录。

### 3. 构建优化

**启用 AOT 编译**（默认已启用）：
```bash
pnpm run build --configuration production
```

**分析包大小**：
```bash
pnpm run build -- --stats-json
npx webpack-bundle-analyzer dist/stats.json
```

### 4. 部署到 Web 服务器

#### 方式 1: 直接部署

```bash
# 将 dist 目录复制到 Web 服务器根目录
cp -r dist/* /var/www/html/
```

#### 方式 2: 使用 CI/CD

创建 `.github/workflows/deploy.yml`：

```yaml
name: Deploy Frontend

on:
  push:
    branches: [main]

jobs:
  build:
    runs-on: ubuntu-latest
    
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup Node.js
        uses: actions/setup-node@v3
        with:
          node-version: '20'
      
      - name: Install pnpm
        run: npm install -g pnpm@9.14.2
      
      - name: Install dependencies
        run: |
          cd src/ClientApp/WebApp
          pnpm install
      
      - name: Build
        run: |
          cd src/ClientApp/WebApp
          pnpm run build
      
      - name: Deploy to Server
        uses: appleboy/scp-action@v0.1.4
        with:
          host: ${{ secrets.SERVER_HOST }}
          username: ${{ secrets.SERVER_USER }}
          key: ${{ secrets.SSH_KEY }}
          source: "src/ClientApp/WebApp/dist/*"
          target: "/var/www/html/"
```

## Docker 部署

### 1. 创建 Dockerfile

在 `src/ClientApp/WebApp/` 目录创建 `Dockerfile`：

```dockerfile
# 构建阶段
FROM node:20-alpine AS builder

WORKDIR /app

# 安装 pnpm
RUN npm install -g pnpm@9.14.2

# 复制依赖配置文件
COPY package.json pnpm-lock.yaml ./

# 安装依赖
RUN pnpm install --frozen-lockfile

# 复制源代码
COPY . .

# 构建应用
RUN pnpm run build

# 生产阶段
FROM nginx:1.25-alpine

# 复制构建产物
COPY --from=builder /app/dist /usr/share/nginx/html

# 复制 nginx 配置
COPY nginx.conf /etc/nginx/conf.d/default.conf

# 暴露端口
EXPOSE 80

# 启动 nginx
CMD ["nginx", "-g", "daemon off;"]
```

### 2. 创建 nginx.conf

```nginx
server {
    listen 80;
    server_name _;
    root /usr/share/nginx/html;
    index index.html;

    location / {
        try_files $uri $uri/ /index.html;
    }

    # API 代理
    location /api/ {
        proxy_pass http://backend:5000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # 启用 gzip 压缩
    gzip on;
    gzip_vary on;
    gzip_min_length 1024;
    gzip_types text/plain text/css text/xml text/javascript application/x-javascript application/xml+rss application/json;

    # 缓存静态资源
    location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg|woff|woff2|ttf|eot)$ {
        expires 1y;
        add_header Cache-Control "public, immutable";
    }
}
```

### 3. 构建 Docker 镜像

```bash
cd src/ClientApp/WebApp
docker build -t iam-frontend:latest .
```

### 4. 运行 Docker 容器

```bash
docker run -d \
  --name iam-frontend \
  -p 80:80 \
  iam-frontend:latest
```

### 5. Docker Compose 部署

创建 `docker-compose.yml`：

```yaml
version: '3.8'

services:
  frontend:
    build: ./src/ClientApp/WebApp
    ports:
      - "80:80"
    environment:
      - API_URL=http://backend:5000
    depends_on:
      - backend
    restart: unless-stopped

  backend:
    image: iam-backend:latest
    ports:
      - "5000:5000"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
    restart: unless-stopped
```

启动：
```bash
docker-compose up -d
```

## Nginx 配置

### 基础配置

```nginx
server {
    listen 80;
    server_name admin.example.com;
    
    # 重定向到 HTTPS
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    server_name admin.example.com;
    
    # SSL 证书
    ssl_certificate /etc/ssl/certs/example.com.crt;
    ssl_certificate_key /etc/ssl/private/example.com.key;
    
    # SSL 配置
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;
    ssl_prefer_server_ciphers on;
    
    root /var/www/html/iam-frontend;
    index index.html;
    
    # Angular 路由支持
    location / {
        try_files $uri $uri/ /index.html;
    }
    
    # API 反向代理
    location /api/ {
        proxy_pass https://api.example.com;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
    
    # 静态资源缓存
    location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg|woff|woff2|ttf|eot)$ {
        expires 1y;
        add_header Cache-Control "public, immutable";
        access_log off;
    }
    
    # 启用 gzip 压缩
    gzip on;
    gzip_vary on;
    gzip_comp_level 6;
    gzip_min_length 1024;
    gzip_types
        text/plain
        text/css
        text/xml
        text/javascript
        application/javascript
        application/x-javascript
        application/json
        application/xml
        application/xml+rss;
    
    # 安全头
    add_header X-Frame-Options "SAMEORIGIN" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-XSS-Protection "1; mode=block" always;
    add_header Referrer-Policy "no-referrer-when-downgrade" always;
    add_header Content-Security-Policy "default-src 'self' http: https: data: blob: 'unsafe-inline'" always;
}
```

### 重启 Nginx

```bash
# 测试配置
sudo nginx -t

# 重启服务
sudo systemctl restart nginx
```

## 环境变量配置

### 多环境支持

创建不同的环境配置文件：

- `environment.ts` - 生产环境
- `environment.development.ts` - 开发环境
- `environment.staging.ts` - 预发环境

### 运行时环境变量

使用 `assets/config.json` 支持运行时配置：

```json
{
  "apiUrl": "https://api.example.com",
  "clientId": "admin-portal",
  "authority": "https://auth.example.com",
  "logLevel": "warn"
}
```

在应用启动时加载：

```typescript
// app.config.ts
export function loadConfig(): Promise<any> {
  return fetch('/assets/config.json')
    .then(response => response.json());
}
```

## 性能优化

### 1. 代码分割

Angular 默认支持懒加载路由，确保正确配置：

```typescript
const routes: Routes = [
  {
    path: 'users',
    loadComponent: () => import('./pages/user/user-list')
      .then(m => m.UserListComponent)
  }
];
```

### 2. 预加载策略

```typescript
// app.config.ts
import { PreloadAllModules } from '@angular/router';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes, withPreloading(PreloadAllModules))
  ]
};
```

### 3. 生产构建优化

```bash
# 启用 AOT、压缩、Tree Shaking
pnpm run build --configuration production

# 分析包大小
pnpm run build -- --stats-json
```

### 4. CDN 加速

将静态资源上传到 CDN：

```typescript
// angular.json
{
  "deployUrl": "https://cdn.example.com/iam-frontend/"
}
```

### 5. Service Worker

启用 PWA 支持：

```bash
ng add @angular/pwa
```

## 故障排查

### 1. 构建失败

**问题**：内存不足
```bash
# 增加 Node.js 内存限制
export NODE_OPTIONS=--max_old_space_size=4096
pnpm run build
```

### 2. 路由 404 错误

确保 Web 服务器配置了 URL 重写：

```nginx
location / {
    try_files $uri $uri/ /index.html;
}
```

### 3. API 跨域问题

开发环境使用代理：

```json
// proxy.conf.json
{
  "/api": {
    "target": "http://localhost:5000",
    "secure": false,
    "changeOrigin": true
  }
}
```

### 4. 性能问题

**检查清单**：
- 启用 gzip 压缩
- 配置浏览器缓存
- 使用 CDN
- 启用 HTTP/2
- 优化图片大小

### 5. 查看日志

**Nginx 日志**：
```bash
sudo tail -f /var/log/nginx/access.log
sudo tail -f /var/log/nginx/error.log
```

**浏览器控制台**：
- 打开开发者工具 (F12)
- 查看 Console 和 Network 标签

## 监控与维护

### 1. 健康检查

创建健康检查端点 `health.html`：

```html
<!DOCTYPE html>
<html>
<body>OK</body>
</html>
```

配置监控：
```bash
curl https://admin.example.com/health.html
```

### 2. 日志监控

使用工具如：
- ELK Stack
- Grafana + Loki
- Sentry（前端错误追踪）

### 3. 性能监控

集成 Google Analytics 或其他分析工具。

### 4. 定期更新

```bash
# 检查依赖更新
pnpm outdated

# 更新依赖
pnpm update

# 安全审计
pnpm audit
```

## 安全建议

1. **HTTPS**：生产环境必须使用 HTTPS
2. **安全头**：配置适当的安全响应头
3. **CSP**：配置内容安全策略
4. **依赖审计**：定期运行 `pnpm audit`
5. **最小权限**：Web 服务器使用非 root 用户运行

## 回滚策略

### 1. 备份当前版本

```bash
cp -r /var/www/html/iam-frontend /var/www/html/iam-frontend.backup
```

### 2. 快速回滚

```bash
rm -rf /var/www/html/iam-frontend
mv /var/www/html/iam-frontend.backup /var/www/html/iam-frontend
sudo systemctl restart nginx
```

### 3. 版本控制

使用版本号管理部署：

```bash
/var/www/html/
  ├── iam-frontend-v1.0.0/
  ├── iam-frontend-v1.0.1/
  └── iam-frontend -> iam-frontend-v1.0.1/  # 符号链接
```

切换版本：
```bash
ln -sfn /var/www/html/iam-frontend-v1.0.0 /var/www/html/iam-frontend
sudo systemctl restart nginx
```

## 技术支持

如需帮助，请联系：

- **文档**：https://github.com/AterDev/IAM/tree/main/docs
- **问题追踪**：https://github.com/AterDev/IAM/issues
- **邮箱**：support@example.com

---

**版本**：v1.0  
**更新日期**：2025-10-28  
**维护者**：IAM 开发团队
