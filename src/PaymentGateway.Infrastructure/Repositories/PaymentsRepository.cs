using System.Collections.Concurrent;

using PaymentGateway.Core.Entities;
using PaymentGateway.Core.Interfaces;

namespace PaymentGateway.Infrastructure.Repositories;

public class PaymentsRepository : IPaymentsRepository
{
    private readonly ConcurrentDictionary<Guid, Payment> _payments = new();

    public void Add(Payment payment)
    {
        _payments[payment.Id] = payment;
    }

    public Payment? Get(Guid id)
    {
        _payments.TryGetValue(id, out var payment);
        return payment;
    }
}