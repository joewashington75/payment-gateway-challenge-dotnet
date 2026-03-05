using PaymentGateway.Core.Enums;

namespace PaymentGateway.Core.Entities;

public class Payment
{
    public required Guid Id { get; set; }
    public required PaymentStatus Status { get; set; }
    public required string CardNumberLastFour { get; set; } = string.Empty;
    public required int ExpiryMonth { get; set; }
    public required int ExpiryYear { get; set; }
    public required string Currency { get; set; } = string.Empty;
    public required int Amount { get; set; }
    public string AuthorizationCode { get; set; } = string.Empty;
    public bool Authorised { get; set; }
    public required DateTimeOffset DateCreated { get; set; }
}