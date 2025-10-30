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
            GrantType.AuthorizationCode => OAuthConstants.GrantTypes.AuthorizationCode,
            GrantType.RefreshToken => OAuthConstants.GrantTypes.RefreshToken,
            GrantType.ClientCredentials => OAuthConstants.GrantTypes.ClientCredentials,
            GrantType.Password => OAuthConstants.GrantTypes.Password,
            GrantType.DeviceCode => OAuthConstants.GrantTypes.DeviceCode,
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
            OAuthConstants.GrantTypes.AuthorizationCode => GrantType.AuthorizationCode,
            OAuthConstants.GrantTypes.RefreshToken => GrantType.RefreshToken,
            OAuthConstants.GrantTypes.ClientCredentials => GrantType.ClientCredentials,
            OAuthConstants.GrantTypes.Password => GrantType.Password,
            OAuthConstants.GrantTypes.DeviceCode => GrantType.DeviceCode,
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
            ResponseType.Code => OAuthConstants.ResponseTypes.Code,
            ResponseType.Token => OAuthConstants.ResponseTypes.Token,
            ResponseType.IdToken => OAuthConstants.ResponseTypes.IdToken,
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
            OAuthConstants.ResponseTypes.Code => ResponseType.Code,
            OAuthConstants.ResponseTypes.Token => ResponseType.Token,
            OAuthConstants.ResponseTypes.IdToken => ResponseType.IdToken,
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
            TokenType.AccessToken => OAuthConstants.TokenTypes.AccessToken,
            TokenType.RefreshToken => OAuthConstants.TokenTypes.RefreshToken,
            TokenType.IdToken => OAuthConstants.TokenTypes.IdToken,
            TokenType.AuthorizationCode => OAuthConstants.TokenTypes.AuthorizationCode,
            TokenType.DeviceCode => OAuthConstants.TokenTypes.DeviceCode,
            TokenType.UserCode => OAuthConstants.TokenTypes.UserCode,
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
            OAuthConstants.TokenTypes.AccessToken => TokenType.AccessToken,
            OAuthConstants.TokenTypes.RefreshToken => TokenType.RefreshToken,
            OAuthConstants.TokenTypes.IdToken => TokenType.IdToken,
            OAuthConstants.TokenTypes.AuthorizationCode => TokenType.AuthorizationCode,
            OAuthConstants.TokenTypes.DeviceCode => TokenType.DeviceCode,
            OAuthConstants.TokenTypes.UserCode => TokenType.UserCode,
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
            TokenStatus.Valid => OAuthConstants.TokenStatuses.Valid,
            TokenStatus.Revoked => OAuthConstants.TokenStatuses.Revoked,
            TokenStatus.Redeemed => OAuthConstants.TokenStatuses.Redeemed,
            TokenStatus.Pending => OAuthConstants.TokenStatuses.Pending,
            TokenStatus.Denied => OAuthConstants.TokenStatuses.Denied,
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
            OAuthConstants.TokenStatuses.Valid => TokenStatus.Valid,
            OAuthConstants.TokenStatuses.Revoked => TokenStatus.Revoked,
            OAuthConstants.TokenStatuses.Redeemed => TokenStatus.Redeemed,
            OAuthConstants.TokenStatuses.Pending => TokenStatus.Pending,
            OAuthConstants.TokenStatuses.Denied => TokenStatus.Denied,
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
            AuthorizationType.Code => OAuthConstants.AuthorizationTypes.Code,
            AuthorizationType.ClientCredentials => OAuthConstants.AuthorizationTypes.ClientCredentials,
            AuthorizationType.Password => OAuthConstants.AuthorizationTypes.Password,
            AuthorizationType.DeviceCode => OAuthConstants.AuthorizationTypes.DeviceCode,
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
            OAuthConstants.AuthorizationTypes.Code => AuthorizationType.Code,
            OAuthConstants.AuthorizationTypes.ClientCredentials => AuthorizationType.ClientCredentials,
            OAuthConstants.AuthorizationTypes.Password => AuthorizationType.Password,
            OAuthConstants.AuthorizationTypes.DeviceCode => AuthorizationType.DeviceCode,
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
            AuthorizationStatus.Valid => OAuthConstants.AuthorizationStatuses.Valid,
            AuthorizationStatus.Revoked => OAuthConstants.AuthorizationStatuses.Revoked,
            AuthorizationStatus.Pending => OAuthConstants.AuthorizationStatuses.Pending,
            AuthorizationStatus.Authorized => OAuthConstants.AuthorizationStatuses.Authorized,
            AuthorizationStatus.Denied => OAuthConstants.AuthorizationStatuses.Denied,
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
            OAuthConstants.AuthorizationStatuses.Valid => AuthorizationStatus.Valid,
            OAuthConstants.AuthorizationStatuses.Revoked => AuthorizationStatus.Revoked,
            OAuthConstants.AuthorizationStatuses.Pending => AuthorizationStatus.Pending,
            OAuthConstants.AuthorizationStatuses.Authorized => AuthorizationStatus.Authorized,
            OAuthConstants.AuthorizationStatuses.Denied => AuthorizationStatus.Denied,
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
            CodeChallengeMethod.Plain => OAuthConstants.CodeChallengeMethods.Plain,
            CodeChallengeMethod.S256 => OAuthConstants.CodeChallengeMethods.S256,
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
            OAuthConstants.CodeChallengeMethods.Plain => CodeChallengeMethod.Plain,
            OAuthConstants.CodeChallengeMethods.S256 => CodeChallengeMethod.S256,
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
            ResponseMode.Query => OAuthConstants.ResponseModes.Query,
            ResponseMode.Fragment => OAuthConstants.ResponseModes.Fragment,
            ResponseMode.FormPost => OAuthConstants.ResponseModes.FormPost,
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
            OAuthConstants.ResponseModes.Query => ResponseMode.Query,
            OAuthConstants.ResponseModes.Fragment => ResponseMode.Fragment,
            OAuthConstants.ResponseModes.FormPost => ResponseMode.FormPost,
            _ => null
        };
    }
}
