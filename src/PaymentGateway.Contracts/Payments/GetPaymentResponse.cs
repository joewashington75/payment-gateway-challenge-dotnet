using System.Text.Json.Serialization;

namespace PaymentGateway.Contracts.Payments;

public sealed record GetPaymentResponse(
    Guid Id,
    PaymentResponseStatus Status,
    [property: JsonPropertyName("card_number_last_four")] string CardNumberLastFour,
    [property: JsonPropertyName("expiry_month")] int ExpiryMonth,
    [property: JsonPropertyName("expiry_year")] int ExpiryYear,
    string Currency,
    int Amount);