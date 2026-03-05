using LanguageExt;

using PaymentGateway.Core.Errors;
using PaymentGateway.Core.Models;

namespace PaymentGateway.Core.Interfaces;

public interface IBankClient
{
    Task<Either<PaymentError, BankPaymentResponse>> ProcessPaymentAsync(BankPaymentRequest request);
}