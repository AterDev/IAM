# Admin Portal Architecture

This document describes the Admin Portal frontend architecture and component organization.

## Architecture Overview

The application follows Angular best practices with a **standalone component** architecture:
- No NgModules (except for app config)
- Components are self-contained and import their own dependencies
- Modular organization through directory structure
- Lazy loading for better performance

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

## HTTP Client

The application uses the existing `CustomerHttpInterceptor` for HTTP request handling.

**Note**: A more comprehensive API request service will be provided in future iterations.

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

## Navigation Flow

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

## Development Guidelines

### Creating a Standalone Component

```typescript
import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-my-component',
  standalone: true,
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

## File Organization

```
src/app/
├── shared/                        # Shared standalone components
│   └── components/
│       └── breadcrumb/            # Breadcrumb component
│
├── layout/                        # Layout components
│   ├── layout.ts                 # Main layout wrapper
│   └── navigation/               # Sidebar navigation
│
├── pages/                         # Feature page components (lazy loaded)
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
