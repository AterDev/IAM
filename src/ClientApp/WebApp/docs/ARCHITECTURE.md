# Admin Portal Architecture

This document describes the Admin Portal frontend architecture and module organization.

## Module Structure

The application follows Angular best practices with the following module organization:

### Core Module (`src/app/core/`)

The CoreModule contains singleton services and app-level providers that should only be loaded once.

**Key Components:**
- `core.module.ts` - Core module definition with singleton guard
- `interceptors/auth-http.interceptor.ts` - HTTP interceptor for authentication

**Features:**
- Access Token injection into HTTP requests
- Global error handling
- Automatic redirect to login on 401 errors
- Request/response transformations

**Usage:** The core module is imported once in the app configuration (`app.config.ts`).

### Shared Module (`src/app/shared/`)

The SharedModule contains reusable components, directives, pipes, and commonly used Angular Material modules.

**Key Components:**
- `shared.module.ts` - Shared module definition
- `components/breadcrumb/` - Breadcrumb navigation component

**Exported Modules:**
- Angular Material UI components
- Forms modules (FormsModule, ReactiveFormsModule)
- Common Angular modules (CommonModule, RouterModule)
- Translation module (TranslateModule)

**Usage:** Import SharedModule in feature modules that need common functionality.

## Global Layout

The application uses a consistent layout structure with the following components:

### Layout Component (`src/app/layout/`)

The main layout wrapper that includes:
- **Header/Toolbar** - Top navigation bar with user menu and language selector
- **Navigation Sidebar** - Collapsible side menu with hierarchical navigation
- **Breadcrumbs** - Dynamic breadcrumb trail showing current location
- **Content Area** - Main content rendered via `<router-outlet>`

### Navigation Components

1. **NavigationComponent** - Sidebar navigation with menu items loaded from `assets/menus.json`
2. **BreadcrumbComponent** - Automatic breadcrumb generation from route data

## HTTP Interceptor

### AuthHttpInterceptor

The `AuthHttpInterceptor` provides:

**Token Injection:**
```typescript
Authorization: Bearer {accessToken}
```
- Automatically adds the Authorization header to all HTTP requests
- Excludes specific URLs (login, public assets, etc.)
- Retrieves token from localStorage

**Error Handling:**
- **401 Unauthorized** - Clears session and redirects to login
- **403 Forbidden** - Shows permission denied message
- **404 Not Found** - Shows resource not found message
- **409 Conflict** - Shows conflict message
- **500 Server Error** - Shows server error message
- Displays user-friendly error messages via MatSnackBar

## Routing

Routes are defined in `src/app/app.routes.ts` with:
- Lazy-loaded feature modules
- Route guards for authentication
- Breadcrumb metadata in route data

Example route configuration:
```typescript
{
  path: 'system-user',
  data: { breadcrumb: 'systemUser.title' },
  children: [
    { 
      path: 'index', 
      loadComponent: () => import('./pages/system-user/index/index').then(m => m.Index),
      data: { breadcrumb: 'systemUser.list' }
    },
  ]
}
```

## Authentication Flow

1. User logs in via `/login` page
2. Login successful - token stored in localStorage
3. `AuthService` updates login state
4. `AuthHttpInterceptor` adds token to subsequent requests
5. On 401 error - session cleared and redirected to login

## Development Guidelines

### Adding a New Feature Module

1. Create feature module structure
2. Import SharedModule for common functionality
3. Define routes with breadcrumb metadata
4. Add menu items to `assets/menus.json`

### Adding Global Services

1. Add service to `src/app/core/services/`
2. Register in `CoreModule` providers if needed
3. Use `providedIn: 'root'` for singleton services

### Adding Shared Components

1. Create component in `src/app/shared/components/`
2. Export from SharedModule if needed in multiple places
3. Keep components standalone for better tree-shaking

## Material Design Integration

Angular Material is fully integrated with:
- Material Design 3 theming
- Custom color palettes (violet primary, orange accent)
- Dark mode support via `prefers-color-scheme`
- Material Icons (outlined variant)

Theme configuration is in `src/theme.scss`.

## Internationalization (i18n)

The application supports multiple languages using `@ngx-translate`:
- Default language: Chinese (zh)
- Fallback language: Chinese (zh)
- Translation files: `src/assets/i18n/*.json`
- Auto-generated translation keys: `src/app/share/i18n-keys.ts`

## Build and Development

```bash
# Install dependencies
pnpm install

# Development server
pnpm start

# Build for production
pnpm build

# Build for development
pnpm ng build --configuration development
```

## Security Considerations

1. **Token Storage** - Access tokens stored in localStorage
2. **XSS Protection** - Angular's built-in sanitization
3. **CSRF** - Handled by backend with CORS configuration
4. **Secure Communication** - HTTPS required in production
5. **Token Expiration** - Handle 401 responses and redirect to login

## Future Enhancements

As per the IAM development plan (F2-F7):
- OAuth2/OIDC client integration
- Multi-factor authentication UI
- User and organization management interfaces
- Role and permission management
- Client and scope configuration
- Security monitoring and audit log viewing
