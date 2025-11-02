You are an expert in TypeScript, Angular, and scalable IAM (Identity & Access Management) web application development. You write maintainable, performant, and accessible code following Angular and TypeScript best practices.

## IAM Application Architecture
This is an **OIDC/OAuth2 IAM admin portal** with PKCE authentication, user/role/client management, and multi-tenant organization support.

### Key Services & Patterns
- **ApiClient** (`src/app/services/api/api-client.ts`): Unified service aggregator - ALWAYS use `inject(ApiClient)` and access via `this.api.users`, `this.api.clients`, etc. NEVER import individual services
- **AuthService**: Authentication service with localStorage-based state management
- **CustomerHttpInterceptor**: Handles 401 redirects, shows Chinese error messages via MatSnackBar
- **AuthGuard**: Protects routes, uses `AuthService.isAuthenticated()`

### Component Patterns
- **CRUD Structure**: Each entity has `/add`, `/edit`, `/list`, `/detail` folders with standalone components
- **Shared Modules**: Import from `CommonModules`, `BaseMatModules`, `CommonFormModules` in `shared-modules.ts`
- **Signal Usage**: Use signals ONLY for reactive template data. Regular properties for page size, search text, etc.

```typescript
// ✅ Correct signal usage
dataSource = signal<UserItemDto[]>([]);
isLoading = signal(false);
// ✅ Regular properties for non-reactive values  
pageSize = 10;
searchText = '';
```

### Development Workflow
- **I18n Keys**: Run `pnpm i18n:keys` to generate `i18n-keys.ts` from `assets/i18n/zh.json`
- **Testing**: Jest unit tests, Playwright E2E tests
- **Development**: `pnpm start` (auto-generates i18n keys)

## Angular Best Practices
- Always use standalone components over NgModules
- Must NOT set `standalone: true` inside Angular decorators. It's the default.
- Use signals for state management
- Implement lazy loading for feature routes
- Do NOT use the `@HostBinding` and `@HostListener` decorators. Put host bindings inside the `host` object of the `@Component` or `@Directive` decorator instead
- Use `NgOptimizedImage` for all static images.
  - `NgOptimizedImage` does not work for inline base64 images.

## Components
- Keep components small and focused on a single responsibility
- Use `input()` and `output()` functions instead of decorators
- Use `computed()` for derived state
- Prefer inline templates for small components
- Prefer Reactive forms instead of Template-driven ones
- Do NOT use `ngClass`, use `class` bindings instead
- DO NOT use `ngStyle`, use `style` bindings instead

## State Management
- Use signals for local component state
- Use `computed()` for derived state
- Keep state transformations pure and predictable
- Do NOT use `mutate` on signals, use `update` or `set` instead

## Templates
- Keep templates simple and avoid complex logic
- Use native control flow (`@if`, `@for`, `@switch`) instead of `*ngIf`, `*ngFor`, `*ngSwitch`
- Use the async pipe to handle observables

## Services
- Design services around a single responsibility
- Use the `providedIn: 'root'` option for singleton services
- Use the `inject()` function instead of constructor injection

## Angular Docs
- when getting Angular documentation detail, use #fetch from  https://angular.dev/llms.txt to get Angular documentation
