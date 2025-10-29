# IAM Integration Samples

This directory contains sample projects demonstrating how to integrate with the IAM (Identity and Access Management) system.

## Available Samples

### 1. Backend Sample (ASP.NET Core)
Location: `backend-dotnet/`

A sample ASP.NET Core Web API demonstrating:
- JWT Bearer authentication
- Token validation with IAM
- Protected API endpoints
- CORS configuration for SPAs

[View Backend Sample Documentation](backend-dotnet/README.md)

### 2. Frontend Sample (Angular)
Location: `frontend-angular/`

A sample Angular application demonstrating:
- OAuth 2.0 / OpenID Connect authentication
- Authorization Code flow with PKCE
- Automatic token management
- Protected routes
- HTTP interceptor for API calls

[View Frontend Sample Documentation](frontend-angular/README.md)

## Quick Start Guide

### Prerequisites

1. **IAM Server Running**
   - Default URL: `https://localhost:7001`
   - Admin credentials: `admin` / `MakeDotnetGreatAgain`

2. **Development Tools**
   - .NET 10 SDK (for backend sample)
   - Node.js 20+ and npm (for frontend sample)

### Step 1: Configure IAM

1. Start the IAM server:
   ```bash
   cd src/AppHost
   dotnet run
   ```

2. Log in to the IAM admin portal at `https://localhost:7001`
   - Username: `admin`
   - Password: `MakeDotnetGreatAgain`

3. Register an API Resource (for backend sample):
   - Navigate to Resources → API Resources
   - Create new resource:
     - Name: `sample-api`
     - Display Name: Sample API
     - Scopes: Add scopes as needed (e.g., `read`, `write`)

4. Register a Client (for frontend sample):
   - Navigate to Clients → Create New Client
   - Client ID: `sample-angular-client`
   - Application Type: SPA (Single Page Application)
   - Grant Types: Authorization Code with PKCE
   - Redirect URIs: `http://localhost:4200`
   - Post Logout Redirect URIs: `http://localhost:4200`
   - Allowed Scopes: `openid`, `profile`, `email`, `sample-api`
   - Require PKCE: Yes
   - Require Client Secret: No

### Step 2: Run the Backend Sample

```bash
cd samples/backend-dotnet
dotnet run
```

The API will be available at `https://localhost:5001`

**Test endpoints:**
- Public: `GET https://localhost:5001/api/public`
- Protected: `GET https://localhost:5001/api/protected` (requires authentication)
- Swagger UI: `https://localhost:5001/swagger`

### Step 3: Run the Frontend Sample

```bash
cd samples/frontend-angular
npm install
npm start
```

The application will be available at `http://localhost:4200`

### Step 4: Test the Integration

1. Open `http://localhost:4200` in your browser
2. Click "Login" button
3. You'll be redirected to IAM login page
4. Enter credentials: `admin` / `MakeDotnetGreatAgain`
5. After successful login, you'll be redirected back to the Angular app
6. Navigate to "Protected" page to see user information
7. Click "Call Protected API" to test API integration

## Architecture Overview

```
┌─────────────────┐
│  Angular App    │
│  (Port 4200)    │
│                 │
│  - OIDC Client  │
│  - UI/UX        │
└────────┬────────┘
         │
         │ 1. Auth Request
         │ 3. Token Request
         ▼
┌─────────────────┐
│   IAM Server    │
│  (Port 7001)    │
│                 │
│  - Auth Server  │
│  - User Store   │
│  - Token Issue  │
└────────┬────────┘
         │
         │ 2. User Login
         │
         │
         │ 4. Access Token
         ▼
┌─────────────────┐
│  API Server     │
│  (Port 5001)    │
│                 │
│  - Validate     │
│  - Resources    │
└─────────────────┘
```

## Authentication Flow

1. **User initiates login** from Angular app
2. **Angular redirects** to IAM authorization endpoint
3. **User authenticates** at IAM login page
4. **IAM redirects back** to Angular with authorization code
5. **Angular exchanges code** for tokens (access token, ID token, refresh token)
6. **Angular stores tokens** securely
7. **API calls include** access token in Authorization header
8. **API validates token** with IAM
9. **API returns** protected resources

## Token Types

### Access Token
- Used to access protected API resources
- Short-lived (typically 1 hour)
- Validated by resource servers

### ID Token
- Contains user identity information
- Used by client application
- Not sent to APIs

### Refresh Token
- Used to obtain new access tokens
- Longer-lived (typically 30 days)
- Enables silent authentication renewal

## Security Considerations

### Production Deployment

When deploying to production:

1. **Use HTTPS everywhere**
   - IAM server must use HTTPS
   - API servers must use HTTPS
   - Configure valid SSL certificates

2. **Secure token storage**
   - Angular: Uses session storage by default
   - Consider using HTTP-only cookies for refresh tokens

3. **Configure CORS properly**
   - Only allow specific origins
   - Don't use wildcard (*) in production

4. **Token lifetime**
   - Keep access tokens short-lived (5-60 minutes)
   - Use refresh tokens for long sessions
   - Implement proper token revocation

5. **Client secrets**
   - Public clients (SPAs) should not use client secrets
   - Backend clients should use strong client secrets
   - Store secrets securely (environment variables, key vault)

6. **Validate all tokens**
   - Verify token signature
   - Check expiration
   - Validate issuer and audience
   - Check required claims

## Development Tips

### Debugging OIDC Flow

Enable debug logging in Angular app:
```typescript
// In app.config.ts
logLevel: LogLevel.Debug
```

### Testing with Different Users

Create additional test users in IAM:
1. Navigate to Users → Create New User
2. Assign appropriate roles
3. Test different authorization scenarios

### API Testing with Postman

1. Get access token:
   - Use OAuth 2.0 in Postman
   - Authorization URL: `https://localhost:7001/connect/authorize`
   - Token URL: `https://localhost:7001/connect/token`
   - Client ID: Your client ID
   - Scopes: Required scopes

2. Use token in requests:
   - Add header: `Authorization: Bearer {access_token}`

## Common Issues and Solutions

### Issue: CORS errors
**Solution**: Ensure client's allowed CORS origins includes the Angular app URL

### Issue: Redirect URI mismatch
**Solution**: Verify redirect URIs in IAM exactly match the app URL (check trailing slashes)

### Issue: Token validation fails
**Solution**: Check that API's authority configuration matches IAM's issuer URL

### Issue: Infinite redirect loop
**Solution**: Clear browser storage and check OIDC configuration

### Issue: SSL certificate errors
**Solution**: For development, accept self-signed certificates or configure proper certificates

## Additional Resources

- [IAM Project Documentation](../../README.md)
- [OAuth 2.0 RFC](https://tools.ietf.org/html/rfc6749)
- [OpenID Connect Specification](https://openid.net/specs/openid-connect-core-1_0.html)
- [PKCE RFC](https://tools.ietf.org/html/rfc7636)

## Support

For issues or questions:
1. Check the sample READMEs for specific guidance
2. Review the main IAM documentation
3. Check the issue tracker on GitHub
