namespace TaskManagement.Domain.Enums;

/// <summary>
/// Görevin yaşam döngüsü durumu.
///
/// Akış:
/// Todo            -> Görev oluşturulduğunda
/// InProgress      -> Atanan kişi işleme aldığında
/// InReview        -> Tüm todo'lar tamamlandığında otomatik VEYA atanan kişi
///                     incelemeye gönderdiğinde
/// Completed       -> Görevi oluşturan kişi onayladığında
/// </summary>
public enum TaskStatusEnum
{
    Todo = 1,
    InProgress = 2,
    InReview = 3,
    Completed = 4
}
