using FluentValidation;

using PaymentGateway.Contracts.Payments;

namespace PaymentGateway.Application.Validation;

public sealed class CreatePaymentRequestValidator : AbstractValidator<CreatePaymentRequest>
{
    private static readonly HashSet<string> AllowedCurrencies = ["GBP", "USD", "EUR"];

    private const string CardNumberMessage = "Card number must be between 14 and 19 numeric digits.";
    private const string ExpiryMonthMessage = "Expiry month must be between 1 and 12.";
    private const string ExpiryFutureMessage = "Card expiry date must be in the future.";
    private static readonly string CurrencyMessage = $"Currency must be one of: {string.Join(", ", AllowedCurrencies)}.";
    private const string AmountMessage = "Amount must be greater than zero.";
    private const string CvvMessage = "CVV must be 3 or 4 numeric digits.";

    public CreatePaymentRequestValidator()
    {
        RuleFor(x => x.CardNumber)
            .NotEmpty().WithMessage(CardNumberMessage)
            .MinimumLength(14).WithMessage(CardNumberMessage)
            .MaximumLength(19).WithMessage(CardNumberMessage)
            .Must(card => card.All(char.IsDigit)).WithMessage(CardNumberMessage);

        RuleFor(x => x.ExpiryMonth)
            .InclusiveBetween(1, 12).WithMessage(ExpiryMonthMessage);

        RuleFor(x => x)
            .Must(x =>
            {
                var now = DateTime.UtcNow;
                return x.ExpiryYear > now.Year ||
                       (x.ExpiryYear == now.Year && x.ExpiryMonth >= now.Month);
            })
            .WithMessage(ExpiryFutureMessage);

        RuleFor(x => x.Currency)
            .Must(c => AllowedCurrencies.Contains(c)).WithMessage(CurrencyMessage);

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage(AmountMessage);

        RuleFor(x => x.Cvv)
            .NotEmpty().WithMessage(CvvMessage)
            .MinimumLength(3).WithMessage(CvvMessage)
            .MaximumLength(4).WithMessage(CvvMessage)
            .Must(cvv => cvv.All(char.IsDigit)).WithMessage(CvvMessage);
    }
}