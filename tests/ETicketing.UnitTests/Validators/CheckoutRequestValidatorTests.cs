using ETicketing.Application.Payments;
using ETicketing.Domain.Enums;
using FluentAssertions;

namespace ETicketing.UnitTests.Validators;

public class CheckoutRequestValidatorTests
{
    private readonly CheckoutRequestValidator _validator = new();

    [Theory]
    [InlineData(PaymentMethod.CreditCard)]
    [InlineData(PaymentMethod.QR)]
    public void Accepts_every_defined_payment_method(PaymentMethod method)
    {
        var result = _validator.Validate(new CheckoutRequest(Guid.NewGuid(), method));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    public void Rejects_a_missing_or_undefined_payment_method(int method)
    {
        var result = _validator.Validate(new CheckoutRequest(Guid.NewGuid(), (PaymentMethod)method));

        result.Errors.Should().ContainSingle(error => error.PropertyName == nameof(CheckoutRequest.PaymentMethod));
    }

    [Fact]
    public void Rejects_an_empty_order_id()
    {
        var result = _validator.Validate(new CheckoutRequest(Guid.Empty, PaymentMethod.QR));

        result.Errors.Should().ContainSingle(error => error.PropertyName == nameof(CheckoutRequest.OrderId));
    }
}
