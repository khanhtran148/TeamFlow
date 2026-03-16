using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Errors;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Domain.Events;

namespace TeamFlow.Application.Features.Retros.CreateRetroActionItem;

public sealed class CreateRetroActionItemHandler(
    IRetroSessionRepository retroRepo,
    IWorkItemRepository workItemRepo,
    ICurrentUser currentUser,
    IPermissionChecker permissions,
    IPublisher publisher)
    : IRequestHandler<CreateRetroActionItemCommand, Result<RetroActionItemDto>>
{
    public async Task<Result<RetroActionItemDto>> Handle(CreateRetroActionItemCommand request, CancellationToken ct)
    {
        var session = await retroRepo.GetByIdAsync(request.SessionId, ct);
        if (session is null)
            return DomainError.NotFound<RetroActionItemDto>("Retro session not found");

        if (!await permissions.HasPermissionAsync(currentUser.Id, session.ProjectId, Permission.Retro_Facilitate, ct))
            return DomainError.Forbidden<RetroActionItemDto>();

        var actionItem = new RetroActionItem
        {
            SessionId = request.SessionId,
            CardId = request.CardId,
            Title = request.Title,
            Description = request.Description,
            AssigneeId = request.AssigneeId,
            DueDate = request.DueDate
        };

        await retroRepo.AddActionItemAsync(actionItem, ct);

        if (request.LinkToBacklog)
        {
            var workItem = new WorkItem
            {
                ProjectId = session.ProjectId,
                Type = WorkItemType.Task,
                Title = request.Title,
                Description = request.Description,
                RetroActionItemId = actionItem.Id
            };

            var created = await workItemRepo.AddAsync(workItem, ct);
            actionItem.LinkedTaskId = created.Id;
            await retroRepo.UpdateActionItemAsync(actionItem, ct);
        }

        await publisher.Publish(new RetroActionItemCreatedDomainEvent(
            session.Id, actionItem.Id, actionItem.Title, actionItem.AssigneeId, currentUser.Id), ct);

        return Result.Success(RetroMapper.ToActionItemDto(actionItem));
    }
}
