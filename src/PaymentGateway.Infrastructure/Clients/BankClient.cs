using System.Net.Http.Json;

using LanguageExt;

using Microsoft.Extensions.Logging;

using PaymentGateway.Core.Errors;
using PaymentGateway.Core.Interfaces;
using PaymentGateway.Core.Models;

namespace PaymentGateway.Infrastructure.Clients;

public sealed class BankClient(
    HttpClient httpClient,
    ILogger<BankClient> logger) : IBankClient
{
    public async Task<Either<PaymentError, BankPaymentResponse>> ProcessPaymentAsync(BankPaymentRequest request)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync("/payments", request);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Bank returned unsuccessful status code {StatusCode}", (int)response.StatusCode);
                return new PaymentError.BankUnavailable();
            }

            var bankResponse = await response.Content.ReadFromJsonAsync<BankPaymentResponse>();
            return bankResponse ?? (Either<PaymentError, BankPaymentResponse>)new PaymentError.BankUnavailable();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception occurred while calling bank API");
            return new PaymentError.BankUnavailable();
        }
    }
}