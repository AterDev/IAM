namespace IdentityMod;

/// <summary>
/// Extension methods for OAuth types
/// </summary>
public static class OAuthTypeExtensions
{
    /// <summary>
    /// Convert GrantType enum to OAuth grant type string
    /// </summary>
    public static string ToOAuthString(this GrantType grantType)
    {
        return grantType switch
        {
            GrantType.AuthorizationCode => GrantTypes.AuthorizationCode,
            GrantType.RefreshToken => GrantTypes.RefreshToken,
            GrantType.ClientCredentials => GrantTypes.ClientCredentials,
            GrantType.Password => GrantTypes.Password,
            GrantType.DeviceCode => GrantTypes.DeviceCode,
            _ => throw new ArgumentOutOfRangeException(nameof(grantType), grantType, null)
        };
    }

    /// <summary>
    /// Parse OAuth grant type string to GrantType enum
    /// </summary>
    public static GrantType? ParseGrantType(string? grantType)
    {
        return grantType switch
        {
            GrantTypes.AuthorizationCode => GrantType.AuthorizationCode,
            GrantTypes.RefreshToken => GrantType.RefreshToken,
            GrantTypes.ClientCredentials => GrantType.ClientCredentials,
            GrantTypes.Password => GrantType.Password,
            GrantTypes.DeviceCode => GrantType.DeviceCode,
            _ => null
        };
    }

    /// <summary>
    /// Convert ResponseType enum to OAuth response type string
    /// </summary>
    public static string ToOAuthString(this ResponseType responseType)
    {
        return responseType switch
        {
            ResponseType.Code => ResponseTypes.Code,
            ResponseType.Token => ResponseTypes.Token,
            ResponseType.IdToken => ResponseTypes.IdToken,
            _ => throw new ArgumentOutOfRangeException(nameof(responseType), responseType, null)
        };
    }

    /// <summary>
    /// Parse OAuth response type string to ResponseType enum
    /// </summary>
    public static ResponseType? ParseResponseType(string? responseType)
    {
        return responseType switch
        {
            ResponseTypes.Code => ResponseType.Code,
            ResponseTypes.Token => ResponseType.Token,
            ResponseTypes.IdToken => ResponseType.IdToken,
            _ => null
        };
    }

    /// <summary>
    /// Convert TokenType enum to OAuth token type string
    /// </summary>
    public static string ToOAuthString(this TokenType tokenType)
    {
        return tokenType switch
        {
            TokenType.AccessToken => TokenTypes.AccessToken,
            TokenType.RefreshToken => TokenTypes.RefreshToken,
            TokenType.IdToken => TokenTypes.IdToken,
            TokenType.AuthorizationCode => TokenTypes.AuthorizationCode,
            TokenType.DeviceCode => TokenTypes.DeviceCode,
            TokenType.UserCode => TokenTypes.UserCode,
            _ => throw new ArgumentOutOfRangeException(nameof(tokenType), tokenType, null)
        };
    }

    /// <summary>
    /// Parse OAuth token type string to TokenType enum
    /// </summary>
    public static TokenType? ParseTokenType(string? tokenType)
    {
        return tokenType switch
        {
            TokenTypes.AccessToken => TokenType.AccessToken,
            TokenTypes.RefreshToken => TokenType.RefreshToken,
            TokenTypes.IdToken => TokenType.IdToken,
            TokenTypes.AuthorizationCode => TokenType.AuthorizationCode,
            TokenTypes.DeviceCode => TokenType.DeviceCode,
            TokenTypes.UserCode => TokenType.UserCode,
            _ => null
        };
    }

    /// <summary>
    /// Convert TokenStatus enum to OAuth token status string
    /// </summary>
    public static string ToOAuthString(this TokenStatus tokenStatus)
    {
        return tokenStatus switch
        {
            TokenStatus.Valid => TokenStatuses.Valid,
            TokenStatus.Revoked => TokenStatuses.Revoked,
            TokenStatus.Redeemed => TokenStatuses.Redeemed,
            TokenStatus.Pending => TokenStatuses.Pending,
            TokenStatus.Denied => TokenStatuses.Denied,
            _ => throw new ArgumentOutOfRangeException(nameof(tokenStatus), tokenStatus, null)
        };
    }

    /// <summary>
    /// Parse OAuth token status string to TokenStatus enum
    /// </summary>
    public static TokenStatus? ParseTokenStatus(string? tokenStatus)
    {
        return tokenStatus switch
        {
            TokenStatuses.Valid => TokenStatus.Valid,
            TokenStatuses.Revoked => TokenStatus.Revoked,
            TokenStatuses.Redeemed => TokenStatus.Redeemed,
            TokenStatuses.Pending => TokenStatus.Pending,
            TokenStatuses.Denied => TokenStatus.Denied,
            _ => null
        };
    }

