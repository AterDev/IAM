# F1: Admin Portal Skeleton Implementation

This document summarizes the implementation of Task F1 from the IAM development plan.

## Overview

Task F1 establishes the foundational architecture for the Angular Admin Portal, including:
- Standalone component architecture (no NgModules)
- Global layout with navigation, sidebar, and breadcrumbs
- Basic routing framework with metadata support

## Deliverables

### ✅ 1. Standalone Component Architecture

**Shared Components** (`src/app/shared/components/`)
- All components are standalone (no NgModules)
- Modular organization through directory structure
- BreadcrumbComponent as standalone component example
- Components import their own dependencies directly

### ✅ 2. Global Layout Structure

**Components Implemented:**

1. **Layout Component** (existing, enhanced)
   - Top toolbar with user menu and language selector
   - Integration point for navigation and content

2. **Navigation Component** (existing, enhanced)
   - Collapsible sidebar with hierarchical menu
   - Menu items loaded from `assets/menus.json`
   - Breadcrumb integration
   - Material Design sidenav implementation

3. **Breadcrumb Component** (new)
   - Automatic breadcrumb trail generation from route data
   - Translation support via ngx-translate
   - Responsive design with Material styling
   - Home icon navigation

### ✅ 3. HTTP Interceptor

**Note**: HTTP interceptor implementation is deferred. The existing `CustomerHttpInterceptor` remains in place, and a wrapped API request service will be provided separately.

### ✅ 4. Routing Framework

**Enhancements:**
- Added breadcrumb metadata to route definitions
- Maintained lazy-loading for feature modules
- Route guards for authentication
- Structured route hierarchy for better organization

**Example Route Configuration:**
```typescript
{
  path: 'system-user',
  data: { breadcrumb: 'systemUser.title' },
  children: [
    { 
      path: 'index', 
      loadComponent: () => import('./pages/system-user/index/index'),
      data: { breadcrumb: 'systemUser.list' }
    },
  ]
}
```

## File Structure

```
src/app/
├── shared/                        # Shared standalone components
│   └── components/
│       └── breadcrumb/
│           ├── breadcrumb.ts     # Standalone breadcrumb component
│           ├── breadcrumb.html   # Template
│           └── breadcrumb.scss   # Styles
│
├── layout/                        # Layout components
│   └── navigation/
│       ├── navigation.ts         # Breadcrumb integration
│       └── navigation.html       # Breadcrumb rendering
│
├── app.config.ts                  # App configuration (unchanged)
└── app.routes.ts                  # Route definitions with breadcrumb metadata
```

## Build Verification

**Development Build:**
```bash
pnpm ng build --configuration development
```

**Results:**
- ✅ No compilation errors
- ✅ No TypeScript errors
- ✅ No warnings
- ✅ Bundle size: ~3.88 MB (development)
- ✅ All lazy routes properly configured

## Development Guidelines

### Adding a New Feature Module

1. Create feature module structure using directories
2. Create standalone components
3. Define routes with breadcrumb metadata
4. Add menu items to `assets/menus.json`

### Adding Shared Components

1. Create component in `src/app/shared/components/`
2. Make component standalone with `standalone: true`
3. Import required modules directly in component
4. Use across feature modules by importing directly

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

## Future Enhancements

As per the IAM development plan (F2-F7):
- OAuth2/OIDC client integration
- Multi-factor authentication UI
- User and organization management interfaces
- Role and permission management
- Client and scope configuration
- Security monitoring and audit log viewing
