using ETicketing.Domain.Enums;

namespace ETicketing.Application.Payments;

public sealed record CheckoutRequest(Guid OrderId, PaymentMethod PaymentMethod);
