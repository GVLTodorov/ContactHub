using System.Text.Json;
using ContactHub.Api.Controllers;
using ContactHub.Api.Middleware;
using ContactHub.Application.Abstractions;
using ContactHub.Application.Exceptions;
using ContactHub.Domain.Entities;
using ContactHub.Domain.Exceptions;
using ContactHub.Domain.ValueObjects;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ContactHub.Api.Tests;

[TestFixture]
public class ExceptionHandlerTests
{
    private ServiceProvider _provider = null!;

    [SetUp]
    public void SetUp()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddProblemDetails();
        _provider = services.BuildServiceProvider();
    }

    [TearDown]
    public void TearDown() => _provider.Dispose();

    private DefaultHttpContext NewContext() => new()
    {
        RequestServices = _provider,
        Response = { Body = new MemoryStream() },
    };

    private static async Task<JsonElement> BodyAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        var json = await JsonDocument.ParseAsync(context.Response.Body);

        return json.RootElement.Clone();
    }

    [Test]
    public async Task Validation_FluentValidationException_WritesGroupedCamelCaseErrors()
    {
        var handler = new ValidationExceptionHandler(_provider.GetRequiredService<IProblemDetailsService>());
        var context = NewContext();
        var exception = new ValidationException(
        [
            new ValidationFailure("Address.City", "City is required."),
            new ValidationFailure("Address.City", "City is required."),
            new ValidationFailure("Address.City", "City too long."),
            new ValidationFailure("FirstName", "First name is required."),
        ]);

        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);
        var body = await BodyAsync(context);

        Assert.That(handled, Is.True);
        Assert.That(context.Response.StatusCode, Is.EqualTo(400));
        Assert.That(body.GetProperty("errors").GetProperty("address.city").GetArrayLength(), Is.EqualTo(2));
        Assert.That(body.GetProperty("errors").TryGetProperty("firstName", out _), Is.True);
        Assert.That(body.GetProperty("status").GetInt32(), Is.EqualTo(400));
    }

    [Test]
    public async Task Validation_DomainException_WritesItsErrors()
    {
        var handler = new ValidationExceptionHandler(_provider.GetRequiredService<IProblemDetailsService>());
        var context = NewContext();
        var exception = new DomainValidationException(new Dictionary<string, string[]> { ["iban"] = ["IBAN is not valid."] });

        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);
        var body = await BodyAsync(context);

        Assert.That(handled, Is.True);
        Assert.That(context.Response.StatusCode, Is.EqualTo(400));
        Assert.That(body.GetProperty("errors").GetProperty("iban")[0].GetString(), Is.EqualTo("IBAN is not valid."));
    }

    [Test]
    public async Task Validation_OtherException_IsNotHandled()
    {
        var handler = new ValidationExceptionHandler(_provider.GetRequiredService<IProblemDetailsService>());
        var context = NewContext();

        var handled = await handler.TryHandleAsync(context, new InvalidOperationException(), CancellationToken.None);

        Assert.That(handled, Is.False);
        Assert.That(context.Response.StatusCode, Is.EqualTo(200));
    }

    [Test]
    public async Task NotFound_WritesProblemDetails()
    {
        var handler = new NotFoundExceptionHandler(_provider.GetRequiredService<IProblemDetailsService>());
        var context = NewContext();
        var id = Guid.NewGuid();

        var handled = await handler.TryHandleAsync(context, new NotFoundException("Contact", id), CancellationToken.None);
        var body = await BodyAsync(context);

        Assert.That(handled, Is.True);
        Assert.That(context.Response.StatusCode, Is.EqualTo(404));
        Assert.That(body.GetProperty("title").GetString(), Is.EqualTo("Not Found"));
        Assert.That(body.GetProperty("detail").GetString(), Does.Contain(id.ToString()));
    }

    [Test]
    public async Task NotFound_OtherException_IsNotHandled()
    {
        var handler = new NotFoundExceptionHandler(_provider.GetRequiredService<IProblemDetailsService>());
        var context = NewContext();

        var handled = await handler.TryHandleAsync(context, new InvalidOperationException(), CancellationToken.None);

        Assert.That(handled, Is.False);
        Assert.That(context.Response.StatusCode, Is.EqualTo(200));
    }
}
