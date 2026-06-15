namespace TaskManagement.Domain.Enums;

/// <summary>
/// Görev önceliği. Sayısal değerler filtreleme/sıralama için anlamlıdır
/// (Urgent en yüksek öncelik).
/// </summary>
public enum TaskPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Urgent = 4
}
