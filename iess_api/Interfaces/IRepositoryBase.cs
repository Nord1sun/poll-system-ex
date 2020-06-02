using System;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace iess_api.Interfaces
{
    public interface IRepositoryBase<T> where T : class
    {
        Task<T> GetAsync(string id);

        Task<List<T>> GetAllAsync();
        
        Task<T> PostAsync(T obj);

        Task<bool> ReplaceAsync(T obj, Expression<Func<T, bool>> expression);
        
        Task<bool> DeleteAsync(Expression<Func<T, bool>> expression);
    }
}
