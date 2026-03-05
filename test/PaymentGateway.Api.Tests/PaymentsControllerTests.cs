using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using PaymentGateway.Api.Tests.Fixtures;
using PaymentGateway.Contracts.Payments;
using PaymentGateway.Testing.Builders;

using Shouldly;

namespace PaymentGateway.Api.Tests;

public class PaymentsControllerTests(BankSimulatorFixture fixture) : IClassFixture<BankSimulatorFixture>
{
    private const string PaymentsUrl = "/api/payments/";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Theory]
    [InlineData("2222405343248871", PaymentResponseStatus.Authorized)]
    [InlineData("2222405343248873", PaymentResponseStatus.Authorized)]
    [InlineData("2222405343248875", PaymentResponseStatus.Authorized)]
    [InlineData("2222405343248877", PaymentResponseStatus.Authorized)]
    [InlineData("2222405343248879", PaymentResponseStatus.Authorized)]
    [InlineData("2222405343248872", PaymentResponseStatus.Declined)]
    [InlineData("2222405343248874", PaymentResponseStatus.Declined)]
    [InlineData("2222405343248876", PaymentResponseStatus.Declined)]
    [InlineData("2222405343248878", PaymentResponseStatus.Declined)]
    public async Task GivenCardNumber_WhenPostPayment_ThenReturnsExpectedStatus(
        string cardNumber,
        PaymentResponseStatus expectedStatus)
    {
        // Arrange
        var client = fixture.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;
        var request = new CreatePaymentRequestBuilder()
            .WithCardNumber(cardNumber)
            .Build();

        // Act
        var response = await client.PostAsJsonAsync(PaymentsUrl, request, cancellationToken);
        var paymentResponse = await response.Content.ReadFromJsonAsync<GetPaymentResponse>(JsonOptions, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        paymentResponse.ShouldNotBeNull();
        paymentResponse.Status.ShouldBe(expectedStatus);
        paymentResponse.CardNumberLastFour.ShouldBe(cardNumber[^4..]);
    }

    [Fact]
    public async Task GivenInvalidCardNumber_WhenPostPayment_ThenReturnsBadRequestWithRejected()
    {
        // Arrange
        var client = fixture.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        var request = new CreatePaymentRequestBuilder()
            .WithCardNumber("123")
            .Build();

        // Act
        var response = await client.PostAsJsonAsync(PaymentsUrl, request, cancellationToken);
        var paymentResponse = await response.Content.ReadFromJsonAsync<RejectedPaymentResponse>(JsonOptions, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        paymentResponse.ShouldNotBeNull();
        paymentResponse.Status.ShouldBe(PaymentResponseStatus.Rejected);
        paymentResponse.Errors.ShouldContain(e => e.Field == "CardNumber" && e.Message == "Card number must be between 14 and 19 numeric digits.");
    }

    [Fact]
    public async Task GivenExpiredCard_WhenPostPayment_ThenReturnsBadRequestWithRejected()
    {
        // Arrange
        var client = fixture.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        var request = new CreatePaymentRequestBuilder()
            .WithExpiryMonth(1)
            .WithExpiryYear(2020)
            .Build();

        // Act
        var response = await client.PostAsJsonAsync(PaymentsUrl, request, cancellationToken);
        var paymentResponse = await response.Content.ReadFromJsonAsync<RejectedPaymentResponse>(JsonOptions, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        paymentResponse.ShouldNotBeNull();
        paymentResponse.Status.ShouldBe(PaymentResponseStatus.Rejected);
        paymentResponse.Errors.ShouldContain(e => e.Message == "Card expiry date must be in the future.");
    }

    [Fact]
    public async Task GivenInvalidCvv_WhenPostPayment_ThenReturnsBadRequestWithRejected()
    {
        // Arrange
        var client = fixture.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        var request = new CreatePaymentRequestBuilder()
            .WithCvv("12")
            .Build();

        // Act
        var response = await client.PostAsJsonAsync(PaymentsUrl, request, cancellationToken);
        var paymentResponse = await response.Content.ReadFromJsonAsync<RejectedPaymentResponse>(JsonOptions, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        paymentResponse.ShouldNotBeNull();
        paymentResponse.Status.ShouldBe(PaymentResponseStatus.Rejected);
        paymentResponse.Errors.ShouldContain(e => e.Field == "Cvv" && e.Message == "CVV must be 3 or 4 numeric digits.");
    }

    [Fact]
    public async Task GivenBankEndsWithZero_WhenPostPayment_ThenReturnsServiceUnavailable()
    {
        // Arrange
        var client = fixture.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        var request = new CreatePaymentRequestBuilder()
            .WithCardNumber("2222405343248870")
            .Build();

        // Act
        var response = await client.PostAsJsonAsync(PaymentsUrl, request, cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task GivenExistingPayment_WhenGetPayment_ThenReturnsOk()
    {
        // Arrange
        var client = fixture.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;
        var postRequest = new CreatePaymentRequestBuilder().Build();

        var postResponse = await client.PostAsJsonAsync(PaymentsUrl, postRequest, cancellationToken);
        var postResult = await postResponse.Content.ReadFromJsonAsync<GetPaymentResponse>(JsonOptions, cancellationToken);

        // Act
        var getResponse = await client.GetFromJsonAsync<GetPaymentResponse>($"{PaymentsUrl}{postResult!.Id}", JsonOptions, cancellationToken);

        // Assert
        getResponse.ShouldNotBeNull();
        getResponse.Id.ShouldBe(postResult.Id);
        getResponse.Status.ShouldBe(PaymentResponseStatus.Authorized);
        getResponse.CardNumberLastFour.ShouldBe("8877");
        getResponse.ExpiryMonth.ShouldBe(4);
        getResponse.Currency.ShouldBe("GBP");
        getResponse.Amount.ShouldBe(100);
    }

    [Fact]
    public async Task GivenNonExistingPayment_WhenGetPayment_ThenReturnsNotFound()
    {
        // Arrange
        var client = fixture.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var response = await client.GetAsync($"{PaymentsUrl}{Guid.NewGuid()}", cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}