using PaymentGateway.Core.Entities;

namespace PaymentGateway.Core.Interfaces;

public interface IPaymentsRepository
{
    void Add(Payment payment);
    Payment? Get(Guid id);
}