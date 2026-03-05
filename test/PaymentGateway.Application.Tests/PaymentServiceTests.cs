using LanguageExt;

using Moq;

using PaymentGateway.Application.Services;
using PaymentGateway.Contracts.Payments;
using PaymentGateway.Core.Entities;
using PaymentGateway.Core.Errors;
using PaymentGateway.Core.Interfaces;
using PaymentGateway.Core.Models;
using PaymentGateway.Testing.Builders;

using Shouldly;

namespace PaymentGateway.Application.Tests;

public class PaymentServiceTests
{
    private readonly Mock<IBankClient> _mockBankClient = new();
    private readonly Mock<IPaymentsRepository> _mockRepository = new();
    private readonly PaymentService _service;

    public PaymentServiceTests()
    {
        _service = new PaymentService(_mockBankClient.Object, _mockRepository.Object);
    }

    [Fact]
    public async Task GivenBankAuthorizes_WhenProcessPayment_ThenReturnsAuthorizedAndStores()
    {
        // Arrange
        _mockBankClient
            .Setup(x => x.ProcessPaymentAsync(It.IsAny<BankPaymentRequest>()))
            .ReturnsAsync(Either<PaymentError, BankPaymentResponse>.Right(
                new BankPaymentResponse(true, "auth-123")));

        // Act
        var result = await _service.ProcessPaymentAsync(new CreatePaymentRequestBuilder().Build());

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(response =>
        {
            response.Status.ShouldBe(PaymentResponseStatus.Authorized);
            response.CardNumberLastFour.ShouldBe("8877");
        });
        _mockRepository.Verify(r => r.Add(It.IsAny<Payment>()), Times.Once);
    }

    [Fact]
    public async Task GivenBankDeclines_WhenProcessPayment_ThenReturnsDeclinedAndStores()
    {
        // Arrange
        _mockBankClient
            .Setup(x => x.ProcessPaymentAsync(It.IsAny<BankPaymentRequest>()))
            .ReturnsAsync(Either<PaymentError, BankPaymentResponse>.Right(
                new BankPaymentResponse(false, "")));

        // Act
        var result = await _service.ProcessPaymentAsync(new CreatePaymentRequestBuilder().Build());

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(response =>
        {
            response.Status.ShouldBe(PaymentResponseStatus.Declined);
        });
        _mockRepository.Verify(r => r.Add(It.IsAny<Payment>()), Times.Once);
    }

    [Fact]
    public async Task GivenBankUnavailable_WhenProcessPayment_ThenReturnsBankUnavailableError()
    {
        // Arrange
        _mockBankClient
            .Setup(x => x.ProcessPaymentAsync(It.IsAny<BankPaymentRequest>()))
            .ReturnsAsync(Either<PaymentError, BankPaymentResponse>.Left(
                new PaymentError.BankUnavailable()));

        // Act
        var result = await _service.ProcessPaymentAsync(new CreatePaymentRequestBuilder().Build());

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.ShouldBeOfType<PaymentError.BankUnavailable>());
    }

    [Fact]
    public async Task GivenValidRequest_WhenProcessPayment_ThenMasksCardNumber()
    {
        // Arrange
        _mockBankClient
            .Setup(x => x.ProcessPaymentAsync(It.IsAny<BankPaymentRequest>()))
            .ReturnsAsync(Either<PaymentError, BankPaymentResponse>.Right(
                new BankPaymentResponse(true, "auth-123")));

        var request = new CreatePaymentRequestBuilder()
            .WithCardNumber("12345678901234")
            .Build();

        // Act
        var result = await _service.ProcessPaymentAsync(request);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(response =>
        {
            response.CardNumberLastFour.ShouldBe("1234");
        });
    }

    [Fact]
    public void GivenExistingPayment_WhenGetPayment_ThenReturnsPayment()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockRepository.Setup(r => r.Get(id)).Returns(new Payment
        {
            Id = id,
            Status = Core.Enums.PaymentStatus.Authorized,
            CardNumberLastFour = "8877",
            ExpiryMonth = 4,
            ExpiryYear = 2025,
            Currency = "GBP",
            Amount = 100,
            Authorised = true,
            AuthorizationCode = "1234",
            DateCreated = DateTimeOffset.UtcNow
        }
        );

        // Act
        var result = _service.GetPayment(id);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(response =>
        {
            response.Id.ShouldBe(id);
            response.Status.ShouldBe(PaymentResponseStatus.Authorized);
            response.CardNumberLastFour.ShouldBe("8877");
        });
    }

    [Fact]
    public void GivenNonExistingPayment_WhenGetPayment_ThenReturnsNotFoundError()
    {
        // Arrange
        _mockRepository.Setup(r => r.Get(It.IsAny<Guid>())).Returns((Payment?)null);

        // Act
        var result = _service.GetPayment(Guid.NewGuid());

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.ShouldBeOfType<PaymentError.NotFound>());
    }
}