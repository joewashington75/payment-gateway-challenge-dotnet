using LanguageExt;

using PaymentGateway.Contracts.Payments;
using PaymentGateway.Core.Entities;
using PaymentGateway.Core.Enums;
using PaymentGateway.Core.Errors;
using PaymentGateway.Core.Interfaces;
using PaymentGateway.Core.Models;

namespace PaymentGateway.Application.Services;

public class PaymentService(IBankClient bankClient, IPaymentsRepository paymentsRepository)
    : IPaymentService
{
    public async Task<Either<PaymentError, GetPaymentResponse>> ProcessPaymentAsync(CreatePaymentRequest request)
    {
        var bankRequest = new BankPaymentRequest(
            CardNumber: request.CardNumber,
            ExpiryDate: $"{request.ExpiryMonth:D2}/{request.ExpiryYear}",
            Currency: request.Currency,
            Amount: request.Amount,
            Cvv: request.Cvv);

        var bankResult = await bankClient.ProcessPaymentAsync(bankRequest);

        return bankResult.Map(bankResponse =>
        {
            var status = bankResponse.Authorized ? PaymentStatus.Authorized : PaymentStatus.Declined;

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                Status = status,
                CardNumberLastFour = request.CardNumber[^4..],
                ExpiryMonth = request.ExpiryMonth,
                ExpiryYear = request.ExpiryYear,
                Currency = request.Currency,
                Amount = request.Amount,
                AuthorizationCode = bankResponse.AuthorizationCode,
                Authorised = bankResponse.Authorized,
                DateCreated = DateTimeOffset.UtcNow
            };

            paymentsRepository.Add(payment);

            return MapToResponse(payment);
        });
    }

    public Either<PaymentError, GetPaymentResponse> GetPayment(Guid id)
    {
        var payment = paymentsRepository.Get(id);
        if (payment is null)
        {
            return new PaymentError.NotFound(id);
        }

        return MapToResponse(payment);
    }

    private static GetPaymentResponse MapToResponse(Payment payment) => new(
        payment.Id,
        MapStatus(payment.Status),
        payment.CardNumberLastFour,
        payment.ExpiryMonth,
        payment.ExpiryYear,
        payment.Currency,
        payment.Amount);

    private static PaymentResponseStatus MapStatus(PaymentStatus status) => status switch
    {
        PaymentStatus.Authorized => PaymentResponseStatus.Authorized,
        PaymentStatus.Declined => PaymentResponseStatus.Declined,
        PaymentStatus.Rejected => PaymentResponseStatus.Rejected,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };
}