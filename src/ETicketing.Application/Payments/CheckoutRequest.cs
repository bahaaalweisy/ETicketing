using ETicketing.Domain.Enums;

namespace ETicketing.Application.Payments;

/// <summary>
/// Body of POST /api/checkout.
/// </summary>
public sealed record CheckoutRequest(Guid OrderId, PaymentMethod PaymentMethod);
