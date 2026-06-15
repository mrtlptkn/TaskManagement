using MediatR;
using MediatR;
using TaskManagement.Application.Abstractions.Persistence;
using TaskManagement.Application.Abstractions.Services;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.Features.Tasks.Commands.AddActivityLog;

namespace TaskManagement.Application.Features.Tasks.Commands.UpdateTask;

/// <summary>
/// UpdateTaskCommand handler'ı.
///
/// Resource Based Authorization akışı:
///  1. Görev repository'den çekilir.
///  2. Bulunamazsa NotFoundException (-> 404).
///  3. task.Update(...) metoduna mevcut kullanıcı (CreatedByUserId ile karşılaştırılmak üzere) verilir.
///     Eğer mevcut kullanıcı görevi oluşturan değilse, Domain katmanı
///     UnauthorizedTaskOperationException fırlatır; bu da API katmanında
///     ForbiddenAccessException/403'e map'lenir (Exception middleware'de dönüştürülür).
///
/// Not: Yetki kontrolü kasıtlı olarak Domain entity'sinin içinde (TaskItem.Update) yapılır,
/// böylece iş kuralı tek bir yerde (Single Source of Truth) tanımlanır ve her zaman uygulanır.
/// </summary>
public class UpdateTaskCommandHandler : IRequestHandler<UpdateTaskCommand>
{
    private readonly ITaskRepository _taskRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateTaskCommandHandler(
        ITaskRepository taskRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _taskRepository = taskRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _taskRepository.GetByIdAsync(request.TaskId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.TaskItem), request.TaskId);

        task.Update(
            title: request.Title,
            description: request.Description,
            priority: request.Priority,
            deadLine: request.DeadLine,
            assignedToUserId: request.AssignedToUserId,
            requestUserId: _currentUserService.UserId);

        await _unitOfWork.SaveChangesAsync(cancellationToken);


  }
}
