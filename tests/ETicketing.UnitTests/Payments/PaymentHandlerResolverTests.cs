using ETicketing.Application.Abstractions;
using ETicketing.Application.Payments;
using ETicketing.Domain.Enums;
using ETicketing.Domain.Exceptions;
using FluentAssertions;
using Moq;

namespace ETicketing.UnitTests.Payments;

public class PaymentHandlerResolverTests
{
    [Fact]
    public void Resolves_the_handler_registered_for_the_method()
    {
        var creditCard = HandlerFor(PaymentMethod.CreditCard);
        var qr = HandlerFor(PaymentMethod.QR);
        var resolver = new PaymentHandlerResolver([creditCard, qr]);

        resolver.Resolve(PaymentMethod.QR).Should().BeSameAs(qr);
    }

    [Fact]
    public void Throws_for_a_method_without_a_handler()
    {
        var resolver = new PaymentHandlerResolver([HandlerFor(PaymentMethod.CreditCard)]);

        var act = () => resolver.Resolve(PaymentMethod.QR);

        act.Should().Throw<UnsupportedPaymentMethodException>();
    }

    private static IPaymentHandler HandlerFor(PaymentMethod method)
    {
        var handler = new Mock<IPaymentHandler>();
        handler.Setup(item => item.Method).Returns(method);

        return handler.Object;
    }
}
