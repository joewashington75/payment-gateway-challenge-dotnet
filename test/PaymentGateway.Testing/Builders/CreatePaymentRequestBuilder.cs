using PaymentGateway.Contracts.Payments;

namespace PaymentGateway.Testing.Builders;

public sealed class CreatePaymentRequestBuilder
{
    private string _cardNumber = "2222405343248877";
    private int _expiryMonth = 4;
    private int _expiryYear = DateTime.UtcNow.Year + 1;
    private string _currency = "GBP";
    private int _amount = 100;
    private string _cvv = "123";

    public CreatePaymentRequestBuilder WithCardNumber(string cardNumber)
    {
        _cardNumber = cardNumber;
        return this;
    }

    public CreatePaymentRequestBuilder WithExpiryMonth(int expiryMonth)
    {
        _expiryMonth = expiryMonth;
        return this;
    }

    public CreatePaymentRequestBuilder WithExpiryYear(int expiryYear)
    {
        _expiryYear = expiryYear;
        return this;
    }

    public CreatePaymentRequestBuilder WithCurrency(string currency)
    {
        _currency = currency;
        return this;
    }

    public CreatePaymentRequestBuilder WithAmount(int amount)
    {
        _amount = amount;
        return this;
    }

    public CreatePaymentRequestBuilder WithCvv(string cvv)
    {
        _cvv = cvv;
        return this;
    }

    public CreatePaymentRequest Build() => new(
        _cardNumber,
        _expiryMonth,
        _expiryYear,
        _currency,
        _amount,
        _cvv);
}