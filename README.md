# Payment Gateway - Documentation

## Architecture Overview

The solution follows a **clean/layered architecture** with clear separation of concerns:

```
src/
    PaymentGateway.Api            - ASP.NET Core Web API (controllers, filters, DI setup)
    PaymentGateway.Application    - Business logic and services
    PaymentGateway.Contracts      - DTOs and shared request/response models
    PaymentGateway.Core           - Domain entities, enums, interfaces, and error types
    PaymentGateway.Infrastructure - External integrations (bank client, repository)
test/
    PaymentGateway.Api.Tests         - Integration tests (real HTTP + Testcontainers)
    PaymentGateway.Application.Tests - Unit tests (Moq-based)
    PaymentGateway.Testing           - Shared test utilities (builders, fixtures)
```

I have tried to not overengineer as mentioned in the description of the test by not introducing techniques such as CQRS, DDD, or using packages such as MediatR to try and keep the approach as lightweight as possible.

## Key Design Decisions

### Functional Error Handling with `Either<TLeft, TRight>`
Instead of throwing exceptions for expected failures, the application uses **LanguageExt**'s `Either` monad. `Left` represents a typed `PaymentError`, `Right` represents success. This makes error paths explicit and composable - the controller uses `.Match()` to map errors to the appropriate HTTP status codes (404, 503, etc.).

### Validation via FluentValidation + Action Filter
A `ValidationFilter` intercepts requests before they reach the controller. `CreatePaymentRequestValidator` enforces rules declaratively:
- Card number: 14–19 numeric digits
- Expiry: must be current month or future
- Currency: whitelist (GBP, USD, EUR)
- Amount: greater than zero
- CVV: 3–4 numeric digits

Invalid requests return `400 Bad Request` with a `RejectedPaymentResponse` containing field-level errors.

This could also be moved to the service layer if we foresee there will be other ports/consumers of this layer to ensure validation lower down the layers.

### PCI Compliance Considerations
Full card numbers are accepted in requests but **never stored or logged unmasked**. Only the last four digits are persisted in the `Payment` entity. The validation filter masks card numbers in log output.

### In-Memory Repository (Singleton)
`PaymentsRepository` uses a `ConcurrentDictionary<Guid, Payment>` for thread-safe storage. Registered as a singleton so data persists across requests during application lifetime. This is intentionally simple - in production this would be replaced with a database/persistent store.

### Bank Client Resilience
`BankClient` wraps HTTP calls to the bank simulator. Any non-success status code or network exception is caught and returned as `PaymentError.BankUnavailable`, which maps to `503 Service Unavailable`. This prevents bank failures from crashing the gateway. Polly or other Resiliency has not been implemented to keep the solution focused and light.

### Global Exception Filter
`GlobalExceptionFilter` catches any unhandled exceptions, logs them, and returns a clean `500 Internal Server Error` - preventing stack traces from leaking to clients.

### Logging
Currently logging to the console, but could be switched out to use structured logging and logging to AppInsights/DataDog/Loki etc. At the moment it is logging warnings and errors, but could be expanded if required

## Assumptions

1. **No authentication added** - this is for simplicity and the gateway is assumed to sit behind an API gateway or internal network that handles auth.
2. **Data does not need to persist across restarts** - in-memory storage is acceptable for this exercise.
3. **Supported currencies are fixed** - only GBP, USD, and EUR are accepted. A production system would likely make this configurable.
4. **Card expiry is inclusive of the current month** - a card expiring this month is still valid.
5. **Amount is an integer** - represents the smallest currency unit (e.g., pence/cents), avoiding floating-point precision issues.
6. **Idempotency not implemented** - Considered overkill for this exercise
7. **No versioning implemented** - No versioning has been implemented for the APIs to keep the exercise simple

## API Endpoints

| Method | Endpoint | Success | Failure |
|--------|----------|---------|---------|
| `POST /api/payments` | Process a payment | `200` with payment details | `400` validation error, `503` bank unavailable |
| `GET /api/payments/{id}` | Retrieve a payment | `200` with payment details | `404` not found |

## Running the Application

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) (for the bank simulator)

### Start the bank simulator
```bash
docker-compose up -d
```
This starts a Mountebank bank simulator on port `8080`.

### Run the API
```bash
dotnet run --project src/PaymentGateway.Api
```
The API will be available at `https://localhost:7092` (or `http://localhost:5067`). Swagger UI is available at `/swagger`.

### Run the tests
```bash
# Unit tests
dotnet test test/PaymentGateway.Application.Tests

# Integration tests (requires Docker - Testcontainers will start the bank simulator automatically), ensure the port 8080 for the bank simulator is not currently in use
dotnet test test/PaymentGateway.Api.Tests

# All tests
dotnet test
```

## Testing Strategy

- **Unit tests** validate business logic and all validation rules in isolation using Moq.
- **Integration tests** spin up the full API and a real Mountebank bank simulator via **Testcontainers**, testing actual HTTP flows end-to-end following an Outside In approach to testing
- **Shared test utilities** provide a fluent `CreatePaymentRequestBuilder` for consistent test data.

## Production Considerations

If this were a production service, the following would be worth introducing:

- **Resilience** - Polly for retry policies, circuit breakers, and timeouts on the bank client
- **Persistent storage** - Replace the in-memory repository with a database (e.g., PostgreSQL via EF Core)
- **Orchestration** - .NET Aspire for service discovery, orchestration, and built-in telemetry across distributed services
- **Structured logging** - Serilog or OpenTelemetry exporting to AppInsights/Datadog/Loki for observability
- **Authentication** - API key or OAuth/JWT validation at the gateway level
- **Idempotency** - Idempotency keys on POST requests to prevent duplicate payments
- **API versioning** - URL or header-based versioning to allow non-breaking API evolution
- **Rate limiting** - To protect against abuse and ensure fair usage

# Instructions for candidates

This is the .NET version of the Payment Gateway challenge. If you haven't already read this [README.md](https://github.com/cko-recruitment/) on the details of this exercise, please do so now. 

## Template structure
```
src/
    PaymentGateway.Api - a skeleton ASP.NET Core Web API
test/
    PaymentGateway.Api.Tests - an empty xUnit test project
imposters/ - contains the bank simulator configuration. Don't change this

.editorconfig - don't change this. It ensures a consistent set of rules for submissions when reformatting code
docker-compose.yml - configures the bank simulator
PaymentGateway.sln
```

Feel free to change the structure of the solution, use a different test library etc.