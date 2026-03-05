using System.Text.Json.Serialization;

namespace PaymentGateway.Contracts.Payments;

public sealed record CreatePaymentRequest(
    [property: JsonPropertyName("card_number")] string CardNumber,
    [property: JsonPropertyName("expiry_month")] int ExpiryMonth,
    [property: JsonPropertyName("expiry_year")] int ExpiryYear,
    string Currency,
    int Amount,
    string Cvv);