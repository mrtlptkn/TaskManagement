using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.Features.Tasks.Commands.AddAttachment;

namespace TaskManagement.API.Controllers;

/// <summary>
/// Görev eklerinin (attachment/dokümantasyon) yönetimi.
/// multipart/form-data ile dosya yükleme bu controller'a ayrıştırılmıştır
/// (Single Responsibility: TasksController CRUD ile, bu controller dosya I/O ile ilgilenir).
/// </summary>
[ApiController]
[Route("api/v1/tasks/{taskId:guid}/attachments")]
[Authorize]
public class TaskAttachmentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TaskAttachmentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Göreve bir dosya eki (ek/dokümantasyon) yükler.</summary>
    [HttpPost]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload(Guid taskId, IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "Yüklenecek dosya boş olamaz." });

        await using var stream = file.OpenReadStream();

        var command = new AddAttachmentCommand
        {
            TaskId = taskId,
            FileName = file.FileName,
            ContentType = file.ContentType,
            FileSize = file.Length,
            Content = stream
        };

        var attachmentId = await _mediator.Send(command, cancellationToken);

        return Ok(new { id = attachmentId });
    }
}
