namespace TaskManagement.Application.Abstractions.Persistence;

/// <summary>
/// Unit of Work portu. DbContext.SaveChangesAsync'i sarmalar;
/// Application katmanı, EF Core'a doğrudan bağımlı olmaz.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
