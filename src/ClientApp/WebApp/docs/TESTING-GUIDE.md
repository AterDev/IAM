# IAM 前端测试指南

## 目录

1. [测试概述](#测试概述)
2. [单元测试](#单元测试)
3. [端到端测试](#端到端测试)
4. [测试覆盖率](#测试覆盖率)
5. [持续集成](#持续集成)
6. [最佳实践](#最佳实践)

## 测试概述

本项目使用以下测试框架和工具：

- **Jest**: 单元测试框架
- **Playwright**: 端到端测试框架
- **@angular/core/testing**: Angular 测试工具
- **jest-preset-angular**: Jest 的 Angular 预设

### 测试金字塔

```
       /\
      /  \     E2E Tests (Playwright)
     /----\
    /      \   Integration Tests
   /--------\
  /          \ Unit Tests (Jest)
 /____________\
```

## 单元测试

### 运行单元测试

```bash
# 运行所有测试
pnpm test

# 监视模式（开发时使用）
pnpm test:watch

# 生成覆盖率报告
pnpm test:coverage
```

### 测试文件结构

```
src/app/
├── services/
│   ├── auth.service.ts
│   └── auth.service.spec.ts
├── guards/
│   ├── auth.guard.ts
│   └── auth.guard.spec.ts
└── pages/
    ├── login/
    │   ├── login.ts
    │   └── login.spec.ts
    └── user/
        ├── user-list.ts
        └── user-list.spec.ts
```

### 编写服务测试

#### 示例：测试 HTTP 服务

```typescript
import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { UsersService } from './users.service';

describe('UsersService', () => {
  let service: UsersService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [UsersService]
    });
    
    service = TestBed.inject(UsersService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    // 验证没有未处理的请求
    httpMock.verify();
  });

  it('should fetch users', (done) => {
    const mockUsers = [
      { id: '1', userName: 'user1' },
      { id: '2', userName: 'user2' }
    ];

    service.getUsers(null, null, null, null, null, null, 1, 10, null)
      .subscribe(response => {
        expect(response.data).toEqual(mockUsers);
        done();
      });

    const req = httpMock.expectOne(req => req.url.includes('/api/Users'));
    expect(req.request.method).toBe('GET');
    req.flush({ data: mockUsers, total: 2 });
  });

  it('should handle error', (done) => {
    service.getUsers(null, null, null, null, null, null, 1, 10, null)
      .subscribe(
        () => fail('should have failed'),
        (error) => {
          expect(error.status).toBe(500);
          done();
        }
      );

    const req = httpMock.expectOne(req => req.url.includes('/api/Users'));
    req.flush('Server Error', { status: 500, statusText: 'Server Error' });
  });
});
```

### 编写组件测试

#### 示例：测试表单组件

```typescript
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { LoginComponent } from './login';
import { OidcAuthService } from '../../services/oidc-auth.service';

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  let authService: jasmine.SpyObj<OidcAuthService>;

  beforeEach(async () => {
    const authServiceSpy = jasmine.createSpyObj('OidcAuthService', ['login']);

    await TestBed.configureTestingModule({
      imports: [
        LoginComponent,
        ReactiveFormsModule,
        MatFormFieldModule,
        MatInputModule,
        NoopAnimationsModule
      ],
      providers: [
        { provide: OidcAuthService, useValue: authServiceSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    authService = TestBed.inject(OidcAuthService) as jasmine.SpyObj<OidcAuthService>;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should have invalid form when empty', () => {
    expect(component.loginForm.valid).toBeFalsy();
  });

  it('should validate username field', () => {
    const username = component.loginForm.get('username');
    expect(username?.valid).toBeFalsy();

    username?.setValue('testuser');
    expect(username?.valid).toBeTruthy();
  });

  it('should call auth service on submit', () => {
    component.loginForm.patchValue({
      username: 'testuser',
      password: 'password123'
    });

    authService.login.and.returnValue(Promise.resolve());
    component.onSubmit();

    expect(authService.login).toHaveBeenCalled();
  });
});
```

### 编写守卫测试

```typescript
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { AuthGuard } from './auth.guard';
import { OidcAuthService } from '../services/oidc-auth.service';

describe('AuthGuard', () => {
  let guard: AuthGuard;
  let authService: jasmine.SpyObj<OidcAuthService>;
  let router: jasmine.SpyObj<Router>;

  beforeEach(() => {
    const authServiceSpy = jasmine.createSpyObj('OidcAuthService', ['isAuthenticated']);
    const routerSpy = jasmine.createSpyObj('Router', ['parseUrl']);

    TestBed.configureTestingModule({
      providers: [
        AuthGuard,
        { provide: OidcAuthService, useValue: authServiceSpy },
        { provide: Router, useValue: routerSpy }
      ]
    });

    guard = TestBed.inject(AuthGuard);
    authService = TestBed.inject(OidcAuthService) as jasmine.SpyObj<OidcAuthService>;
    router = TestBed.inject(Router) as jasmine.SpyObj<Router>;
  });

  it('should allow authenticated users', () => {
    authService.isAuthenticated.and.returnValue(true);
    
    const result = guard.canActivate({} as any, { url: '/dashboard' } as any);
    
    expect(result).toBe(true);
  });

  it('should redirect unauthenticated users', () => {
    authService.isAuthenticated.and.returnValue(false);
    const loginUrl = {} as any;
    router.parseUrl.and.returnValue(loginUrl);
    
    const result = guard.canActivate({} as any, { url: '/dashboard' } as any);
    
    expect(result).toBe(loginUrl);
    expect(router.parseUrl).toHaveBeenCalledWith('/login');
  });
});
```

### Mock 数据

创建 `src/app/testing/mock-data.ts`：

```typescript
export const mockUsers = [
  {
    id: '1',
    userName: 'admin',
    email: 'admin@example.com',
    roles: ['Administrator']
  },
  {
    id: '2',
    userName: 'user1',
    email: 'user1@example.com',
    roles: ['User']
  }
];

export const mockRoles = [
  {
    id: '1',
    name: 'Administrator',
    description: 'System administrator'
  },
  {
    id: '2',
    name: 'User',
    description: 'Regular user'
  }
];
```

## 端到端测试

### 运行 E2E 测试

```bash
# 运行所有 E2E 测试
pnpm e2e

# 以 UI 模式运行（推荐用于调试）
pnpm e2e:ui

# 以有头模式运行（可见浏览器）
pnpm e2e:headed

# 查看测试报告
pnpm e2e:report

# 运行特定测试文件
pnpm e2e auth.spec.ts

# 运行特定浏览器
pnpm e2e --project=chromium
```

### E2E 测试结构

```
e2e/
├── auth.spec.ts              # 认证流程测试
├── user-management.spec.ts   # 用户管理测试
├── role-management.spec.ts   # 角色管理测试
├── client-management.spec.ts # 客户端管理测试
└── fixtures/                 # 测试固件
    ├── users.json
    └── roles.json
```

### 编写 E2E 测试

#### 基础测试

```typescript
import { test, expect } from '@playwright/test';

test.describe('Login Page', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/login');
  });

  test('should display login form', async ({ page }) => {
    await expect(page.locator('h1, h2').filter({ hasText: /登录|Login/i }))
      .toBeVisible();
    
    await expect(page.locator('input[name="username"]'))
      .toBeVisible();
    
    await expect(page.locator('input[name="password"]'))
      .toBeVisible();
  });

  test('should show validation errors', async ({ page }) => {
    await page.click('button[type="submit"]');
    
    await expect(page.locator('mat-error'))
      .toBeVisible();
  });
});
```

#### 使用 Page Object Model

创建 `e2e/pages/login.page.ts`：

```typescript
import { Page } from '@playwright/test';

export class LoginPage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/login');
  }

  async login(username: string, password: string) {
    await this.page.fill('input[name="username"]', username);
    await this.page.fill('input[name="password"]', password);
    await this.page.click('button[type="submit"]');
  }

  async getErrorMessage() {
    return await this.page.locator('mat-error').textContent();
  }
}
```

使用 Page Object：

```typescript
import { test, expect } from '@playwright/test';
import { LoginPage } from './pages/login.page';

test('should login successfully', async ({ page }) => {
  const loginPage = new LoginPage(page);
  
  await loginPage.goto();
  await loginPage.login('admin', 'password123');
  
  await expect(page).toHaveURL(/\/dashboard/);
});
```

#### 测试 Fixtures

创建 `e2e/fixtures/auth.fixture.ts`：

```typescript
import { test as base } from '@playwright/test';
import { LoginPage } from '../pages/login.page';

export const test = base.extend({
  authenticatedPage: async ({ page }, use) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'password123');
    
    await use(page);
  }
});
```

使用 Fixture：

```typescript
import { test, expect } from './fixtures/auth.fixture';

test('should access protected route', async ({ authenticatedPage }) => {
  await authenticatedPage.goto('/users');
  await expect(authenticatedPage).toHaveURL(/\/users/);
});
```

### 截图和视频

在 `playwright.config.ts` 中配置：

```typescript
export default defineConfig({
  use: {
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
    trace: 'on-first-retry',
  },
});
```

## 测试覆盖率

### 查看覆盖率报告

```bash
pnpm test:coverage
```

报告生成在 `coverage/` 目录，打开 `coverage/lcov-report/index.html` 查看详细报告。

### 覆盖率目标

- **语句覆盖率**: ≥ 80%
- **分支覆盖率**: ≥ 75%
- **函数覆盖率**: ≥ 80%
- **行覆盖率**: ≥ 80%

### 配置覆盖率阈值

在 `jest.config.js` 中：

```javascript
module.exports = {
  coverageThreshold: {
    global: {
      branches: 75,
      functions: 80,
      lines: 80,
      statements: 80
    }
  }
};
```

## 持续集成

### GitHub Actions 配置

创建 `.github/workflows/test.yml`：

```yaml
name: Frontend Tests

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main, develop]

jobs:
  unit-tests:
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
      
      - name: Run unit tests
        run: |
          cd src/ClientApp/WebApp
          pnpm test:coverage
      
      - name: Upload coverage to Codecov
        uses: codecov/codecov-action@v3
        with:
          directory: ./src/ClientApp/WebApp/coverage

  e2e-tests:
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
      
      - name: Install Playwright browsers
        run: |
          cd src/ClientApp/WebApp
          npx playwright install --with-deps
      
      - name: Run E2E tests
        run: |
          cd src/ClientApp/WebApp
          pnpm e2e
      
      - name: Upload test results
        if: always()
        uses: actions/upload-artifact@v3
        with:
          name: playwright-report
          path: src/ClientApp/WebApp/playwright-report/
```

## 最佳实践

### 1. 测试命名

使用描述性的测试名称：

```typescript
// ❌ 不好
it('test 1', () => {});

// ✅ 好
it('should display error message when login fails', () => {});
```

### 2. AAA 模式

遵循 Arrange-Act-Assert 模式：

```typescript
it('should create user', () => {
  // Arrange - 准备测试数据
  const userData = { username: 'test', email: 'test@example.com' };
  
  // Act - 执行操作
  const result = service.createUser(userData);
  
  // Assert - 验证结果
  expect(result).toBeDefined();
  expect(result.username).toBe('test');
});
```

### 3. 避免测试实现细节

```typescript
// ❌ 测试实现细节
it('should call private method', () => {
  expect(component['privateMethod']).toHaveBeenCalled();
});

// ✅ 测试公共接口
it('should update user list after creation', () => {
  component.createUser(userData);
  expect(component.users.length).toBe(1);
});
```

### 4. 使用 beforeEach 清理

```typescript
describe('UserService', () => {
  let service: UserService;
  
  beforeEach(() => {
    service = new UserService();
  });
  
  afterEach(() => {
    // 清理
    service = null;
  });
});
```

### 5. 异步测试

```typescript
// 使用 done 回调
it('should load data', (done) => {
  service.getData().subscribe(data => {
    expect(data).toBeDefined();
    done();
  });
});

// 使用 async/await
it('should load data', async () => {
  const data = await service.getData().toPromise();
  expect(data).toBeDefined();
});
```

### 6. 隔离测试

每个测试应该独立，不依赖其他测试：

```typescript
// ❌ 测试相互依赖
describe('Counter', () => {
  let count = 0;
  
  it('increment', () => {
    count++;
    expect(count).toBe(1);
  });
  
  it('increment again', () => {
    count++;
    expect(count).toBe(2); // 依赖前一个测试
  });
});

// ✅ 测试独立
describe('Counter', () => {
  let counter: Counter;
  
  beforeEach(() => {
    counter = new Counter();
  });
  
  it('increment', () => {
    counter.increment();
    expect(counter.value).toBe(1);
  });
  
  it('decrement', () => {
    counter.decrement();
    expect(counter.value).toBe(-1);
  });
});
```

### 7. 测试边界情况

```typescript
describe('UserValidator', () => {
  it('should validate email format', () => {
    expect(validator.isValidEmail('test@example.com')).toBe(true);
    expect(validator.isValidEmail('invalid')).toBe(false);
    expect(validator.isValidEmail('')).toBe(false);
    expect(validator.isValidEmail(null)).toBe(false);
  });
});
```

## 调试技巧

### 1. 使用 fit 和 fdescribe

运行单个测试：

```typescript
fit('should focus on this test', () => {
  // 只运行这个测试
});

fdescribe('focused suite', () => {
  // 只运行这个测试套件
});
```

### 2. Playwright 调试

```bash
# 调试模式
pnpm e2e --debug

# 在特定点暂停
await page.pause();
```

### 3. 查看测试输出

```typescript
it('debug test', () => {
  console.log('Debug info:', component.data);
  expect(component.data).toBeDefined();
});
```

## 常见问题

### Q1: 测试中如何 mock HttpClient？

使用 `HttpClientTestingModule`：

```typescript
TestBed.configureTestingModule({
  imports: [HttpClientTestingModule]
});
```

### Q2: 如何测试路由导航？

使用 `Router` 的 spy：

```typescript
const routerSpy = jasmine.createSpyObj('Router', ['navigate']);
await component.navigateToUser();
expect(routerSpy.navigate).toHaveBeenCalledWith(['/users', userId]);
```

### Q3: E2E 测试如何处理认证？

使用 fixture 或在 beforeEach 中登录。

---

**版本**：v1.0  
**更新日期**：2025-10-28  
**维护者**：IAM 测试团队
