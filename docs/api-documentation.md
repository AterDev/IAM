# IAM API Documentation Guide

This document provides comprehensive information about the IAM system's REST API.

## Base URL

- **Development**: `https://localhost:7001`
- **Production**: `https://your-domain.com`

## Authentication

Most endpoints require authentication using JWT Bearer tokens.

### Obtaining Access Token

```http
POST /connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=password&
username=your_username&
password=your_password&
client_id=your_client_id&
client_secret=your_client_secret&
scope=openid profile email
```

### Using Access Token

Include the access token in the `Authorization` header:

```http
GET /api/users
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

## API Endpoints

### OAuth/OIDC Endpoints (`/connect`)

#### Authorization Endpoint

Initiates the OAuth 2.0 authorization code flow.

```http
GET /connect/authorize?response_type=code&client_id=my_client&redirect_uri=https://example.com/callback&scope=openid%20profile&state=xyz&code_challenge=E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM&code_challenge_method=S256
```

**Parameters:**
- `response_type`: Must be "code"
- `client_id`: Your client identifier
- `redirect_uri`: Callback URL after authorization
- `scope`: Space-separated scopes (openid, profile, email, etc.)
- `state`: CSRF protection token
- `code_challenge`: PKCE code challenge (recommended)
- `code_challenge_method`: S256 or plain

**Response:** Redirects to login page or callback URL with authorization code

#### Token Endpoint

Exchanges authorization codes or refresh tokens for access tokens.

```http
POST /connect/token
Content-Type: application/x-www-form-urlencoded

# Authorization Code Flow
grant_type=authorization_code&
code=authorization_code_here&
redirect_uri=https://example.com/callback&
client_id=my_client&
client_secret=my_secret&
code_verifier=dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk
```

```http
# Refresh Token Flow
grant_type=refresh_token&
refresh_token=refresh_token_here&
client_id=my_client&
client_secret=my_secret
```

**Response:**
```json
{
  "access_token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "token_type": "Bearer",
  "expires_in": 3600,
  "refresh_token": "refresh_token_here",
  "scope": "openid profile email"
}
```

#### Token Revocation

Revokes access or refresh tokens.

```http
POST /connect/revoke
Content-Type: application/x-www-form-urlencoded

token=token_to_revoke&
token_type_hint=access_token&
client_id=my_client&
client_secret=my_secret
```

### User Management (`/api/users`)

#### List Users

```http
GET /api/users?page=1&pageSize=20&search=john
Authorization: Bearer {token}
```

**Response:**
```json
{
  "items": [
    {
      "id": "uuid",
      "userName": "johndoe",
      "email": "john@example.com",
      "emailConfirmed": true,
      "createdTime": "2025-01-01T00:00:00Z"
    }
  ],
  "totalCount": 100,
  "pageSize": 20,
  "page": 1
}
```

#### Get User by ID

```http
GET /api/users/{id}
Authorization: Bearer {token}
```

**Response:**
```json
{
  "id": "uuid",
  "userName": "johndoe",
  "email": "john@example.com",
  "phoneNumber": "1234567890",
  "emailConfirmed": true,
  "phoneNumberConfirmed": false,
  "twoFactorEnabled": false,
  "lockoutEnabled": true,
  "lockoutEnd": null,
  "roles": ["User", "Admin"],
  "createdTime": "2025-01-01T00:00:00Z"
}
```

#### Create User

```http
POST /api/users
Authorization: Bearer {token}
Content-Type: application/json

{
  "userName": "johndoe",
  "email": "john@example.com",
  "phoneNumber": "1234567890",
  "password": "SecurePassword@123",
  "emailConfirmed": false,
  "lockoutEnabled": true
}
```

**Response:** 201 Created with user details

#### Update User

```http
PUT /api/users/{id}
Authorization: Bearer {token}
Content-Type: application/json

{
  "email": "newemail@example.com",
  "phoneNumber": "9876543210",
  "emailConfirmed": true
}
```

#### Lock/Unlock User

```http
PATCH /api/users/{id}/status
Authorization: Bearer {token}
Content-Type: application/json

"2025-12-31T23:59:59Z"  // Lockout until this date, or null to unlock
```

#### Delete User

```http
DELETE /api/users/{id}?hardDelete=false
Authorization: Bearer {token}
```

### Role Management (`/api/roles`)

#### List Roles

```http
GET /api/roles
Authorization: Bearer {token}
```

#### Create Role

```http
POST /api/roles
Authorization: Bearer {token}
Content-Type: application/json

{
  "name": "Manager",
  "description": "Manager role with elevated permissions",
  "isSystemRole": false
}
```

#### Assign Permissions to Role

```http
POST /api/roles/{id}/permissions
Authorization: Bearer {token}
Content-Type: application/json

