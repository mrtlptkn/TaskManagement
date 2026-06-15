using AutoMapper;
using TaskManagement.Application.Common.Models;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Common.Mappings;

/// <summary>
/// TaskItem ve ilişkili entity'lerin DTO'lara mapping kuralları.
/// Kullanıcı adları (CreatedByUserName / AssignedToUserName / UserName) repository
/// tarafında zaten join edilip Handler içinde set edilir; burada doğrudan
/// entity -> dto eşlemesi yapılır.
/// </summary>
public class TaskMappingProfile : Profile
{
    public TaskMappingProfile()
    {
        CreateMap<TaskItem, TaskDto>()
            .ForMember(d => d.CreatedByUserName, opt => opt.Ignore())
            .ForMember(d => d.AssignedToUserName, opt => opt.Ignore());

        CreateMap<TaskItem, TaskListItemDto>()
            .ForMember(d => d.AssignedToUserName, opt => opt.Ignore())
            .ForMember(d => d.TotalTodoCount, opt => opt.MapFrom(s => s.TodoItems.Count))
            .ForMember(d => d.CompletedTodoCount, opt => opt.MapFrom(s => s.TodoItems.Count(t => t.IsChecked)));

        CreateMap<TaskTodoItem, TaskTodoItemDto>();

        CreateMap<TaskAttachment, TaskAttachmentDto>();

        CreateMap<TaskActivityLog, TaskActivityLogDto>()
            .ForMember(d => d.UserName, opt => opt.Ignore());
    }
}
