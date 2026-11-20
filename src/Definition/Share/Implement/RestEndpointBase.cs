using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;

namespace Share.Implement;

/// <summary>Common helpers for statically registered Minimal API endpoint groups.</summary>
public abstract class RestEndpointBase
{
    public sealed class ApiResult<T> : IResult, IEndpointMetadataProvider
    {
        private readonly IResult _result;

        private ApiResult(IResult result) => _result = result;

        public static implicit operator ApiResult<T>(T value) => new(TypedResults.Ok(value));
        public static implicit operator ApiResult<T>(Ok<T> result) => new(result);
        public static implicit operator ApiResult<T>(Created<T> result) => new(result);
        public static implicit operator ApiResult<T>(Accepted<T> result) => new(result);
        public static implicit operator ApiResult<T>(Ok result) => new(result);
        public static implicit operator ApiResult<T>(Created result) => new(result);
        public static implicit operator ApiResult<T>(Accepted result) => new(result);
        public static implicit operator ApiResult<T>(NoContent result) => new(result);
        public static implicit operator ApiResult<T>(BadRequest result) => new(result);
        public static implicit operator ApiResult<T>(NotFound result) => new(result);
        public static implicit operator ApiResult<T>(Conflict result) => new(result);
        public static implicit operator ApiResult<T>(UnauthorizedHttpResult result) => new(result);
        public static implicit operator ApiResult<T>(ForbidHttpResult result) => new(result);
        public static implicit operator ApiResult<T>(ProblemHttpResult result) => new(result);

        public Task ExecuteAsync(HttpContext httpContext) => _result.ExecuteAsync(httpContext);

        static void IEndpointMetadataProvider.PopulateMetadata(MethodInfo method, EndpointBuilder builder)
        {
            builder.Metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status200OK, typeof(T), ["application/json"]));
        }
    }

    protected static RouteGroupBuilder MapGroup(
        IEndpointRouteBuilder endpoints,
        string pattern,
        string tag,
        string? authorizationPolicy = null)
    {
        var group = endpoints.MapGroup(pattern).WithTags(tag);
        return string.IsNullOrWhiteSpace(authorizationPolicy)
            ? group
            : group.RequireAuthorization(authorizationPolicy);
    }

    protected static ProblemHttpResult BadRequest(string? detail = null) =>
        TypedResults.Problem(detail: detail, statusCode: StatusCodes.Status400BadRequest);

    protected static ProblemHttpResult Conflict(string? detail = null) =>
        TypedResults.Problem(detail: detail, statusCode: StatusCodes.Status409Conflict);

    protected static ProblemHttpResult NotFound(string? detail = null) =>
        TypedResults.Problem(detail: detail, statusCode: StatusCodes.Status404NotFound);

    protected static ProblemHttpResult Forbid(string? detail = null) =>
        TypedResults.Problem(detail: detail, statusCode: StatusCodes.Status403Forbidden);

    protected static ProblemHttpResult Problem(string? detail = null, int statusCode = StatusCodes.Status500InternalServerError) =>
        TypedResults.Problem(detail: detail, statusCode: statusCode);
}
