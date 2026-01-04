using System;
using System.Linq.Expressions;
using CoinPulse.Core.Common;

namespace CoinPulse.Core.Interfaces;

public interface IGenericRepository<T> where T : BaseEntity
{
    Task<IReadOnlyList<T>> GetAllAsync();
    Task<IReadOnlyList<T>> GetAsync(Expression<Func<T, bool>> predicate);
    Task<T?> GetByIdAsync(Guid id);
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);

    // performans için db de var mı yok mu
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
}
