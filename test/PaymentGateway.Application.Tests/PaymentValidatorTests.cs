using PaymentGateway.Application.Validation;
using PaymentGateway.Testing.Builders;

using Shouldly;

namespace PaymentGateway.Application.Tests;

public sealed class PaymentValidatorTests
{
    private readonly CreatePaymentRequestValidator _validator = new();

    private const string CardNumberLengthMessage = "Card number must be between 14 and 19 numeric digits.";
    private const string ExpiryMonthMessage = "Expiry month must be between 1 and 12.";
    private const string ExpiryFutureMessage = "Card expiry date must be in the future.";
    private const string CurrencyMessage = "Currency must be one of: GBP, USD, EUR.";
    private const string AmountMessage = "Amount must be greater than zero.";
    private const string CvvMessage = "CVV must be 3 or 4 numeric digits.";

    [Fact]
    public void GivenValidRequest_WhenValidate_ThenReturnsNoErrors()
    {
        var request = new CreatePaymentRequestBuilder().Build();
        var result = _validator.Validate(request);
        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("123")]
    [InlineData("12345678901234567890")]
    public void GivenInvalidCardNumberLength_WhenValidate_ThenReturnsError(string cardNumber)
    {
        var request = new CreatePaymentRequestBuilder()
            .WithCardNumber(cardNumber)
            .Build();
        var result = _validator.Validate(request);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage == CardNumberLengthMessage);
    }

    [Fact]
    public void GivenNonNumericCardNumber_WhenValidate_ThenReturnsError()
    {
        var request = new CreatePaymentRequestBuilder()
            .WithCardNumber("2222ABCD43248877")
            .Build();
        var result = _validator.Validate(request);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage == CardNumberLengthMessage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    [InlineData(-1)]
    public void GivenExpiryMonthOutOfRange_WhenValidate_ThenReturnsError(int month)
    {
        var request = new CreatePaymentRequestBuilder()
            .WithExpiryMonth(month)
            .Build();
        var result = _validator.Validate(request);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage == ExpiryMonthMessage);
    }

    [Fact]
    public void GivenCardExpiringThisMonth_WhenValidate_ThenReturnsNoError()
    {
        var now = DateTime.UtcNow;
        var request = new CreatePaymentRequestBuilder()
            .WithExpiryMonth(now.Month)
            .WithExpiryYear(now.Year)
            .Build();
        var result = _validator.Validate(request);
        result.Errors.ShouldNotContain(e => e.ErrorMessage == ExpiryFutureMessage);
    }

    [Fact]
    public void GivenCardExpiredLastMonth_WhenValidate_ThenReturnsError()
    {
        var lastMonth = DateTime.UtcNow.AddMonths(-1);
        var request = new CreatePaymentRequestBuilder()
            .WithExpiryMonth(lastMonth.Month)
            .WithExpiryYear(lastMonth.Year)
            .Build();
        var result = _validator.Validate(request);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage == ExpiryFutureMessage);
    }

    [Fact]
    public void GivenExpiredCard_WhenValidate_ThenReturnsError()
    {
        var request = new CreatePaymentRequestBuilder()
            .WithExpiryMonth(1)
            .WithExpiryYear(2020)
            .Build();
        var result = _validator.Validate(request);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage == ExpiryFutureMessage);
    }

    [Theory]
    [InlineData("")]
    [InlineData("US")]
    [InlineData("GBPP")]
    [InlineData("JPY")]
    public void GivenInvalidCurrency_WhenValidate_ThenReturnsError(string currency)
    {
        var request = new CreatePaymentRequestBuilder()
            .WithCurrency(currency)
            .Build();
        var result = _validator.Validate(request);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage == CurrencyMessage);
    }

    [Theory]
    [InlineData("GBP")]
    [InlineData("USD")]
    [InlineData("EUR")]
    public void GivenValidCurrency_WhenValidate_ThenReturnsNoError(string currency)
    {
        var request = new CreatePaymentRequestBuilder()
            .WithCurrency(currency)
            .Build();
        var result = _validator.Validate(request);
        result.Errors.ShouldNotContain(e => e.ErrorMessage == CurrencyMessage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void GivenNonPositiveAmount_WhenValidate_ThenReturnsError(int amount)
    {
        var request = new CreatePaymentRequestBuilder()
            .WithAmount(amount)
            .Build();
        var result = _validator.Validate(request);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage == AmountMessage);
    }

    [Theory]
    [InlineData("")]
    [InlineData("12")]
    [InlineData("12345")]
    [InlineData("ab3")]
    public void GivenInvalidCvv_WhenValidate_ThenReturnsError(string cvv)
    {
        var request = new CreatePaymentRequestBuilder()
            .WithCvv(cvv)
            .Build();
        var result = _validator.Validate(request);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage == CvvMessage);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("1234")]
    public void GivenValidCvvLength_WhenValidate_ThenReturnsNoError(string cvv)
    {
        var request = new CreatePaymentRequestBuilder()
            .WithCvv(cvv)
            .Build();
        var result = _validator.Validate(request);
        result.Errors.ShouldNotContain(e => e.ErrorMessage == CvvMessage);
    }
}