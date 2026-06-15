namespace TaskManagement.Application.Common.Exceptions;

/// <summary>
/// Belirtilen kaynak/entity bulunamadığında fırlatılır.
/// API katmanında 404 Not Found'a map'lenir.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string name, object key)
        : base($"\"{name}\" ({key}) bulunamadı.")
    {
    }

    public NotFoundException(string message) : base(message) { }
}

/// <summary>
/// Kullanıcının istenen işlem için yetkisi olmadığında fırlatılır.
/// API katmanında 403 Forbidden'a map'lenir.
/// </summary>
public class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException()
        : base("Bu işlem için yetkiniz bulunmamaktadır.")
    {
    }

    public ForbiddenAccessException(string message) : base(message) { }
}
