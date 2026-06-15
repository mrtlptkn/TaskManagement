using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.Features.Tasks.Commands.AddActivityLog;
using TaskManagement.Application.Features.Tasks.Commands.ChangeTaskStatus;
using TaskManagement.Application.Features.Tasks.Commands.CreateTask;
using TaskManagement.Application.Features.Tasks.Commands.ToggleTodoItem;
using TaskManagement.Application.Features.Tasks.Commands.UpdateTask;
using TaskManagement.Application.Features.Tasks.Queries.GetOverdueTasks;
using TaskManagement.Application.Features.Tasks.Queries.GetTaskById;
using TaskManagement.Application.Features.Tasks.Queries.GetTasks;

namespace TaskManagement.API.Controllers;

/// <summary>
/// Görev (Task) yönetimi endpoint'leri.
///
/// Controller "thin" tutulmuştur: tüm orchestration ve iş kuralları
/// MediatR üzerinden Application katmanındaki Command/Query Handler'larına
/// devredilir (Mediator/CQRS deseni). Resource Based Authorization
/// kuralları (örn. sadece görevi oluşturan kişi güncelleyebilir/onaylayabilir)
/// Domain entity'sinin (TaskItem) metodları içinde uygulanır ve
/// ExceptionHandlingMiddleware tarafından uygun HTTP status kodlarına çevrilir.
/// </summary>
[ApiController]
[Route("api/v1/tasks")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly IMediator _mediator;

    public TasksController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Yeni bir görev oluşturur.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTaskCommand command, CancellationToken cancellationToken)
    {
        var taskId = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = taskId }, new { id = taskId });
    }

    /// <summary>
    /// Görevleri filtreli ve sayfalı şekilde listeler.
    ///
    /// Örnekler:
    ///   - Önceliğe göre       : ?priority=Urgent
    ///   - Duruma göre         : ?status=InProgress
    ///   - Atanan kişiye göre  : ?assignedToUserId={guid}
    ///   - Gecikmiş görevler   : ?isOverdue=true
    ///   - Son 1 hafta içinde  : ?dueWithinWeek=true
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetTasks([FromQuery] GetTasksQuery query, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>Sadece gecikmiş (deadline'ı geçmiş ve tamamlanmamış) görevleri listeler.</summary>
    [HttpGet("overdue")]
    public async Task<IActionResult> GetOverdue([FromQuery] GetOverdueTasksQuery query, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>Bir görevin tüm detaylarını (todo'lar, ekler, aktivite akışı) getirir.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetTaskByIdQuery { TaskId = id }, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Görev bilgilerini güncelller.
    /// Resource Based Authorization: sadece görevi oluşturan kullanıcı güncelleyebilir.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaskCommand command, CancellationToken cancellationToken)
    {
        command.TaskId = id;
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Görev durumu üzerinde aksiyon uygular (StartProgress, SubmitForReview, Approve, RejectReview).
    /// Yetki kuralları aksiyona göre değişir; bkz. TaskStatusAction enum açıklamaları.
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] ChangeTaskStatusCommand command, CancellationToken cancellationToken)
    {
        command.TaskId = id;
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Bir todo (yapılacaklar) öğesinin checkbox durumunu değiştirir.
    /// Tüm öğeler işaretlenirse görev otomatik olarak "İncelemeye Alındı" durumuna geçer.
    /// </summary>
    [HttpPatch("{id:guid}/todos/{todoId:guid}")]
    public async Task<IActionResult> ToggleTodo(Guid id, Guid todoId, [FromBody] ToggleTodoItemCommand command, CancellationToken cancellationToken)
    {
        command.TaskId = id;
        command.TodoItemId = todoId;
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>Göreve bir akış/yorum kaydı ekler (timeline girişi).</summary>
    [HttpPost("{id:guid}/comments")]
    public async Task<IActionResult> AddComment(Guid id, [FromBody] AddActivityLogCommand command, CancellationToken cancellationToken)
    {
        command.TaskId = id;
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }
}
