using BuildingBlocks.Application;
using BuildingBlocks.Domain;
using BuildingBlocks.Infrastructure.DependencyInjection;
using BuildingBlocks.Infrastructure.Dispatching;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.Tests;

public sealed class FailureTranslationTests
{
    [Fact]
    public async Task DomainValidationException_IsTranslatedToValidationFailure()
    {
        var result = await SendThrowing(new DomainValidationException("Name must not be empty."));

        Assert.True(result.IsFailure);
        var failure = Assert.Single(result.Failures);
        Assert.Equal(FailureCategory.Validation, failure.Category);
        Assert.Equal(ExceptionToResultBehavior<ThrowingCommand, Result>.ValidationFailureCode, failure.Code);
    }

    [Fact]
    public async Task BusinessRuleViolationException_IsTranslatedToBusinessRuleFailure()
    {
        var result = await SendThrowing(new BusinessRuleViolationException("Recipe already published."));

        Assert.True(result.IsFailure);
        var failure = Assert.Single(result.Failures);
        Assert.Equal(FailureCategory.BusinessRule, failure.Category);
        Assert.Equal(ExceptionToResultBehavior<ThrowingCommand, Result>.BusinessRuleFailureCode, failure.Code);
    }

    private static async Task<Result> SendThrowing(Exception exception)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<IUnitOfWork, NoOpUnitOfWork>();
        services.AddScoped<ICommandHandler<ThrowingCommand>>(_ => new ThrowingCommandHandler(exception));
        services.AddBuildingBlocks(_ => { });

        using var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new ThrowingCommand(), CancellationToken.None).ConfigureAwait(false);
        return result;
    }

    private sealed record ThrowingCommand : ICommand;

    private sealed class ThrowingCommandHandler(Exception exception) : ICommandHandler<ThrowingCommand>
    {
        public Task<Result> Handle(ThrowingCommand command, CancellationToken cancellationToken) => throw exception;
    }

    private sealed class NoOpUnitOfWork : IUnitOfWork
    {
        public Task CommitAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
