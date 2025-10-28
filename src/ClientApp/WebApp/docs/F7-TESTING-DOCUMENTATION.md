# Frontend Testing & Documentation (F7)

This directory contains comprehensive testing infrastructure and documentation for the IAM frontend application.

## 📋 Overview

The F7 task delivers:
- ✅ Unit tests with Jest for components, services, and guards
- ✅ E2E tests with Playwright for critical user flows
- ✅ Comprehensive documentation for users, administrators, and developers

## 🧪 Testing

### Unit Tests (Jest)

**Coverage Areas**:
- Services: UsersService, RolesService, OAuthService, ClientsService
- Guards: AuthGuard
- Components: Login, Role Management, User Management
- Utilities and Helpers

**Commands**:
```bash
# Run all tests
pnpm test

# Watch mode
pnpm test:watch

# Coverage report
pnpm test:coverage
```

**Test Files**:
- `src/app/share/auth.guard.spec.ts`
- `src/app/services/api/services/users.service.spec.ts`
- `src/app/services/api/services/roles.service.spec.ts`
- `src/app/services/api/services/oauth.service.spec.ts`
- `src/app/services/api/services/clients.service.spec.ts`

### E2E Tests (Playwright)

**Test Scenarios**:
- Authentication flow (login, validation, redirects)
- User management workflow (CRUD operations)
- Role management workflow (permissions assignment)
- Client management workflow (OAuth client configuration)

**Commands**:
```bash
# Run E2E tests
pnpm e2e

# UI mode (interactive)
pnpm e2e:ui

# Headed mode (visible browser)
pnpm e2e:headed

# View report
pnpm e2e:report
```

**Test Files**:
- `e2e/auth.spec.ts` - Authentication and login tests
- `e2e/user-management.spec.ts` - User CRUD tests
- `e2e/role-management.spec.ts` - Role and permission tests
- `e2e/client-management.spec.ts` - OAuth client tests

## 📚 Documentation

### User Documentation

**User Manual** (`docs/USER-MANUAL.md`):
- System overview
- Login procedures
- Personal information management
- Password and security settings
- Multi-factor authentication
- Session management
- Common troubleshooting

### Administrator Documentation

**Admin Manual** (`docs/ADMIN-MANUAL.md`):
- Administrator responsibilities
- User account management
- Role and permission configuration
- Organization structure management
- OAuth client setup
- Scope management
- Security auditing
- System settings
- Best practices

### Developer Documentation

**Deployment Guide** (`docs/DEPLOYMENT-GUIDE.md`):
- Environment requirements
- Development setup
- Production deployment
- Docker deployment
- Nginx configuration
- Performance optimization
- Monitoring and maintenance

**Testing Guide** (`docs/TESTING-GUIDE.md`):
- Testing overview
- Unit testing with Jest
- E2E testing with Playwright
- Code coverage
- CI/CD integration
- Best practices

## 📊 Test Coverage

Current test coverage targets:
- **Statements**: ≥ 75%
- **Branches**: ≥ 70%
- **Functions**: ≥ 75%
- **Lines**: ≥ 75%

View coverage report:
```bash
pnpm test:coverage
# Open coverage/lcov-report/index.html
```

## 🔧 Configuration Files

- `jest.config.js` - Jest configuration
- `setup-jest.ts` - Jest setup
- `playwright.config.ts` - Playwright configuration
- `package.json` - Test scripts

## 🚀 Quick Start

### Run All Tests

```bash
# Install dependencies
pnpm install

# Run unit tests
pnpm test

# Run E2E tests
pnpm e2e
```

### Continuous Integration

Tests are integrated into CI/CD pipeline via GitHub Actions (see `.github/workflows/test.yml`).

## 📖 Documentation Structure

```
docs/
├── USER-MANUAL.md          # End user guide
├── ADMIN-MANUAL.md         # Administrator guide
├── DEPLOYMENT-GUIDE.md     # Deployment instructions
├── TESTING-GUIDE.md        # Testing documentation
├── ARCHITECTURE.md         # System architecture
└── CODING-STANDARDS.md     # Code standards
```

## ✅ Completion Checklist

- [x] Jest unit tests for services
- [x] Jest unit tests for guards
- [x] Playwright E2E authentication tests
- [x] Playwright E2E user management tests
- [x] Playwright E2E role management tests
- [x] Playwright E2E client management tests
- [x] User operation manual
- [x] Administrator operation manual
- [x] Deployment guide
- [x] Testing guide

## 🎯 Dependencies

This task (F7) depends on:
- [F1] Admin Portal skeleton ✅
- [F2] Authentication flow ✅
- [F3] User and organization management ✅
- [F4] Role and permission management ✅
- [F5] Client configuration ✅
- [F6] Security monitoring ✅

## 🔗 Related Resources

- [Development Plan](../../../../docs/tasks/iam-development-plan.md)
- [Project README](../../../../README.md)
- [API Documentation](../../../../docs/api-documentation.md)

---

**Task**: F7 - Frontend Automated Testing & Documentation  
**Status**: ✅ Complete  
**Date**: 2025-10-28  
**Version**: 1.0
