using Application.Interfaces.Services;
using FluentValidation;
using Shared.Constants;
using Shared.Responses;

namespace WebApi.Common.Filters;

/// <summary>
/// Generic minimal-API endpoint filter that validates any request type with a
/// registered <see cref="IValidator{T}"/> (auto-registered for every validator in
/// Application via <c>AddValidatorsFromAssemblyContaining</c>). Apply it per-endpoint:
/// <c>.AddEndpointFilter&lt;ValidationFilter&lt;TRequest&gt;&gt;()</c>. Validation
/// error messages are message keys, translated via <see cref="IMessageProvider"/>
/// before returning — same convention as every other user-facing message in the
/// codebase (<see cref="MessageKeys"/>).
/// </summary>
public sealed class ValidationFilter<TRequest>(IMessageProvider messageProvider) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var validator = context.HttpContext.RequestServices.GetService<IValidator<TRequest>>();
        var request = validator is not null ? context.Arguments.OfType<TRequest>().FirstOrDefault() : default;

        if (validator is not null && request is not null)
        {
            var ct = context.HttpContext.RequestAborted;
            var result = await validator.ValidateAsync(request, ct);

            if (!result.IsValid)
            {
                // Resolve all error messages concurrently rather than one await per
                // error in sequence — the original MyMoney ValidationFilter awaited
                // each translation in a loop.
                var errors = await Task.WhenAll(
                    result.Errors.Select(e => messageProvider.GetMessagesAsync(e.ErrorMessage, ct)));

                var validationMessage = await messageProvider.GetMessagesAsync(MessageKeys.Common.ValidationError, ct);

                var response = ApiResponse<object?>.Fail(StatusCodes.Status400BadRequest, validationMessage, errors);

                // Every handled business outcome (including validation failure) is
                // HTTP 200 with the real status inside the body — see
                // WebApi/Common/ApiResponseExtensions.cs. The original MyMoney
                // ValidationFilter returned a literal HTTP 400 here, which broke its
                // own "always 200 except real server errors" convention; fixed here.
                return Results.Ok(response);
            }
        }

        return await next(context);
    }
}
