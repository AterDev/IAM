# F3: User and Organization Management Interface

## Overview
This implementation provides a comprehensive user and organization management interface for the IAM system, built with Angular 20 and Angular Material.

## Features Implemented

### User Management (`/system-user`)

#### User List Page
- **Table View**: Displays users with username, email, phone, status, and creation time
- **Pagination**: Configurable page size (5, 10, 20, 50 items)
- **Search**: Filter users by username, email, or phone number
- **Status Filter**: Filter by locked/active status
- **Batch Operations**:
  - Select multiple users using checkboxes
  - Batch lock/unlock users
- **Actions Menu**:
  - View user details
  - Edit user
  - Lock/unlock user
  - Delete user (soft delete)

#### User Detail Page (`/system-user/:id`)
- **User Information Display**:
  - Username (read-only)
  - Email and confirmation status
  - Phone number and confirmation status
  - Two-factor authentication status
  - Account lock status
  - Creation timestamp
- **Actions**:
  - Edit user information
  - Toggle lock/unlock status
  - Delete user

#### User Add Dialog
- **Form Fields**:
  - Username (required, min 3 characters)
  - Email (optional, validated)
  - Phone number (optional)
  - Password (required, min 6 characters)
  - Confirm password (must match)
- **Validation**: Real-time form validation with error messages
- **Password Visibility Toggle**: Show/hide password fields

#### User Edit Dialog
- **Editable Fields**:
  - Email
  - Phone number
- **Read-only**: Username displayed as info (cannot be changed)
- **Validation**: Email format validation

### Organization Management (`/organization`)

#### Organization Tree View
- **Hierarchical Display**: Nested tree structure with expand/collapse
- **Visual Indicators**:
  - Business icons for organizations
  - Expand/collapse icons
  - Hover states
- **Node Selection**: Click to select and view details
- **Action Buttons** (per node):
  - Add child organization
  - Edit organization
  - Manage members
  - Delete organization

#### Organization Detail Panel
- **Information Display**:
  - Organization name
  - Level in hierarchy
  - Display order
  - Description (if available)

#### Organization Add Dialog
- **Form Fields**:
  - Organization name (required, min 2 characters)
  - Description (optional)
  - Display order (numeric, min 0)
- **Context**: Shows info when adding child organization
- **Parent Support**: Can add root or child organizations

#### Organization Edit Dialog
- **Editable Fields**:
  - Organization name
  - Description
  - Display order
- **Form Validation**: Real-time validation

#### Organization Members Dialog
- **Member Management**:
  - Add users to organization
  - Remove users from organization
  - User selection dropdown
- **Member Table**: Display current members with actions

## Technical Implementation

### Component Structure

```
src/app/pages/
├── system-user/
│   ├── user-list.ts/html/scss       # Main user list page
│   ├── user-add.ts/html/scss        # Add user dialog
│   ├── user-edit.ts/html/scss       # Edit user dialog
│   └── user-detail.ts/html/scss     # User detail page
└── organization/
    ├── organization-list.ts/html/scss       # Organization tree page
    ├── organization-add.ts/html/scss        # Add organization dialog
    ├── organization-edit.ts/html/scss       # Edit organization dialog
    └── organization-members.ts/html/scss    # Members management dialog
```

### Key Technologies

- **Angular 20**: Latest Angular framework with standalone components
- **Angular Material**: UI component library
  - MatTable for data display
  - MatTree for hierarchical data
  - MatDialog for modals
  - MatPaginator for pagination
  - MatFormField for form inputs
- **Signals**: Reactive state management
- **FormsModule**: Template-driven and reactive forms
- **i18n**: Multi-language support (Chinese and English)

### State Management

Components use Angular Signals for reactive state:
```typescript
dataSource = signal<UserItemDto[]>([]);
total = signal(0);
isLoading = signal(false);
selectedIds = signal<Set<string>>(new Set());
```

### Routing

Routes are configured in `app.routes.ts` with lazy loading:
```typescript
{
  path: 'system-user',
  loadComponent: () => import('./pages/system-user/user-list').then(m => m.UserListComponent)
},
{
  path: 'system-user/:id',
  loadComponent: () => import('./pages/system-user/user-detail').then(m => m.UserDetailComponent)
},
{
  path: 'organization',
  loadComponent: () => import('./pages/organization/organization-list').then(m => m.OrganizationListComponent)
}
```

### Security

- **AuthGuard**: All routes protected by authentication guard
- **Permission Control**: Ready for directive-based permission system
- **Soft Delete**: Users and organizations are soft-deleted, not permanently removed

### API Integration

All components integrate with backend REST APIs:
- **UsersService**: User CRUD operations
- **OrganizationsService**: Organization CRUD operations

### Internationalization

Full i18n support with translations in:
- `assets/i18n/zh.json` - Chinese
- `assets/i18n/en.json` - English

Translation keys cover:
- Common UI elements
- User management terms
- Organization management terms
- Form validation messages

## Usage

### Development

1. Install dependencies:
   ```bash
   cd src/ClientApp/WebApp
   pnpm install
   ```

2. Run development server:
   ```bash
   pnpm start
   ```

3. Build for production:
   ```bash
   pnpm build
   ```

### Navigation

After logging in, access the management interfaces via the system menu:
- **System** → **Account** (`/system-user`)
- **System** → **Organization** (`/organization`)

## Future Enhancements

Potential improvements:
1. Add role-based permission directives for fine-grained access control
2. Implement real-time member list loading for organizations (pending backend API)
3. Add export functionality for user lists
4. Implement advanced filtering and sorting options
5. Add user import from CSV/Excel
6. Implement organization chart visualization

## Dependencies

- Angular 20.3.2
- Angular Material 20.2.5
- TypeScript 5.8.3
- RxJS 7.8.1

## Browser Compatibility

Supports all modern browsers:
- Chrome (latest)
- Firefox (latest)
- Safari (latest)
- Edge (latest)
