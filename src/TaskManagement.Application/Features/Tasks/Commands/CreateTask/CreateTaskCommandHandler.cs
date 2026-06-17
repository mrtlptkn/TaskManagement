using MediatR;
using TaskManagement.Application.Abstractions.Persistence;
using TaskManagement.Application.Abstractions.Services;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Features.Tasks.Commands.CreateTask;

/// <summary>
/// CreateTaskCommand handler'ı. Domain'in TaskItem.Create static factory metoduyla
/// aggregate'i oluşturur, todo öğelerini ekler ve repository üzerinden persist eder.
/// </summary>
public class CreateTaskCommandHandler : IRequestHandler<CreateTaskCommand, Guid>
{
    private readonly ITaskRepository _taskRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public CreateTaskCommandHandler(
        ITaskRepository taskRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _taskRepository = taskRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
    // Domain Layerda bir servis olsaydı bu task ile lakalı bir bussiness logic servis olacaktı bu sebeple burada bir taskın birine atanması ile ilgili hafata en fazla kaç saat task atanabilir veya kaç tane yüksek derecei task verebiliriz. kaç adet task atama hakkı kalmış gibi logicleri yönetirdi. 
    // TaskAssigmentDomainService.CheckIfUserCan();

    var task = TaskItem.Create(
            title: request.Title,
            description: request.Description,
            priority: request.Priority,
            deadLine: request.DeadLine,
            createdByUserId: _currentUserService.UserId,
            assignedToUserId: request.AssignedToUserId);

        foreach (var todoTitle in request.TodoItems)
        {
            task.AddTodoItem(todoTitle, _currentUserService.UserId);
        }

       

        await _taskRepository.AddAsync(task, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return task.Id;
    }
}
