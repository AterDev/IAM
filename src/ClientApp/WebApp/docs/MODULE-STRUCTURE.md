# Admin Portal Module Structure

This diagram shows the module structure implemented for the Admin Portal.

## Module Hierarchy

```
App Root
├── app.config.ts (Application Configuration)
│   ├── Router Configuration
│   ├── HTTP Client with Interceptors
│   ├── Translation Service
│   └── Zone-less Change Detection
│
├── CoreModule (Singleton Services)
│   ├── AuthHttpInterceptor
│   │   ├── Token Injection (Bearer {token})
│   │   ├── Error Handling (401, 403, 404, 409, 500)
│   │   └── Redirect Logic
│   └── [Future Core Services]
│
├── SharedModule (Reusable Components & Modules)
│   ├── Components
│   │   └── BreadcrumbComponent
│   ├── Material Design Modules
│   │   ├── MatButtonModule
│   │   ├── MatIconModule
│   │   ├── MatToolbarModule
│   │   ├── MatSidenavModule
│   │   ├── MatTableModule
│   │   ├── MatFormFieldModule
│   │   └── [20+ Material modules]
│   └── Common Angular Modules
│       ├── CommonModule
│       ├── RouterModule
│       ├── FormsModule
│       ├── ReactiveFormsModule
│       └── TranslateModule
│
└── Feature Modules (Lazy Loaded)
    ├── Layout Module
    │   ├── LayoutComponent
    │   │   ├── Toolbar (User menu, Language selector)
    │   │   └── NavigationComponent
    │   │       ├── Sidebar (Collapsible menu)
    │   │       ├── BreadcrumbComponent
    │   │       └── Content Area (router-outlet)
    │   └── Navigation Data (menus.json)
    │
    ├── Authentication Module
    │   ├── LoginComponent
    │   ├── AuthService
    │   └── AuthGuard
    │
    └── Business Modules (Lazy Loaded)
        ├── System User Module
        ├── System Role Module
        └── System Logs Module
```

## Data Flow

### Authentication Flow
```
1. User Login
   ↓
2. Token stored in localStorage
   ↓
3. AuthService updates state
   ↓
4. AuthHttpInterceptor injects token
   Authorization: Bearer {token}
   ↓
5. Backend validates token
   ↓
6. Response returned
   ↓
7. Error? → AuthHttpInterceptor handles
   - 401: Logout + Redirect to /login
   - 403: Permission denied message
   - 404/409/500: User-friendly message
```

### Navigation Flow
```
User navigates
   ↓
Router activates route
   ↓
AuthGuard checks authentication
   ↓
Route data loaded
   ↓
BreadcrumbComponent reads route metadata
   ↓
Breadcrumb trail updated
   ↓
Component loaded in router-outlet
```

## Component Communication

```
LayoutComponent
├── Injects: AuthService, Router, TranslateService
├── Provides: User menu, Language switching
└── Contains: NavigationComponent

NavigationComponent
├── Injects: HttpClient
├── Loads: menus.json
├── Provides: Sidebar navigation
└── Contains: BreadcrumbComponent, router-outlet

BreadcrumbComponent
├── Injects: Router, ActivatedRoute
├── Reads: Route data (breadcrumb metadata)
└── Renders: Dynamic breadcrumb trail

AuthHttpInterceptor
├── Injects: AuthService, Router, MatSnackBar
├── Intercepts: All HTTP requests
├── Adds: Authorization header
└── Handles: HTTP errors globally
```

## File Organization

```
src/app/
├── core/                          # Singleton services (import once)
│   ├── core.module.ts            # Core module definition
│   ├── interceptors/
│   │   ├── auth-http.interceptor.ts      # OAuth token interceptor
│   │   └── auth-http.interceptor.spec.ts # Unit tests
│   └── services/                 # [Future: Global services]
│
├── shared/                        # Shared components (import many times)
│   ├── shared.module.ts          # Shared module definition
│   └── components/
│       ├── breadcrumb/
│       │   ├── breadcrumb.ts     # Breadcrumb component
│       │   ├── breadcrumb.html   # Template
│       │   └── breadcrumb.scss   # Styles
│       └── [Future: More shared components]
│
├── layout/                        # Layout components
│   ├── layout.ts                 # Main layout wrapper
│   ├── layout.html               # Layout template
│   └── navigation/
│       ├── navigation.ts         # Sidebar navigation
│       ├── navigation.html       # Navigation template
│       └── navigation.scss       # Navigation styles
│
├── pages/                         # Feature page components
│   ├── login/                    # Login page
│   ├── system-user/              # User management
│   ├── system-role/              # Role management
│   └── system-logs/              # Audit logs
│
├── services/                      # Business services
│   └── auth.service.ts           # Authentication service
│
├── share/                         # Legacy shared utilities
│   ├── shared-modules.ts         # Module exports helper
│   ├── auth.guard.ts             # Route guard
│   └── components/               # Reusable components
│
├── app.config.ts                  # App configuration
├── app.routes.ts                  # Route definitions
└── app.ts                         # Root component
```

## Module Import Strategy

### CoreModule
- **Import once** in app.config.ts
- Contains singleton services
- Uses guard to prevent multiple imports

### SharedModule
- **Import many times** in feature modules
- No services (only declarations/exports)
- Provides common UI components and modules

### Feature Modules
- **Lazy loaded** for better performance
- Import SharedModule as needed
- Define feature-specific routes

## Dependency Graph

```
app.config.ts
    ↓
[Imports CoreModule providers]
    ↓
CoreModule
    ├─→ AuthHttpInterceptor (HTTP_INTERCEPTORS)
    └─→ [Future core services]

Feature Modules
    ↓
[Import SharedModule]
    ↓
SharedModule
    ├─→ Material Design Modules
    ├─→ Common Angular Modules
    ├─→ Forms Modules
    └─→ Translation Module

Feature Components
    ↓
[Use shared components]
    ↓
BreadcrumbComponent (standalone)
```

## Best Practices Applied

1. **Separation of Concerns**
   - Core: Singleton services and interceptors
   - Shared: Reusable UI components and modules
   - Features: Business logic and pages

2. **Lazy Loading**
   - Feature modules loaded on demand
   - Reduces initial bundle size
   - Better performance

3. **Standalone Components**
   - BreadcrumbComponent is standalone
   - Better tree-shaking
   - Easier to test and reuse

4. **Module Guards**
   - CoreModule prevents multiple imports
   - Ensures singleton pattern

5. **Token Injection**
   - Centralized in interceptor
   - Consistent across all requests
   - Easy to maintain

6. **Error Handling**
   - Global error handling
   - User-friendly messages
   - Automatic session management
