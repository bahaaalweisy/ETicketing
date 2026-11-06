using ETicketing.Application.Abstractions;
using ETicketing.Domain.Entities;
using ETicketing.Domain.Enums;
using ETicketing.Domain.Exceptions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ETicketing.Application.Payments;

public sealed class CheckoutService(
    IAppDbContext db,
    IPaymentHandlerResolver handlerResolver,
    IClock clock,
    IValidator<CheckoutRequest> validator)
{
    public async Task<CheckoutResponse> CheckoutAsync(CheckoutRequest request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var order = await db.Orders.AsNoTracking().SingleOrDefaultAsync(order => order.Id == request.OrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        order.EnsureCanCheckout(clock.UtcNow);

        var handler = handlerResolver.Resolve(request.PaymentMethod);
        var result = await handler.ProcessAsync(new PaymentContext(order.Id, order.TotalAmount), cancellationToken);

        if (result.Status == TransactionStatus.Failed)
        {
            throw new PaymentFailedException(result.Message);
        }

        return new CheckoutResponse(order.Id, result.TransactionId!.Value, result.Status, result.Reference!, result.Message);
    }
}
