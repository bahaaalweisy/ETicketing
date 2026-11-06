using FluentValidation;

namespace ETicketing.Application.Payments;

public sealed class CheckoutRequestValidator : AbstractValidator<CheckoutRequest>
{
    public CheckoutRequestValidator()
    {
        RuleFor(request => request.OrderId).NotEmpty();
        RuleFor(request => request.PaymentMethod).IsInEnum();
    }
}
