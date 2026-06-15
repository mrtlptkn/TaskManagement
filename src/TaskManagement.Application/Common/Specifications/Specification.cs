using System.Linq.Expressions;

namespace TaskManagement.Application.Common.Specifications;

/// <summary>
/// Specification Pattern için generic temel sınıf.
/// Filtreleme kriterleri Expression olarak biriktirilir; Infrastructure
/// katmanında IQueryable üzerine Where ile uygulanır. Bu sayede filtreleme
/// mantığı Application katmanında kalır, EF Core'a sızmaz.
/// </summary>
public abstract class Specification<T>
{
    private readonly List<Expression<Func<T, bool>>> _criteria = new();

    public IReadOnlyCollection<Expression<Func<T, bool>>> Criteria => _criteria.AsReadOnly();

    protected void AddCriteria(Expression<Func<T, bool>> criteria) => _criteria.Add(criteria);
}
