# Sample ASP.NET Core API with IAM Integration

This is a sample ASP.NET Core Web API project demonstrating OAuth 2.0/OIDC integration with the IAM system.

## Features

- JWT Bearer authentication
- Protected API endpoints
- CORS configuration for Angular frontend
- Swagger/OpenAPI documentation

## Configuration

Update `appsettings.json` to configure the IAM authority:

```json
{
  "Authentication": {
    "Authority": "https://localhost:7001",
    "Audience": "sample-api"
  }
}
```

### IAM Setup

Before running this sample, ensure you have:

1. A client registered in IAM with the following settings:
   - **Client ID**: `sample-api-client`
   - **Client Type**: Resource Server / API
   - **Allowed Scopes**: `openid`, `profile`, `sample-api`
   - **Token Endpoint Auth Method**: Client credentials or bearer token

2. An API Resource registered in IAM:
   - **Name**: `sample-api`
   - **Display Name**: Sample API
   - **Scopes**: Define the scopes your API supports

## Running the Application

1. Ensure .NET 10 SDK is installed
2. Navigate to the sample directory:
   ```bash
   cd samples/backend-dotnet
   ```

3. Run the application:
   ```bash
   dotnet run
   ```

4. Access Swagger UI at: `https://localhost:5001/swagger`

## API Endpoints

### Public Endpoints

- `GET /api/public` - Publicly accessible endpoint

### Protected Endpoints (Require Authentication)

- `GET /api/protected` - Basic protected endpoint
- `GET /api/weatherforecast` - Returns weather forecast data with user information

## Testing with cURL

### Public Endpoint
```bash
curl https://localhost:5001/api/public
```

### Protected Endpoint with JWT Token
```bash
curl -H "Authorization: Bearer YOUR_ACCESS_TOKEN" https://localhost:5001/api/protected
```

## Integration with Angular Frontend

This API is configured to work with the Angular sample frontend (`samples/frontend-angular`).

The CORS policy allows requests from `http://localhost:4200` (Angular dev server).

## Authentication Flow

1. User authenticates via Angular frontend
2. Angular app obtains access token from IAM
3. Angular app includes token in API requests via Authorization header
4. API validates token with IAM authority
5. API returns protected resources if token is valid

## Additional Resources

- [ASP.NET Core Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/)
- [JWT Bearer Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/jwt-authn)
- [OAuth 2.0 and OpenID Connect](https://openid.net/developers/how-connect-works/)
