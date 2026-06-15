using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using TaskManagement.Application.Common.Models;
using TaskManagement.Application.Features.Auth.Commands.Login;
using TaskManagement.Application.Features.Auth.Commands.Register;
using TaskManagement.Application.Features.Tasks.Commands.ChangeTaskStatus;
using TaskManagement.Application.Features.Tasks.Commands.CreateTask;
using TaskManagement.Application.Features.Tasks.Commands.UpdateTask;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using Xunit;

namespace TaskManagement.API.IntegrationTests;

/// <summary>
/// TasksController için uçtan uca testler.
/// Resource Based Authorization akışı: bir kullanıcının oluşturduğu görevi
/// başka bir kullanıcının güncelleyememesi senaryosu özellikle test edilir.
/// </summary>
public class TasksControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public TasksControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<(HttpClient Client, Guid UserId)> CreateAuthenticatedClientAsync(string role)
    {
        var client = _factory.CreateClient();

        var email = $"{role.ToLower()}_{Guid.NewGuid():N}@example.com";
        const string password = "Test123!";

        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterCommand
        {
            FullName = $"{role} Kullanıcı",
            Email = email,
            Password = password,
            Role = role
        });
        registerResponse.EnsureSuccessStatusCode();
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterResultDto>();

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand
        {
            Email = email,
            Password = password
        });
        loginResponse.EnsureSuccessStatusCode();
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResultDto>();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult!.Token);

        return (client, Guid.Parse(registerResult!.UserId));
    }

    [Fact]
    public async Task Create_ShouldReturn201_AndTaskShouldBeRetrievable()
    {
        var (managerClient, _) = await CreateAuthenticatedClientAsync(Roles.Manager);
        var (_, employeeId) = await CreateAuthenticatedClientAsync(Roles.Employee);

        var command = new CreateTaskCommand
        {
            Title = "Entegrasyon Test Görevi",
            Description = "Açıklama",
            Priority = TaskPriority.High,
            DeadLine = DateTime.UtcNow.AddDays(5),
            AssignedToUserId = employeeId,
            TodoItems = new List<string> { "Adım 1", "Adım 2" }
        };

        var createResponse = await managerClient.PostAsJsonAsync("/api/v1/tasks", command);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var location = createResponse.Headers.Location;
        location.Should().NotBeNull();

        var getResponse = await managerClient.GetAsync(location);
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var task = await getResponse.Content.ReadFromJsonAsync<TaskDto>();
        task!.Title.Should().Be("Entegrasyon Test Görevi");
        task.TodoItems.Should().HaveCount(2);
        task.Status.Should().Be(TaskStatusEnum.Todo);
    }

    [Fact]
    public async Task Create_ShouldReturn401_WhenNotAuthenticated()
    {
        var client = _factory.CreateClient();

        var command = new CreateTaskCommand
        {
            Title = "Yetkisiz Görev",
            Description = "Açıklama",
            Priority = TaskPriority.Low,
            DeadLine = DateTime.UtcNow.AddDays(1),
            AssignedToUserId = Guid.NewGuid()
        };

        var response = await client.PostAsJsonAsync("/api/v1/tasks", command);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Update_ShouldReturn403_WhenCalledByNonCreatorUser()
    {
        var (managerClient, _) = await CreateAuthenticatedClientAsync(Roles.Manager);
        var (employeeClient, employeeId) = await CreateAuthenticatedClientAsync(Roles.Employee);

        // Manager bir görev oluşturur
        var createResponse = await managerClient.PostAsJsonAsync("/api/v1/tasks", new CreateTaskCommand
        {
            Title = "Sahiplik Testi Görevi",
            Description = "Açıklama",
            Priority = TaskPriority.Medium,
            DeadLine = DateTime.UtcNow.AddDays(5),
            AssignedToUserId = employeeId
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var location = createResponse.Headers.Location!;
        var getResponse = await managerClient.GetAsync(location);
        var task = await getResponse.Content.ReadFromJsonAsync<TaskDto>();

        // Employee (görevi oluşturmayan kullanıcı) güncelleme dener -> 403 Forbidden bekleniyor
        var updateCommand = new UpdateTaskCommand
        {
            Title = "Hileli Güncelleme Denemesi",
            Description = "Açıklama",
            Priority = TaskPriority.Urgent,
            DeadLine = DateTime.UtcNow.AddDays(20),
            AssignedToUserId = employeeId
        };

        var updateResponse = await employeeClient.PutAsJsonAsync($"/api/v1/tasks/{task!.Id}", updateCommand);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Update_ShouldReturn204_WhenCalledByCreatorUser()
    {
        var (managerClient, _) = await CreateAuthenticatedClientAsync(Roles.Manager);
        var (_, employeeId) = await CreateAuthenticatedClientAsync(Roles.Employee);

        var createResponse = await managerClient.PostAsJsonAsync("/api/v1/tasks", new CreateTaskCommand
        {
            Title = "Güncellenecek Görev",
            Description = "Açıklama",
            Priority = TaskPriority.Low,
            DeadLine = DateTime.UtcNow.AddDays(5),
            AssignedToUserId = employeeId
        });

        var location = createResponse.Headers.Location!;
        var getResponse = await managerClient.GetAsync(location);
        var task = await getResponse.Content.ReadFromJsonAsync<TaskDto>();

        var updateCommand = new UpdateTaskCommand
        {
            Title = "Başarıyla Güncellendi",
            Description = "Yeni Açıklama",
            Priority = TaskPriority.Urgent,
            DeadLine = DateTime.UtcNow.AddDays(15),
            AssignedToUserId = employeeId
        };

        var updateResponse = await managerClient.PutAsJsonAsync($"/api/v1/tasks/{task!.Id}", updateCommand);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var verifyResponse = await managerClient.GetAsync(location);
        var updatedTask = await verifyResponse.Content.ReadFromJsonAsync<TaskDto>();

        updatedTask!.Title.Should().Be("Başarıyla Güncellendi");
        updatedTask.Priority.Should().Be(TaskPriority.Urgent);
    }

    [Fact]
    public async Task ChangeStatus_FullWorkflow_ShouldCompleteSuccessfully()
    {
        var (managerClient, _) = await CreateAuthenticatedClientAsync(Roles.Manager);
        var (employeeClient, employeeId) = await CreateAuthenticatedClientAsync(Roles.Employee);

        // 1. Manager görev oluşturur (Status: Todo)
        var createResponse = await managerClient.PostAsJsonAsync("/api/v1/tasks", new CreateTaskCommand
        {
            Title = "Tam Akış Testi",
            Description = "Açıklama",
            Priority = TaskPriority.High,
            DeadLine = DateTime.UtcNow.AddDays(5),
            AssignedToUserId = employeeId
        });

        var location = createResponse.Headers.Location!;
        var task = await (await managerClient.GetAsync(location)).Content.ReadFromJsonAsync<TaskDto>();

        // 2. Employee işleme alır (Todo -> InProgress)
        var startResponse = await employeeClient.PatchAsJsonAsync($"/api/v1/tasks/{task!.Id}/status",
            new ChangeTaskStatusCommand { Action = TaskStatusAction.StartProgress });
        startResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 3. Employee incelemeye gönderir (InProgress -> InReview)
        var submitResponse = await employeeClient.PatchAsJsonAsync($"/api/v1/tasks/{task.Id}/status",
            new ChangeTaskStatusCommand { Action = TaskStatusAction.SubmitForReview });
        submitResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 4. Employee onaylamaya çalışır -> 403 (sadece Manager/creator onaylayabilir)
        var employeeApproveResponse = await employeeClient.PatchAsJsonAsync($"/api/v1/tasks/{task.Id}/status",
            new ChangeTaskStatusCommand { Action = TaskStatusAction.Approve });
        employeeApproveResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // 5. Manager onaylar (InReview -> Completed)
        var approveResponse = await managerClient.PatchAsJsonAsync($"/api/v1/tasks/{task.Id}/status",
            new ChangeTaskStatusCommand { Action = TaskStatusAction.Approve });
        approveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var finalTask = await (await managerClient.GetAsync(location)).Content.ReadFromJsonAsync<TaskDto>();
        finalTask!.Status.Should().Be(TaskStatusEnum.Completed);
    }

    [Fact]
    public async Task GetTasks_ShouldFilterByPriority()
    {
        var (managerClient, _) = await CreateAuthenticatedClientAsync(Roles.Manager);
        var (_, employeeId) = await CreateAuthenticatedClientAsync(Roles.Employee);

        await managerClient.PostAsJsonAsync("/api/v1/tasks", new CreateTaskCommand
        {
            Title = "Acil Görev",
            Description = "Açıklama",
            Priority = TaskPriority.Urgent,
            DeadLine = DateTime.UtcNow.AddDays(1),
            AssignedToUserId = employeeId
        });

        await managerClient.PostAsJsonAsync("/api/v1/tasks", new CreateTaskCommand
        {
            Title = "Düşük Öncelikli Görev",
            Description = "Açıklama",
            Priority = TaskPriority.Low,
            DeadLine = DateTime.UtcNow.AddDays(10),
            AssignedToUserId = employeeId
        });

        var response = await managerClient.GetAsync("/api/v1/tasks?priority=Urgent&pageSize=50");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<TaskListItemDto>>();

        result!.Items.Should().NotBeEmpty();
        result.Items.Should().OnlyContain(t => t.Priority == TaskPriority.Urgent);
    }

    [Fact]
    public async Task GetUsers_ShouldReturnRegisteredUsers()
    {
        var (managerClient, _) = await CreateAuthenticatedClientAsync(Roles.Manager);

        var response = await managerClient.GetAsync("/api/v1/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var users = await response.Content.ReadFromJsonAsync<List<UserDto>>();
        users.Should().NotBeNull();
        users!.Should().NotBeEmpty();
    }
}
