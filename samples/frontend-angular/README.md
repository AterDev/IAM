# Sample Angular Application with IAM OIDC Integration

This is a sample Angular application demonstrating OAuth 2.0/OpenID Connect integration with the IAM system using the `angular-auth-oidc-client` library.

## Features

- OpenID Connect authentication flow
- Automatic access token management
- Protected routes with authentication guard
- HTTP interceptor for automatic token injection in API calls
- Silent token renewal
- User profile display
- Sample protected API calls

## Prerequisites

- Node.js 20+ and npm
- IAM server running at `https://localhost:7001`
- Sample API running at `https://localhost:5001` (optional, for testing API calls)

## IAM Configuration

Before running this application, configure a client in IAM with the following settings:

### Client Registration

1. Log in to IAM admin portal with credentials:
   - Username: `admin`
   - Password: `MakeDotnetGreatAgain`

2. Create a new Client with these settings:
   - **Client ID**: `sample-angular-client`
   - **Client Name**: Sample Angular Application
   - **Application Type**: SPA (Single Page Application)
   - **Grant Types**: Authorization Code with PKCE
   - **Redirect URIs**: 
     - `http://localhost:4200`
     - `http://localhost:4200/`
   - **Post Logout Redirect URIs**:
     - `http://localhost:4200`
     - `http://localhost:4200/`
   - **Allowed CORS Origins**: `http://localhost:4200`
   - **Allowed Scopes**:
     - `openid`
     - `profile`
     - `email`
     - `sample-api` (if using the sample API)
   - **Require PKCE**: Yes
   - **Require Client Secret**: No
   - **Allow Refresh Token**: Yes

### API Resource Configuration (Optional)

If you want to call protected APIs:

1. Create an API Resource:
   - **Name**: `sample-api`
   - **Display Name**: Sample API
   - **Scopes**: Define the scopes your API needs

2. Ensure the client has access to the `sample-api` scope

## Installation

1. Navigate to the sample directory:
   ```bash
   cd samples/frontend-angular
   ```

2. Install dependencies:
   ```bash
   npm install
   ```

## Configuration

The OIDC configuration is in `src/app/app.config.ts`:

```typescript
{
  authority: 'https://localhost:7001',
  redirectUrl: window.location.origin,
  postLogoutRedirectUri: window.location.origin,
  clientId: 'sample-angular-client',
  scope: 'openid profile email sample-api',
  responseType: 'code',
  silentRenew: true,
  useRefreshToken: true
}
```

Adjust these settings if your IAM server is running on a different URL or port.

## Running the Application

Start the development server:

```bash
npm start
```

The application will be available at `http://localhost:4200`.

## Application Structure

```
src/
├── app/
│   ├── home/                    # Home page component
│   ├── protected/               # Protected page component (requires auth)
│   ├── unauthorized/            # Unauthorized page component
│   ├── app.component.ts         # Root component with navigation
│   ├── app.config.ts            # Application configuration with OIDC setup
│   ├── app.routes.ts            # Route configuration
│   └── auth.interceptor.ts      # HTTP interceptor for adding auth tokens
├── index.html
├── main.ts
└── styles.scss
```

## Features Demonstrated

### 1. Authentication Flow

- Click "Login" button to start OAuth 2.0 Authorization Code flow with PKCE
- User is redirected to IAM login page
- After successful authentication, user is redirected back to the app
- Access token and refresh token are automatically managed

### 2. Protected Routes

The `/protected` route is guarded with `AutoLoginPartialRoutesGuard`:
- Unauthenticated users are redirected to IAM login
- After login, users are redirected back to the protected route

### 3. HTTP Interceptor

The `authInterceptor` automatically adds the access token to HTTP requests:
- Configured to add tokens to requests to `https://localhost:5001/api`
- Tokens are added as `Authorization: Bearer {token}` header

### 4. User Information

The protected page displays user information from the ID token:
- Name
- Email
- Subject (user ID)

### 5. API Calls

The protected page includes a button to call the sample API:
- Demonstrates automatic token injection
- Shows API response with user claims

## Authentication State

The application monitors authentication state:
- Navigation bar updates based on login state
- Shows login/logout button appropriately
- Displays user name when authenticated

## Token Management

The `angular-auth-oidc-client` library handles:
- Token storage (in session storage by default)
- Silent token renewal before expiration
- Refresh token usage
- Automatic token cleanup on logout

## Testing the Integration

1. **Start IAM server** (ensure it's running at `https://localhost:7001`)

2. **Register the client** in IAM as described above

3. **Start this Angular application**:
   ```bash
   npm start
   ```

4. **Navigate to** `http://localhost:4200`

5. **Click "Login"** and authenticate with:
   - Username: `admin`
   - Password: `MakeDotnetGreatAgain`

6. **Access protected routes** to verify authentication works

7. **(Optional) Start the sample API** and click "Call Protected API" to test API integration

## Troubleshooting

### CORS Errors

If you see CORS errors:
- Ensure the client's "Allowed CORS Origins" includes `http://localhost:4200`
- Check that IAM CORS policy allows the Angular app origin

### Redirect URI Mismatch

If authentication fails with redirect URI error:
- Verify the redirect URIs in IAM client configuration exactly match `http://localhost:4200`
- Check for trailing slashes

### Token Not Sent to API

If API calls don't include the token:
- Verify the API URL matches the `secureRoutes` configuration in `app.config.ts`
- Check browser console for interceptor errors

### SSL Certificate Warnings

For development with self-signed certificates:
- Accept the certificate warning when accessing IAM for the first time
- You may need to visit `https://localhost:7001` directly to accept the certificate

## Learn More

- [angular-auth-oidc-client Documentation](https://github.com/damienbod/angular-auth-oidc-client)
- [OpenID Connect Specification](https://openid.net/specs/openid-connect-core-1_0.html)
- [OAuth 2.0 Authorization Code Flow with PKCE](https://oauth.net/2/pkce/)

## Integration with Sample API

This Angular application is designed to work with the sample API in `samples/backend-dotnet`.

To test the full integration:

1. Start IAM server
2. Start the sample API (`cd samples/backend-dotnet && dotnet run`)
3. Start this Angular app (`cd samples/frontend-angular && npm start`)
4. Log in and navigate to the Protected page
5. Click "Call Protected API" to test authenticated API calls

The sample API will validate the token with IAM and return protected data along with user information.
