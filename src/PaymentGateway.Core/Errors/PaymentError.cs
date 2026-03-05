namespace PaymentGateway.Core.Errors;

public abstract record PaymentError(string Message)
{
    public sealed record BankUnavailable() : PaymentError("Bank is unavailable");
    public sealed record NotFound : PaymentError
    {
        public NotFound(Guid id) : base($"Payment Id {id} not found") { }
    }
}