{
  "permissions": ["user.read", "user.write", "role.read"]
}
```

### Client Management (`/api/clients`)

#### List Clients

```http
GET /api/clients
Authorization: Bearer {token}
```

#### Create Client

```http
POST /api/clients
Authorization: Bearer {token}
Content-Type: application/json

{
  "clientId": "my_spa_app",
  "displayName": "My SPA Application",
  "type": "public",
  "requirePkce": true,
  "consentType": "explicit",
  "redirectUris": ["https://localhost:4200/callback"],
  "allowedScopes": ["openid", "profile", "email", "api"]
}
```

**Response:**
```json
{
  "client": {
    "id": "uuid",
    "clientId": "my_spa_app",
    "displayName": "My SPA Application",
    "type": "public",
    "requirePkce": true
  },
  "secret": null  // Only for confidential clients
}
```

#### Rotate Client Secret

```http
POST /api/clients/{id}/secret:rotate
Authorization: Bearer {token}
```

**Response:**
```json
{
  "secret": "new_client_secret_here"
}
```

⚠️ **IMPORTANT**: Store this secret securely. It will not be shown again.

#### Assign Scopes to Client

```http
POST /api/clients/{id}/scopes
Authorization: Bearer {token}
Content-Type: application/json

{
  "scopeIds": ["scope-uuid-1", "scope-uuid-2"]
}
```

## Response Codes

| Code | Description |
|------|-------------|
| 200 | Success |
| 201 | Resource created |
| 204 | Success with no content |
| 400 | Bad request - invalid parameters |
| 401 | Unauthorized - authentication required |
| 403 | Forbidden - insufficient permissions |
| 404 | Resource not found |
| 409 | Conflict - resource already exists |
| 429 | Too many requests - rate limited |
| 500 | Internal server error |

## Error Response Format

```json
{
  "error": "error_code",
  "error_description": "Human-readable error description",
  "timestamp": "2025-01-01T00:00:00Z"
}
```

## Rate Limiting

- **Global**: 100 requests per 10 seconds per IP
- **Limited endpoints** (e.g., login): 5 requests per 10 seconds per IP

Exceeded rate limits return HTTP 429 with `Retry-After` header.

## Pagination

List endpoints support pagination with query parameters:

- `page`: Page number (1-based, default: 1)
- `pageSize`: Items per page (default: 20, max: 100)

## Filtering and Sorting

Most list endpoints support:
- `search`: Text search across relevant fields
- `orderBy`: Field to sort by
- `orderDirection`: `asc` or `desc`

## CORS

CORS is enabled for configured origins. See `appsettings.json` for configuration.

## Swagger/OpenAPI

Interactive API documentation is available at:

```
https://your-domain.com/swagger
```

## Security Best Practices

1. **Always use HTTPS** in production
2. **Store secrets securely** - never commit to source control
3. **Implement PKCE** for public clients (SPAs, mobile apps)
4. **Rotate secrets regularly** for confidential clients
5. **Use refresh token rotation** to limit exposure
6. **Implement proper CSRF protection** with state parameter
7. **Validate redirect URIs** to prevent open redirects
8. **Use appropriate scopes** - request minimum required permissions
9. **Monitor rate limits** to prevent abuse
10. **Log security events** for audit trail

## Examples

### Complete OAuth 2.0 Authorization Code Flow with PKCE

```javascript
// 1. Generate PKCE parameters
const codeVerifier = generateRandomString(64);
const codeChallenge = await sha256(codeVerifier);

// 2. Redirect to authorization endpoint
window.location.href = `https://iam.example.com/connect/authorize?` +
  `response_type=code&` +
  `client_id=my_spa&` +
  `redirect_uri=${encodeURIComponent('https://myapp.com/callback')}&` +
  `scope=openid%20profile%20email&` +
  `state=${generateRandomString(32)}&` +
  `code_challenge=${codeChallenge}&` +
  `code_challenge_method=S256`;

// 3. After redirect, exchange code for tokens
const tokenResponse = await fetch('https://iam.example.com/connect/token', {
  method: 'POST',
  headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
  body: new URLSearchParams({
    grant_type: 'authorization_code',
    code: authorizationCode,
    redirect_uri: 'https://myapp.com/callback',
    client_id: 'my_spa',
    code_verifier: codeVerifier
  })
});

const tokens = await tokenResponse.json();
// Store tokens securely
```

### Using Refresh Token

```javascript
const refreshResponse = await fetch('https://iam.example.com/connect/token', {
  method: 'POST',
  headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
  body: new URLSearchParams({
    grant_type: 'refresh_token',
    refresh_token: storedRefreshToken,
    client_id: 'my_spa'
  })
});

const newTokens = await refreshResponse.json();
// Update stored tokens
```

## Support

For API support and questions:
- Documentation: See `/swagger` endpoint
- Issues: GitHub repository issues
- Email: support@example.com
