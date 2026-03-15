using CSharpFunctionalExtensions;
using FluentValidation;
using MediatR;
using NSubstitute;
using TeamFlow.Application.Common.Behaviors;

namespace TeamFlow.Tests.Application;

/// <summary>
/// Tests for the MediatR ValidationBehavior pipeline.
/// TFD: Written before implementation (now verifying behavior).
/// </summary>
public class ValidationBehaviorTests
{
    public record TestCommand(string? Title) : IRequest<Result<string>>;

    public class TestCommandValidator : AbstractValidator<TestCommand>
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
        // Arrange
        var validators = new List<IValidator<TestCommand>> { new TestCommandValidator() };
        var behavior = new ValidationBehavior<TestCommand, Result<string>>(validators);
        var command = new TestCommand("Valid Title");
        var nextCalled = false;

        RequestHandlerDelegate<Result<string>> next = (ct) =>
        {
            nextCalled = true;
            return Task.FromResult(Result.Success("OK"));
        };

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        Assert.True(nextCalled);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_InvalidRequest_ShouldReturnFailure()
    {
        // Arrange
        var validators = new List<IValidator<TestCommand>> { new TestCommandValidator() };
        var behavior = new ValidationBehavior<TestCommand, Result<string>>(validators);
        var command = new TestCommand(null); // Invalid — title is null
        var nextCalled = false;

        RequestHandlerDelegate<Result<string>> next = (ct) =>
        {
            nextCalled = true;
            return Task.FromResult(Result.Success("Should not reach here"));
        };

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        Assert.False(nextCalled);
        Assert.True(result.IsFailure);
        Assert.Contains("Title is required", result.Error);
    }

    [Fact]
    public async Task Handle_NoValidators_ShouldCallNext()
    {
        // Arrange
        var validators = Enumerable.Empty<IValidator<TestCommand>>();
        var behavior = new ValidationBehavior<TestCommand, Result<string>>(validators);
        var command = new TestCommand(null); // Would fail validation, but no validators
        var nextCalled = false;

        RequestHandlerDelegate<Result<string>> next = (ct) =>
        {
            nextCalled = true;
            return Task.FromResult(Result.Success("OK"));
        };

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        Assert.True(nextCalled);
    }
}
