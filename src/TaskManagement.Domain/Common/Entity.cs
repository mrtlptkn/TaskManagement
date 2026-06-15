namespace TaskManagement.Domain.Common;

/// <summary>
/// Tüm domain entity'leri için temel sınıf.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected set; }

    protected Entity() { }

    protected Entity(Guid id)
    {
        Id = id;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Entity other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;

        return Id == other.Id;
    }

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(Entity? left, Entity? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(Entity? left, Entity? right) => !(left == right);
}

/// <summary>
/// Oluşturulma/güncellenme bilgisi taşıyan entity'ler için temel sınıf.
/// </summary>
public abstract class AuditableEntity : Entity
{
    public DateTime CreatedAt { get; protected set; }
    public DateTime? UpdatedAt { get; protected set; }

    protected AuditableEntity() { }

    protected AuditableEntity(Guid id) : base(id) { }

    protected void SetUpdated() => UpdatedAt = DateTime.UtcNow;
}