    /// <summary>
    /// Convert AuthorizationType enum to OAuth authorization type string
    /// </summary>
    public static string ToOAuthString(this AuthorizationType authorizationType)
    {
        return authorizationType switch
        {
            AuthorizationType.Code => AuthorizationTypes.Code,
            AuthorizationType.ClientCredentials => AuthorizationTypes.ClientCredentials,
            AuthorizationType.Password => AuthorizationTypes.Password,
            AuthorizationType.DeviceCode => AuthorizationTypes.DeviceCode,
            _ => throw new ArgumentOutOfRangeException(nameof(authorizationType), authorizationType, null)
        };
    }

    /// <summary>
    /// Parse OAuth authorization type string to AuthorizationType enum
    /// </summary>
    public static AuthorizationType? ParseAuthorizationType(string? authorizationType)
    {
        return authorizationType switch
        {
            AuthorizationTypes.Code => AuthorizationType.Code,
            AuthorizationTypes.ClientCredentials => AuthorizationType.ClientCredentials,
            AuthorizationTypes.Password => AuthorizationType.Password,
            AuthorizationTypes.DeviceCode => AuthorizationType.DeviceCode,
            _ => null
        };
    }

    /// <summary>
    /// Convert AuthorizationStatus enum to OAuth authorization status string
    /// </summary>
    public static string ToOAuthString(this AuthorizationStatus authorizationStatus)
    {
        return authorizationStatus switch
        {
            AuthorizationStatus.Valid => AuthorizationStatuses.Valid,
            AuthorizationStatus.Revoked => AuthorizationStatuses.Revoked,
            AuthorizationStatus.Pending => AuthorizationStatuses.Pending,
            AuthorizationStatus.Authorized => AuthorizationStatuses.Authorized,
            AuthorizationStatus.Denied => AuthorizationStatuses.Denied,
            _ => throw new ArgumentOutOfRangeException(nameof(authorizationStatus), authorizationStatus, null)
        };
    }

    /// <summary>
    /// Parse OAuth authorization status string to AuthorizationStatus enum
    /// </summary>
    public static AuthorizationStatus? ParseAuthorizationStatus(string? authorizationStatus)
    {
        return authorizationStatus switch
        {
            AuthorizationStatuses.Valid => AuthorizationStatus.Valid,
            AuthorizationStatuses.Revoked => AuthorizationStatus.Revoked,
            AuthorizationStatuses.Pending => AuthorizationStatus.Pending,
            AuthorizationStatuses.Authorized => AuthorizationStatus.Authorized,
            AuthorizationStatuses.Denied => AuthorizationStatus.Denied,
            _ => null
        };
    }

    /// <summary>
    /// Convert CodeChallengeMethod enum to OAuth code challenge method string
    /// </summary>
    public static string ToOAuthString(this CodeChallengeMethod codeChallengeMethod)
    {
        return codeChallengeMethod switch
        {
            CodeChallengeMethod.Plain => CodeChallengeMethods.Plain,
            CodeChallengeMethod.S256 => CodeChallengeMethods.S256,
            _ => throw new ArgumentOutOfRangeException(nameof(codeChallengeMethod), codeChallengeMethod, null)
        };
    }

    /// <summary>
    /// Parse OAuth code challenge method string to CodeChallengeMethod enum
    /// </summary>
    public static CodeChallengeMethod? ParseCodeChallengeMethod(string? codeChallengeMethod)
    {
        return codeChallengeMethod switch
        {
            CodeChallengeMethods.Plain => CodeChallengeMethod.Plain,
            CodeChallengeMethods.S256 => CodeChallengeMethod.S256,
            _ => null
        };
    }

    /// <summary>
    /// Convert ResponseMode enum to OAuth response mode string
    /// </summary>
    public static string ToOAuthString(this ResponseMode responseMode)
    {
        return responseMode switch
        {
            ResponseMode.Query => ResponseModes.Query,
            ResponseMode.Fragment => ResponseModes.Fragment,
            ResponseMode.FormPost => ResponseModes.FormPost,
            _ => throw new ArgumentOutOfRangeException(nameof(responseMode), responseMode, null)
        };
    }

    /// <summary>
    /// Parse OAuth response mode string to ResponseMode enum
    /// </summary>
    public static ResponseMode? ParseResponseMode(string? responseMode)
    {
        return responseMode switch
        {
            ResponseModes.Query => ResponseMode.Query,
            ResponseModes.Fragment => ResponseMode.Fragment,
            ResponseModes.FormPost => ResponseMode.FormPost,
            _ => null
        };
    }
}
