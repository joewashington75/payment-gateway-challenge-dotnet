namespace PaymentGateway.Contracts.Payments;

public sealed record RejectedPaymentResponse(
    PaymentResponseStatus Status,
    IEnumerable<ValidationError> Errors);

public sealed record ValidationError(string Field, string Message);