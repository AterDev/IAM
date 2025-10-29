# Integration Testing Guide

This document describes the integration testing setup for the IAM system.

## Initial Admin Account

For integration testing purposes, a default admin account is automatically created during the initial database migration.

### Admin Credentials

- **Username**: `admin`
- **Password**: `MakeDotnetGreatAgain`
- **Email**: `admin@iam.local`
- **Role**: Administrator

### How It Works

The admin account is created automatically by the `MigrationService` when it runs for the first time. The seeding logic is implemented in `Worker.cs`:

1. **Check for existing admin**: The service checks if a user with username "admin" already exists
2. **Create Administrator role**: If the role doesn't exist, it's created
3. **Create admin user**: A user account is created with the predefined credentials
4. **Assign role**: The Administrator role is assigned to the admin user
5. **Password hashing**: The password is securely hashed using PBKDF2

The admin account is only created once. If it already exists, the seeding logic is skipped.

## Admin Login

### Web Portal Login

The IAM system includes a web-based admin portal for managing the system.

**Access the Admin Portal:**
1. Navigate to `https://localhost:7001` (or your IAM server URL)
2. Click on the login link or navigate to `/login`
3. Enter credentials:
   - Username: `admin`
   - Password: `MakeDotnetGreatAgain`
4. Click "Login" button

The login page is located at: `src/ClientApp/WebApp/src/app/pages/login`

### Login Features

The admin login page includes:
- Username and password fields
- Form validation
- Error messages for invalid credentials
- Visual starfield background effect
- Support for internationalization (i18n)
- Integration with OIDC authentication service

### Shared vs Separate Login

The current implementation uses a **shared login page** for both:
- Regular users authenticating to access client applications
- Administrators logging in to manage the IAM system

This approach provides:
- **Consistency**: Same authentication flow for all users
- **Simplicity**: Single login implementation to maintain
- **Security**: Unified security policies and audit logging
- **Role-based access**: Administrators are identified by their role assignment

After login, users are directed to appropriate sections based on their roles and permissions.

## Sample Projects for Testing

The `samples/` directory contains example projects demonstrating how to integrate with IAM:

### Backend Sample (ASP.NET Core)

Located in: `samples/backend-dotnet/`

This sample demonstrates:
- Validating JWT tokens issued by IAM
- Protecting API endpoints
- Extracting user claims from tokens
- CORS configuration for SPAs

**Quick Start:**
```bash
cd samples/backend-dotnet
dotnet run
```

See [Backend Sample README](../samples/backend-dotnet/README.md) for detailed instructions.

### Frontend Sample (Angular)

Located in: `samples/frontend-angular/`

This sample demonstrates:
- OAuth 2.0 / OIDC authentication flow
- Using `angular-auth-oidc-client` library
- Protected routes
- API calls with token injection

**Quick Start:**
```bash
cd samples/frontend-angular
npm install
npm start
```

See [Frontend Sample README](../samples/frontend-angular/README.md) for detailed instructions.

## Integration Testing Scenarios

### Scenario 1: User Authentication Flow

1. Start IAM server
2. Navigate to Angular sample app
3. Click "Login" button
4. Enter admin credentials
5. Verify successful login and token issuance
6. Check that user information is displayed

### Scenario 2: API Authorization

1. Authenticate using Angular sample
2. Navigate to "Protected" page
3. Click "Call Protected API" button
4. Verify API call succeeds with token
5. Check API response includes user claims

### Scenario 3: Token Refresh

1. Authenticate and wait for access token to near expiration
2. Make API call
3. Verify token is automatically refreshed
4. Confirm API call succeeds without re-login

### Scenario 4: Client Management

1. Login to admin portal as admin
2. Navigate to Clients section
3. Create new client for testing
4. Configure scopes and redirect URIs
5. Test authentication with new client

### Scenario 5: User Management

1. Login to admin portal as admin
2. Navigate to Users section
3. Create new test user
4. Assign roles to user
5. Test login with new user
6. Verify role-based access

## Setting Up Test Clients

Before running integration tests, you need to configure clients in IAM:

### For Angular Sample

1. Login to admin portal
2. Go to Clients → Create New Client
3. Configure:
   - **Client ID**: `sample-angular-client`
   - **Client Type**: Public (SPA)
   - **Grant Types**: Authorization Code with PKCE
   - **Redirect URIs**: `http://localhost:4200`
   - **Post Logout Redirect URIs**: `http://localhost:4200`
   - **Allowed Scopes**: `openid`, `profile`, `email`, `sample-api`
   - **Require PKCE**: Yes
   - **Require Client Secret**: No

### For Backend API

1. Login to admin portal
2. Go to Resources → API Resources
3. Create:
   - **Name**: `sample-api`
   - **Display Name**: Sample API
   - **Description**: Sample API for integration testing
   - **Scopes**: Define required scopes

## Testing Checklist

- [ ] IAM server starts successfully
- [ ] Database migrations run without errors
- [ ] Admin account is created automatically
- [ ] Admin can login to web portal
- [ ] Admin can create new users
- [ ] Admin can create new roles
- [ ] Admin can register new clients
- [ ] Admin can configure API resources
- [ ] Angular sample app can authenticate via OIDC
- [ ] Angular sample receives valid tokens
- [ ] Angular sample can call protected API
- [ ] Backend API validates tokens correctly
- [ ] Token refresh works automatically
- [ ] Logout clears tokens and sessions
- [ ] Audit logs capture authentication events

## Troubleshooting

### Admin account not created

**Check:**
- Database migrations ran successfully
- No errors in MigrationService logs
- Check database for Users and Roles tables

**Solution:**
- Review logs in MigrationService output
- Manually verify database state
- Re-run migrations if needed

### Cannot login with admin credentials

**Check:**
- Credentials are exactly: `admin` / `MakeDotnetGreatAgain`
- Admin account exists in database
- No account lockout

**Solution:**
- Verify username is lowercase "admin"
- Check user record in database
- Reset password if needed using UserManager

### Sample apps cannot connect

**Check:**
- IAM server is running and accessible
- URLs in configuration match actual server URLs
- CORS is configured correctly
- Clients are registered in IAM

**Solution:**
- Verify IAM server URL (default: https://localhost:7001)
- Check CORS settings in IAM
- Confirm client configuration matches app settings

### SSL certificate errors

**For Development:**
- Accept self-signed certificate warnings
- Add exception for localhost certificates
- Use `dotnet dev-certs https --trust` to trust certificates

**For Production:**
- Use valid SSL certificates
- Configure proper certificate chain
- Ensure HTTPS is enforced

## Additional Resources

- [Sample Projects README](../samples/README.md)
- [Main Project README](../README.md)
- [Entity Framework Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [OpenID Connect](https://openid.net/connect/)

## Security Notes

### Production Considerations

**⚠️ IMPORTANT**: The default admin credentials are for testing only!

For production deployments:

1. **Change admin password immediately** after first deployment
2. **Use strong, unique passwords** (minimum 16 characters)
3. **Enable two-factor authentication** for admin accounts
4. **Implement account lockout policies**
5. **Regular security audits** of admin accounts
6. **Monitor admin actions** via audit logs
7. **Limit admin access** to specific IP ranges if possible
8. **Use separate admin accounts** for different administrators
9. **Rotate passwords regularly**
10. **Disable or delete default account** and create named admin accounts

### Password Policy Recommendations

- Minimum 12 characters
- Mix of uppercase, lowercase, numbers, and symbols
- No common words or patterns
- Not based on user information
- Changed regularly (every 90 days)
- Not reused from previous passwords
- Stored securely (password managers)
