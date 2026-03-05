using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using PaymentGateway.Application.Services;
using PaymentGateway.Core.Interfaces;
using PaymentGateway.Infrastructure.Clients;
using PaymentGateway.Infrastructure.Repositories;

namespace PaymentGateway.Infrastructure.Extensions;

public static class ServiceRegistrations
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfigurationManager configurationManager)
    {
        services.AddSingleton<IPaymentsRepository, PaymentsRepository>();
        services.AddScoped<IPaymentService, PaymentService>();

        var bankBaseUrl = configurationManager["BankSimulator:BaseUrl"]!;
        services.AddHttpClient<IBankClient, BankClient>(client =>
        {
            client.BaseAddress = new Uri(bankBaseUrl);
        });
        return services;
    }
}