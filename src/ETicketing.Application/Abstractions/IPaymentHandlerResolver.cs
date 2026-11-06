using ETicketing.Domain.Enums;

namespace ETicketing.Application.Abstractions;

public interface IPaymentHandlerResolver
{
    IPaymentHandler Resolve(PaymentMethod method);
}
