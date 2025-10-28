# Admin Portal Architecture

This document describes the Admin Portal frontend architecture and component organization.

## Architecture Overview

The application follows Angular best practices with a **standalone component** architecture:
- No NgModules (except for app config)
- Components are self-contained and import their own dependencies
- Modular organization through directory structure
- Lazy loading for better performance

## Authentication & Authorization

### OIDC/OAuth2 Authentication Service

The application uses a comprehensive OIDC/OAuth2 authentication system with PKCE (Proof Key for Code Exchange) support.

**OidcAuthService** (`src/app/services/oidc-auth.service.ts`):
- Signal-based state management for reactive authentication state
- PKCE flow implementation (S256 method)
- Multiple grant types support:
  - Authorization Code with PKCE
  - Resource Owner Password Credentials
  - Refresh Token
- Automatic token refresh before expiration
- Secure token storage in localStorage
- JWT ID token parsing for user information

**Authentication Flow**:
1. **Authorization Code Flow (recommended)**:
   - `startAuthorizationCodeFlow()` - Initiates PKCE flow
   - `handleCallback()` - Processes authorization code and exchanges for tokens
2. **Password Flow** (for trusted clients):
   - `loginWithPassword()` - Direct username/password authentication
3. **Token Management**:
   - Automatic refresh 5 minutes before expiration
   - `refreshAccessToken()` - Manual token refresh
   - `logout()` - Clear tokens and navigate to login

**State Management**:
```typescript
// Reactive signals for authentication state
readonly isAuthenticated = computed(() => this.authState().isAuthenticated);
readonly user = computed(() => this.authState().user);
readonly accessToken = computed(() => this.authState().accessToken);
```

### HTTP Interceptor

**CustomerHttpInterceptor** (`src/app/customer-http.interceptor.ts`):
- Automatically injects access token into HTTP requests
- Handles 401 unauthorized responses
- Shows user-friendly error messages via Material Snackbar
- Redirects to login on authentication failures

### Route Guard

**AuthGuard** (`src/app/share/auth.guard.ts`):
- Protects routes requiring authentication
- Redirects unauthenticated users to login page
- Uses OidcAuthService for authentication checks

## Authentication Pages

### Login (`src/app/pages/login/`)
- Username/password authentication
- Form validation with reactive forms
- Integration with OidcAuthService
- Links to register and forgot password pages
- Starfield canvas background animation

### Register (`src/app/pages/register/`)
- User self-registration
- Email, username, phone number, password fields
- Password strength validation (requires uppercase, lowercase, numbers)
- Password confirmation matching
- Success message with auto-redirect to login

### Forgot Password (`src/app/pages/forgot-password/`)
- Two-step password reset flow
- Step 1: Email verification code request
- Step 2: New password setup with verification code
- Material Stepper for guided flow

### Device Code (`src/app/pages/device-code/`)
- Device authorization grant flow (RFC 8628)
- User code input with auto-formatting (XXXX-XXXX)
- Validation and submission to backend
- Support for devices without easy input methods

### Authorization Consent (`src/app/pages/authorize/`)
- OAuth2/OIDC authorization consent page
- Displays requesting client information
- Shows requested permissions/scopes
- User can approve or deny access
- Required and optional scopes indication
- Privacy notice and user context

## API Client

### Base Service

**BaseService** (`src/app/services/api/base.service.ts`):
- Centralized HTTP request handling
- Automatic content type detection
- Binary file download support
- JSON/text response parsing
- Authorization header injection

### API Client

**ApiClient** (`src/app/services/api/api-client.ts`):
- Centralized access to all API services
- Dependency injection for services:
  - `auditTrail` - Audit logs
  - `clients` - OAuth clients management
  - `common` - Common utilities
  - `commonSettings` - System settings
  - `externalAuth` - External authentication
  - `oAuth` - OAuth/OIDC endpoints
  - `organizations` - Organization management
  - `resources` - API resources
  - `roles` - Role management
  - `scopes` - OAuth scopes
  - `users` - User management

**Usage Example**:
```typescript
// In a component
private apiClient = inject(ApiClient);

// Login
this.apiClient.oAuth.token(tokenData).subscribe(...);

// Create user
this.apiClient.users.createUser(userData).subscribe(...);

// Get audit logs
this.apiClient.auditTrail.getLogs(...).subscribe(...);
```

### OAuth Service

**OAuthService** (`src/app/services/api/services/oauth.service.ts`):
- `/connect/authorize` - Authorization endpoint
- `/connect/token` - Token endpoint (all grant types)
- `/connect/device` - Device authorization
- `/connect/introspect` - Token introspection
- `/connect/revoke` - Token revocation
- `/connect/logout` - Logout endpoint

## Component Structure

### Shared Components (`src/app/shared/components/`)

Reusable standalone components that can be imported across the application.

**BreadcrumbComponent:**
- Automatic breadcrumb generation from route metadata
- Translation support
- Home icon navigation
- Material Design styling

Example usage:
```typescript
@Component({
  selector: 'app-breadcrumb',
  standalone: true,
  imports: [CommonModule, RouterModule, MatIconModule, TranslateModule],
  templateUrl: './breadcrumb.html',
  styleUrl: './breadcrumb.scss'
})
export class BreadcrumbComponent { }
```

### Global Layout

The application uses a consistent layout structure with the following components:

