using FluentAssertions;
using Moq;
using TaskManagement.Application.Abstractions.Persistence;
using TaskManagement.Application.Abstractions.Services;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.Features.Tasks.Commands.ChangeTaskStatus;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Domain.Exceptions;
using Xunit;

namespace TaskManagement.Application.UnitTests.Tasks;

public class ChangeTaskStatusCommandHandlerTests
{
    private readonly Mock<ITaskRepository> _taskRepositoryMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    private readonly Guid _creatorId = Guid.NewGuid();
    private readonly Guid _assigneeId = Guid.NewGuid();

    private ChangeTaskStatusCommandHandler CreateHandler()
        => new(_taskRepositoryMock.Object, _currentUserServiceMock.Object, _unitOfWorkMock.Object);

    private TaskItem CreateExistingTask()
        => TaskItem.Create("Görev", "Açıklama", TaskPriority.Medium,
            DateTime.UtcNow.AddDays(5), _creatorId, _assigneeId);

    [Fact]
    public async Task Handle_StartProgress_ShouldTransitionStatus_WhenCalledByAssignee()
    {
        var task = CreateExistingTask();
        _taskRepositoryMock.Setup(x => x.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_assigneeId);

        var handler = CreateHandler();
        var command = new ChangeTaskStatusCommand { TaskId = task.Id, Action = TaskStatusAction.StartProgress };

        await handler.Handle(command, CancellationToken.None);

        task.Status.Should().Be(TaskStatusEnum.InProgress);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Approve_ShouldThrowUnauthorized_WhenCalledByAssigneeNotCreator()
    {
        var task = CreateExistingTask();
        task.StartProgress(_assigneeId);
        task.SubmitForReview(_assigneeId);

        _taskRepositoryMock.Setup(x => x.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_assigneeId); // creator değil

        var handler = CreateHandler();
        var command = new ChangeTaskStatusCommand { TaskId = task.Id, Action = TaskStatusAction.Approve };

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedTaskOperationException>();
        task.Status.Should().Be(TaskStatusEnum.InReview);
    }

    [Fact]
    public async Task Handle_Approve_ShouldTransitionToCompleted_WhenCalledByCreator()
    {
        var task = CreateExistingTask();
        task.StartProgress(_assigneeId);
        task.SubmitForReview(_assigneeId);

        _taskRepositoryMock.Setup(x => x.GetByIdAsync(task.Id, It.IsAny<CancellationToken>())).ReturnsAsync(task);
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_creatorId);

        var handler = CreateHandler();
        var command = new ChangeTaskStatusCommand { TaskId = task.Id, Action = TaskStatusAction.Approve };

        await handler.Handle(command, CancellationToken.None);

        task.Status.Should().Be(TaskStatusEnum.Completed);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFound_WhenTaskDoesNotExist()
    {
        _taskRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaskItem?)null);

        var handler = CreateHandler();
        var command = new ChangeTaskStatusCommand { TaskId = Guid.NewGuid(), Action = TaskStatusAction.StartProgress };

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
