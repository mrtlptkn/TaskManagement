using FluentAssertions;
using Moq;
using TaskManagement.Application.Abstractions.Persistence;
using TaskManagement.Application.Abstractions.Services;
using TaskManagement.Application.Features.Tasks.Commands.CreateTask;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using Xunit;

namespace TaskManagement.Application.UnitTests.Tasks;

public class CreateTaskCommandHandlerTests
{
    private readonly Mock<ITaskRepository> _taskRepositoryMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Guid _currentUserId = Guid.NewGuid();

    public CreateTaskCommandHandlerTests()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_currentUserId);
    }

    private CreateTaskCommandHandler CreateHandler()
        => new(_taskRepositoryMock.Object, _currentUserServiceMock.Object, _unitOfWorkMock.Object);

    [Fact]
    public async Task Handle_ShouldCallAddAsyncAndSaveChanges_WhenCommandIsValid()
    {
        var command = new CreateTaskCommand
        {
            Title = "Yeni Görev",
            Description = "Açıklama",
            Priority = TaskPriority.High,
            DeadLine = DateTime.UtcNow.AddDays(3),
            AssignedToUserId = Guid.NewGuid()
        };

        var handler = CreateHandler();

        var taskId = await handler.Handle(command, CancellationToken.None);

        taskId.Should().NotBe(Guid.Empty);

        _taskRepositoryMock.Verify(
            x => x.AddAsync(It.Is<TaskItem>(t =>
                t.Title == command.Title &&
                t.CreatedByUserId == _currentUserId &&
                t.AssignedToUserId == command.AssignedToUserId &&
                t.Priority == command.Priority), It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldAddTodoItems_WhenProvidedInCommand()
    {
        var command = new CreateTaskCommand
        {
            Title = "Görev",
            Description = "Açıklama",
            Priority = TaskPriority.Medium,
            DeadLine = DateTime.UtcNow.AddDays(2),
            AssignedToUserId = Guid.NewGuid(),
            TodoItems = new List<string> { "Adım 1", "Adım 2", "Adım 3" }
        };

        TaskItem? capturedTask = null;
        _taskRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()))
            .Callback<TaskItem, CancellationToken>((task, _) => capturedTask = task)
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();

        await handler.Handle(command, CancellationToken.None);

        capturedTask.Should().NotBeNull();
        capturedTask!.TodoItems.Should().HaveCount(3);
        capturedTask.TodoItems.Select(t => t.Title).Should().BeEquivalentTo(command.TodoItems);
    }
}
