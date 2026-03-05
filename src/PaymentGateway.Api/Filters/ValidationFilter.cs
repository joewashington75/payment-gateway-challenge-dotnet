using FluentValidation;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

using PaymentGateway.Contracts.Payments;

namespace PaymentGateway.Api.Filters;

public class ValidationFilter(
    IValidator<CreatePaymentRequest> validator,
    ILogger<ValidationFilter> logger) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.ActionArguments.Values.FirstOrDefault(v => v is CreatePaymentRequest) is CreatePaymentRequest request)
        {
            var result = await validator.ValidateAsync(request);

            if (!result.IsValid)
            {
                var maskedCard = request.CardNumber.Length >= 4
                    ? $"****{request.CardNumber[^4..]}"
                    : "****";

                logger.LogWarning(
                    "Payment request rejected. CardNumber: {CardNumber}, ExpiryMonth: {ExpiryMonth}, ExpiryYear: {ExpiryYear}, Currency: {Currency}, Amount: {Amount}. Errors: {Errors}",
                    maskedCard, request.ExpiryMonth, request.ExpiryYear, request.Currency, request.Amount,
                    string.Join("; ", result.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}")));

                context.Result = new BadRequestObjectResult(new RejectedPaymentResponse(
                    PaymentResponseStatus.Rejected,
                    result.Errors.Select(e => new ValidationError(e.PropertyName, e.ErrorMessage))));
                return;
            }
        }

        await next();
    }
}