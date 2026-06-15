using TaskManagement.Domain.Common;

namespace TaskManagement.Domain.Events;

public sealed record TaskCreatedEvent(Guid TaskId, Guid UserId) : IDomainEvent;
public sealed record TaskUpdatedEvent(Guid TaskId, Guid UserId) : IDomainEvent;
public sealed record TaskStartedProgressEvent(Guid TaskId, Guid UserId) : IDomainEvent;
public sealed record TaskSubmittedForReviewEvent(Guid TaskId, Guid UserId) : IDomainEvent;
public sealed record TaskApprovedEvent(Guid TaskId, Guid UserId) : IDomainEvent;
public sealed record TaskReviewRejectedEvent(Guid TaskId, Guid UserId) : IDomainEvent;
public sealed record TaskTodoItemAddedEvent(Guid TaskId, string TodoTitle, Guid UserId) : IDomainEvent;
public sealed record TaskTodoItemToggledEvent(Guid TaskId, string TodoTitle, bool IsChecked, Guid UserId) : IDomainEvent;

public record TaskCommentAddedEvent(Guid TaskId, Guid UserId, string Comment) : IDomainEvent;