**Layout Component** (`src/app/layout/`)
- Header/Toolbar - Top navigation bar with user menu and language selector
- NavigationComponent - Sidebar navigation with menu and breadcrumbs
- Content Area - Main content rendered via `<router-outlet>`

**Navigation Components:**
1. **NavigationComponent** - Sidebar navigation with menu items loaded from `assets/menus.json`
2. **BreadcrumbComponent** - Automatic breadcrumb generation from route data

## Routing

Routes are defined in `src/app/app.routes.ts` with:
- Lazy-loaded feature modules
- Route guards for authentication
- Breadcrumb metadata in route data

**Public Routes**:
- `/login` - Login page
- `/register` - User registration
- `/forgot-password` - Password reset
- `/device-code` - Device authorization
- `/authorize` - Authorization consent

**Protected Routes** (require authentication):
- All routes under the main layout component

Example route configuration:
```typescript
{
  path: 'register', 
  loadComponent: () => import('./pages/register/register').then(m => m.Register)
}
```

## Navigation Flow

```
User navigates
   ↓
Router activates route
   ↓
AuthGuard checks authentication (if protected route)
   ↓
Route data loaded
   ↓
BreadcrumbComponent reads route metadata
   ↓
Breadcrumb trail updated
   ↓
Component loaded in router-outlet
```

## Development Guidelines

### Creating a Standalone Component

```typescript
import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-my-component',
  imports: [CommonModule, MatButtonModule],
  templateUrl: './my-component.html',
  styleUrl: './my-component.scss'
})
export class MyComponent { }
```

### Adding Routes with Breadcrumbs

```typescript
{
  path: 'my-feature',
  data: { breadcrumb: 'myFeature.title' },
  children: [
    { 
      path: 'list', 
      loadComponent: () => import('./pages/my-feature/list').then(m => m.ListComponent),
      data: { breadcrumb: 'myFeature.list' }
    },
  ]
}
```

### Importing Shared Components

```typescript
import { BreadcrumbComponent } from '../../shared/components/breadcrumb/breadcrumb';

@Component({
  imports: [BreadcrumbComponent, /* other imports */],
  // ...
})
export class MyComponent { }
```

### Using Signals for State Management

```typescript
import { signal, computed } from '@angular/core';

@Component({...})
export class MyComponent {
  // Writable signal
  count = signal(0);
  
  // Computed signal
  doubleCount = computed(() => this.count() * 2);
  
  increment() {
    this.count.update(n => n + 1);
  }
}
```

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

**Translation Keys for Authentication**:
- `login.*` - Login page translations
- `register.*` - Registration translations
- `forgotPassword.*` - Password reset translations
- `deviceCode.*` - Device code translations
- `authorize.*` - Authorization consent translations
- `scopes.*` - OAuth scope descriptions
- `validation.*` - Form validation messages

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

## File Organization

```
src/app/
├── services/
│   ├── oidc-auth.service.ts       # OIDC/OAuth2 authentication
│   ├── auth.service.ts            # Legacy auth service (deprecated)
│   └── api/
│       ├── api-client.ts          # API client aggregator
│       ├── base.service.ts        # Base HTTP service
│       ├── services/              # Individual API services
│       │   ├── oauth.service.ts   # OAuth endpoints
│       │   ├── users.service.ts   # User management
│       │   ├── roles.service.ts   # Role management
│       │   └── ...
│       └── models/                # TypeScript models/DTOs
│
├── pages/                         # Feature page components (lazy loaded)
│   ├── login/                    # Login page
│   ├── register/                 # Registration page
│   ├── forgot-password/          # Password reset
│   ├── device-code/              # Device authorization
│   ├── authorize/                # OAuth consent
│   └── ...
│
├── shared/                        # Shared standalone components
│   └── components/
│       └── breadcrumb/            # Breadcrumb component
│
├── layout/                        # Layout components
│   ├── layout.ts                 # Main layout wrapper
│   └── navigation/               # Sidebar navigation
│
├── share/                         # Legacy shared utilities
│   ├── auth.guard.ts             # Route guard
│   ├── shared-modules.ts         # Module exports helper
│   └── components/               # Reusable components
│
├── customer-http.interceptor.ts  # HTTP interceptor
├── app.config.ts                 # App configuration
├── app.routes.ts                 # Route definitions
└── app.ts                        # Root component
```

## Best Practices

1. **Standalone Components**
   - All new components should be standalone
   - Import dependencies directly in each component
   - Better tree-shaking and performance

2. **Lazy Loading**
   - Feature modules loaded on demand
   - Reduces initial bundle size
   - Better performance

3. **Component Organization**
   - Shared: Reusable UI components
   - Layout: Application shell components
   - Pages: Feature-specific page components

4. **Routing**
   - Use breadcrumb metadata for navigation
   - Lazy load feature modules
   - Apply route guards for protected routes

5. **Styling**
   - Use Material Design components
   - Support dark mode
   - Responsive design

6. **State Management**
   - Use Angular Signals for reactive state
   - Computed signals for derived state
   - Keep state transformations pure

7. **Security**
   - Never store sensitive data in localStorage without encryption
   - Always use HTTPS in production
   - Implement PKCE for authorization code flow
   - Validate and sanitize all user inputs
   - Use HttpOnly cookies for sensitive tokens when possible
