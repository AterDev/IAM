# F1: Admin Portal Skeleton Implementation

This document summarizes the implementation of Task F1 from the IAM development plan.

## Overview

Task F1 establishes the foundational architecture for the Angular Admin Portal, including:
- Modular structure with CoreModule and SharedModule
- Global layout with navigation, sidebar, and breadcrumbs
- OAuth-ready HTTP interceptor for authentication
- Basic routing framework with metadata support

## Deliverables

### ✅ 1. CoreModule and SharedModule Setup

**CoreModule** (`src/app/core/`)
- Singleton module for app-level services and providers
- Contains `AuthHttpInterceptor` for HTTP request/response handling
- Implements protection against multiple imports
- Registered in `app.config.ts` via HTTP_INTERCEPTORS

**SharedModule** (`src/app/shared/`)
- Reusable components, directives, and Material UI modules
- Exports common modules for feature module consumption
- Includes custom components like BreadcrumbComponent
- Centralizes Material Design module imports

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

### ✅ 3. AuthHttpInterceptor Implementation

**Key Features:**

**Token Injection:**
```typescript
// Automatically adds to requests:
Authorization: Bearer {accessToken}
```
- Retrieves token from localStorage
- Excludes specific URLs (login, public assets)
- Only injects when token exists

**Error Handling:**
- **401 Unauthorized**: Logout user, redirect to login, clear session
- **403 Forbidden**: Display permission denied message
- **404 Not Found**: Show resource not found message
- **409 Conflict**: Display conflict message
- **500 Server Error**: Show server error with details
- User-friendly error messages via MatSnackBar
- Centralized error handling for all HTTP requests

**Security Considerations:**
- Token stored in localStorage (ready for OAuth2 tokens)
- Automatic session cleanup on authentication failure
- Support for excluding authentication endpoints

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
├── core/
│   ├── core.module.ts
│   └── interceptors/
│       ├── auth-http.interceptor.ts
│       └── auth-http.interceptor.spec.ts
├── shared/
│   ├── shared.module.ts
│   └── components/
│       └── breadcrumb/
│           ├── breadcrumb.ts
│           ├── breadcrumb.html
│           └── breadcrumb.scss
├── layout/
│   ├── layout.ts (updated)
│   ├── layout.html
│   └── navigation/
│       ├── navigation.ts (updated)
│       └── navigation.html (updated)
├── app.config.ts (updated)
└── app.routes.ts (updated)
```

## Testing

A comprehensive test suite has been created for the AuthHttpInterceptor:

**Test Coverage:**
- ✅ Token injection when token exists
- ✅ No token injection when token is missing
- ✅ URL exclusion for authentication endpoints
- ✅ 401 error handling with logout and redirect
- ✅ 403 error handling with message display
- ✅ 404 error handling with custom messages
- ✅ 500 error handling with server messages

**Run Tests:**
```bash
cd src/ClientApp/WebApp
pnpm test
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

## Integration with Backend

The HTTP interceptor is ready for integration with the OAuth2/OIDC backend (B4):

**Current Implementation:**
- Token retrieved from localStorage key `accessToken`
- Authorization header: `Bearer {token}`
- Automatic redirect to `/login` on 401

**Future Integration (F2):**
- Will integrate with OAuth2 token endpoints (`/connect/token`)
- Support for refresh token flow
- PKCE implementation
- Token expiration handling

## Documentation

- **Architecture Guide**: `docs/ARCHITECTURE.md`
  - Module structure explained
  - HTTP interceptor details
  - Routing and authentication flow
  - Development guidelines

## Dependencies

**Existing Dependencies (no new packages added):**
- @angular/material (20.2.5)
- @angular/cdk (20.2.5)
- @ngx-translate/core (17.0.0)
- @angular/router (20.3.2)

**Backend Dependency:**
- Depends on B4 (身份认证核心) for OAuth endpoints
- Ready to integrate when backend authentication is available

## Next Steps

With F1 complete, the following tasks can proceed:

**F2 - Authentication and Login Flow:**
- Integrate with OAuth2/OIDC backend
- Implement login page enhancements
- Add PKCE support
- Implement token refresh logic

**F3-F7 - Feature Modules:**
- User and organization management UI
- Role and permission management UI
- Client and scope configuration UI
- Security monitoring and audit log UI

## Success Criteria

- ✅ CoreModule properly configured and imported once
- ✅ SharedModule available for feature modules
- ✅ AuthHttpInterceptor injecting tokens and handling errors
- ✅ Breadcrumb navigation working with route metadata
- ✅ Global layout functional with all components
- ✅ Application builds without errors
- ✅ Code follows Angular best practices
- ✅ Documentation provided

## References

- IAM Development Plan: `docs/tasks/iam-development-plan.md#f1-admin-portal-骨架与共享模块`
- Angular Style Guide: https://angular.dev/style-guide
- Material Design 3: https://m3.material.io/
