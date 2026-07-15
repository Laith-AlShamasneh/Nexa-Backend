using Application.Interfaces.Services;
using FluentValidation;
using Shared.Constants;
using Shared.Responses;

namespace WebApi.Common.Filters;

/// <summary>
/// Runs a <typeparamref name="TRequest"/> through its registered <see cref="IValidator{T}"/>
/// (auto-registered for every validator in Application via
/// <c>AddValidatorsFromAssemblyContaining</c>) and translates any errors via
/// <see cref="IMessageProvider"/>. Shared by <see cref="ValidationFilter{TRequest}"/>
/// (the normal case — JSON-bound minimal-API parameters) and endpoints that must
/// validate a request built manually after mapping (e.g. a multipart form request
/// mapped to its Application DTO — see WebApi/Endpoints/Tenancy/OrganizationEndpoints.cs)
/// where the endpoint-filter pipeline never sees the Application type as a bound
/// argument to intercept.
/// </summary>
public static class RequestValidator
{
    public static async Task<IResult?> ValidateAsync<TRequest>(
        IServiceProvider serviceProvider,
        TRequest request,
        IMessageProvider messageProvider,
        CancellationToken ct)
    {
        var validator = serviceProvider.GetService<IValidator<TRequest>>();
        if (validator is null) return null;

        var result = await validator.ValidateAsync(request, ct);
        if (result.IsValid) return null;

        // Resolve all error messages concurrently rather than one await per error in
        // sequence — the original MyMoney ValidationFilter awaited each translation
        // in a loop.
        var errors = await Task.WhenAll(
            result.Errors.Select(e => messageProvider.GetMessagesAsync(e.ErrorMessage, ct)));

        var validationMessage = await messageProvider.GetMessagesAsync(MessageKeys.Common.ValidationError, ct);
        var response = ApiResponse<object?>.Fail(StatusCodes.Status400BadRequest, validationMessage, errors);

        // Every handled business outcome (including validation failure) is HTTP 200
        // with the real status inside the body — see WebApi/Common/ApiResponseExtensions.cs.
        // The original MyMoney ValidationFilter returned a literal HTTP 400 here,
        // which broke its own "always 200 except real server errors" convention;
        // fixed here.
        return Results.Ok(response);
    }
}

/// <summary>
/// Generic minimal-API endpoint filter that validates any request type with a
/// registered <see cref="IValidator{T}"/>. Apply it per-endpoint:
/// <c>.AddEndpointFilter&lt;ValidationFilter&lt;TRequest&gt;&gt;()</c>. Only usable
/// when <typeparamref name="TRequest"/> itself is bound as a minimal-API parameter
/// (e.g. from JSON) — see <see cref="RequestValidator"/> for the manual-mapping case.
/// </summary>
public sealed class ValidationFilter<TRequest>(IMessageProvider messageProvider) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var request = context.Arguments.OfType<TRequest>().FirstOrDefault();

        if (request is not null)
        {
            var failure = await RequestValidator.ValidateAsync(
                context.HttpContext.RequestServices, request, messageProvider, context.HttpContext.RequestAborted);

            if (failure is not null) return failure;
        }

        return await next(context);
    }
}
