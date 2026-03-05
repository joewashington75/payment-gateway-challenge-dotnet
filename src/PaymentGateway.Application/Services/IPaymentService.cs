using LanguageExt;

using PaymentGateway.Contracts.Payments;
using PaymentGateway.Core.Errors;

namespace PaymentGateway.Application.Services;

public interface IPaymentService
{
    Task<Either<PaymentError, GetPaymentResponse>> ProcessPaymentAsync(CreatePaymentRequest request);
    Either<PaymentError, GetPaymentResponse> GetPayment(Guid id);
}