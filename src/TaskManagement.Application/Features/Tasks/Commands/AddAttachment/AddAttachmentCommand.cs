using FluentValidation;
using MediatR;
using TaskManagement.Application.Abstractions.Persistence;
using TaskManagement.Application.Abstractions.Services;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Features.Tasks.Commands.AddAttachment;

/// <summary>
/// Göreve dosya eki (attachment/dokümantasyon) ekleme komutu.
/// Dosya stream'i IFileStorageService portu üzerinden saklanır;
/// Application katmanı storage detaylarından (disk, blob vb.) bağımsızdır.
/// </summary>
public record AddAttachmentCommand : IRequest<Guid>
{
    public Guid TaskId { get; set; }
    public string FileName { get; init; } = default!;
    public string ContentType { get; init; } = default!;
    public long FileSize { get; init; }

    /// <summary>FluentValidation/serialization kolaylığı için stream burada tutulur.</summary>
    public Stream Content { get; init; } = default!;
}

public class AddAttachmentCommandValidator : AbstractValidator<AddAttachmentCommand>
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    public AddAttachmentCommandValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty().WithMessage("Görev kimliği (TaskId) belirtilmelidir.");

        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("Dosya adı boş olamaz.")
            .MaximumLength(260).WithMessage("Dosya adı en fazla 260 karakter olabilir.");

        RuleFor(x => x.FileSize)
            .GreaterThan(0).WithMessage("Dosya boyutu sıfırdan büyük olmalıdır.")
            .LessThanOrEqualTo(MaxFileSizeBytes).WithMessage("Dosya boyutu 10 MB'ı aşamaz.");

        RuleFor(x => x.ContentType)
            .NotEmpty().WithMessage("Dosya içerik türü (ContentType) belirtilmelidir.");
    }
}

public class AddAttachmentCommandHandler : IRequestHandler<AddAttachmentCommand, Guid>
{
    private readonly ITaskRepository _taskRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IUnitOfWork _unitOfWork;

    public AddAttachmentCommandHandler(
        ITaskRepository taskRepository,
        ICurrentUserService currentUserService,
        IFileStorageService fileStorageService,
        IUnitOfWork unitOfWork)
    {
        _taskRepository = taskRepository;
        _currentUserService = currentUserService;
        _fileStorageService = fileStorageService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(AddAttachmentCommand request, CancellationToken cancellationToken)
    {
        var task = await _taskRepository.GetByIdWithDetailsAsync(request.TaskId, cancellationToken)
            ?? throw new NotFoundException(nameof(TaskItem), request.TaskId);

        var storedPath = await _fileStorageService.SaveFileAsync(
            request.Content, request.FileName, request.ContentType, cancellationToken);

        task.AddAttachment(request.FileName, storedPath, request.ContentType, request.FileSize,
            _currentUserService.UserId);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return task.Attachments.Last().Id;
    }
}
