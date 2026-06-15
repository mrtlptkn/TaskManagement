using AutoMapper;
using FluentAssertions;
using Moq;
using TaskManagement.Application.Abstractions.Persistence;
using TaskManagement.Application.Abstractions.Services;
using TaskManagement.Application.Common.Mappings;
using TaskManagement.Application.Common.Models;
using TaskManagement.Application.Common.Specifications;
using TaskManagement.Application.Features.Tasks.Queries.GetTasks;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using Xunit;

namespace TaskManagement.Application.UnitTests.Tasks;

public class GetTasksQueryHandlerTests
{
    private readonly Mock<ITaskRepository> _taskRepositoryMock = new();
    private readonly Mock<IIdentityService> _identityServiceMock = new();
    private readonly IMapper _mapper;

    public GetTasksQueryHandlerTests()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<TaskMappingProfile>();
        });
        _mapper = config.CreateMapper();
    }

    private GetTasksQueryHandler CreateHandler()
        => new(_taskRepositoryMock.Object, _identityServiceMock.Object, _mapper);

    [Fact]
    public async Task Handle_ShouldReturnPagedResult_WithAssignedUserNamesResolved()
    {
        var assigneeId = Guid.NewGuid();
        var task1 = TaskItem.Create("Görev 1", "Açıklama 1", TaskPriority.Urgent,
            DateTime.UtcNow.AddDays(1), Guid.NewGuid(), assigneeId);
        var task2 = TaskItem.Create("Görev 2", "Açıklama 2", TaskPriority.High,
            DateTime.UtcNow.AddDays(2), Guid.NewGuid(), assigneeId);

        _taskRepositoryMock
            .Setup(x => x.GetFilteredAsync(It.IsAny<TaskFilterSpecification>(), 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<TaskItem> { task1, task2 }, 2));

        _identityServiceMock
            .Setup(x => x.GetUserFullNameAsync(assigneeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Ayşe Çalışan");

        var handler = CreateHandler();
        var query = new GetTasksQuery { Priority = TaskPriority.Urgent, PageNumber = 1, PageSize = 20 };

        var result = await handler.Handle(query, CancellationToken.None);

        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(i => i.AssignedToUserName == "Ayşe Çalışan");

        // Identity servisi her görev için ayrı çağrılmamalı (cache kullanılmalı)
        _identityServiceMock.Verify(
            x => x.GetUserFullNameAsync(assigneeId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyResult_WhenNoTasksMatch()
    {
        _taskRepositoryMock
            .Setup(x => x.GetFilteredAsync(It.IsAny<TaskFilterSpecification>(), 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<TaskItem>(), 0));

        var handler = CreateHandler();
        var query = new GetTasksQuery { Status = TaskStatusEnum.Completed };

        var result = await handler.Handle(query, CancellationToken.None);

        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }
}
