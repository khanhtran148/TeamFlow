using CSharpFunctionalExtensions;
using FluentValidation;
using MediatR;
using TeamFlow.Application.Common.Behaviors;

namespace TeamFlow.Application.Tests;

public sealed class ValidationBehaviorTests
{
    public sealed record TestCommand(string? Title) : IRequest<Result<string>>;

    public sealed class TestCommandValidator : AbstractValidator<TestCommand>
    {
        public TestCommandValidator()
        {
            RuleFor(c => c.Title).NotEmpty().WithMessage("Title is required");
            RuleFor(c => c.Title).MaximumLength(100).WithMessage("Title too long");
        }
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldCallNext()
    {
        var validators = new List<IValidator<TestCommand>> { new TestCommandValidator() };
        var behavior = new ValidationBehavior<TestCommand, Result<string>>(validators);
        var command = new TestCommand("Valid Title");
        var nextCalled = false;

        RequestHandlerDelegate<Result<string>> next = (ct) =>
        {
            nextCalled = true;
            return Task.FromResult(Result.Success("OK"));
        };

        var result = await behavior.Handle(command, next, CancellationToken.None);

        Assert.True(nextCalled);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_InvalidRequest_ShouldReturnFailure()
    {
        var validators = new List<IValidator<TestCommand>> { new TestCommandValidator() };
        var behavior = new ValidationBehavior<TestCommand, Result<string>>(validators);
        var command = new TestCommand(null);
        var nextCalled = false;

        RequestHandlerDelegate<Result<string>> next = (ct) =>
        {
            nextCalled = true;
            return Task.FromResult(Result.Success("Should not reach here"));
        };

        var result = await behavior.Handle(command, next, CancellationToken.None);

        Assert.False(nextCalled);
        Assert.True(result.IsFailure);
        Assert.Contains("Title is required", result.Error);
    }

    [Fact]
    public async Task Handle_NoValidators_ShouldCallNext()
    {
        var validators = Enumerable.Empty<IValidator<TestCommand>>();
        var behavior = new ValidationBehavior<TestCommand, Result<string>>(validators);
        var command = new TestCommand(null);
        var nextCalled = false;

        RequestHandlerDelegate<Result<string>> next = (ct) =>
        {
            nextCalled = true;
            return Task.FromResult(Result.Success("OK"));
        };

        var result = await behavior.Handle(command, next, CancellationToken.None);

        Assert.True(nextCalled);
    }
}
