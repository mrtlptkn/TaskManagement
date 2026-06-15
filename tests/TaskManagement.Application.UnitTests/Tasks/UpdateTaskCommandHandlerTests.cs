using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TaskManagement.Application.Abstractions.Persistence;
using TaskManagement.Application.Abstractions.Services;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.Features.Tasks.Commands.UpdateTask;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Domain.Exceptions;
using Xunit;

namespace TaskManagement.Application.UnitTests.Tasks;

/// <summary>
/// Resource Based Authorization akışını handler seviyesinde doğrulayan testler.
/// Sadece görevi oluşturan kullanıcı (CreatedByUserId) güncelleme yapabilmelidir.
/// </summary>
public class UpdateTaskCommandHandlerTests
{
    private readonly Mock<ITaskRepository> _taskRepositoryMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    private readonly Guid _creatorId = Guid.NewGuid();
    private readonly Guid _assigneeId = Guid.NewGuid();

    private UpdateTaskCommandHandler CreateHandler()
        => new(_taskRepositoryMock.Object, _currentUserServiceMock.Object, _unitOfWorkMock.Object);

    private TaskItem CreateExistingTask()
        => TaskItem.Create("Eski Başlık", "Eski Açıklama", TaskPriority.Low,
            DateTime.UtcNow.AddDays(5), _creatorId, _assigneeId);

    [Fact]
    public async Task Handle_ShouldUpdateTask_WhenRequestUserIsCreator()
    {
        var task = CreateExistingTask();
        _taskRepositoryMock
            .Setup(x => x.GetByIdAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        _currentUserServiceMock.Setup(x => x.UserId).Returns(_creatorId);

        var command = new UpdateTaskCommand
        {
            TaskId = task.Id,
            Title = "Güncellenmiş Başlık",
            Description = "Güncellenmiş Açıklama",
            Priority = TaskPriority.Urgent,
            DeadLine = DateTime.UtcNow.AddDays(10),
            AssignedToUserId = _assigneeId
        };

        var handler = CreateHandler();

        await handler.Handle(command, CancellationToken.None);

        task.Title.Should().Be("Güncellenmiş Başlık");
        task.Priority.Should().Be(TaskPriority.Urgent);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorized_WhenRequestUserIsNotCreator()
    {
        var task = CreateExistingTask();
        _taskRepositoryMock
            .Setup(x => x.GetByIdAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        // Mevcut kullanıcı, görevi oluşturan değil - atanan kişi (assignee)
        _currentUserServiceMock.Setup(x => x.UserId).Returns(_assigneeId);

        var command = new UpdateTaskCommand
        {
            TaskId = task.Id,
            Title = "Hileli Güncelleme",
            Description = "Açıklama",
            Priority = TaskPriority.High,
            DeadLine = DateTime.UtcNow.AddDays(7),
            AssignedToUserId = _assigneeId
        };

        var handler = CreateHandler();

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedTaskOperationException>();

        // Görev değişmemiş olmalı
        task.Title.Should().Be("Eski Başlık");

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFound_WhenTaskDoesNotExist()
    {
        _taskRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaskItem?)null);

        var command = new UpdateTaskCommand
        {
            TaskId = Guid.NewGuid(),
            Title = "Başlık",
            Description = "Açıklama",
            Priority = TaskPriority.Medium,
            DeadLine = DateTime.UtcNow.AddDays(3),
            AssignedToUserId = _assigneeId
        };

        var handler = CreateHandler();

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
