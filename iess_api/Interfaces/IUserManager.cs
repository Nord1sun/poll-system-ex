using iess_api.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace iess_api.Interfaces
{
    public interface IUserManager
    {
        Task<PageResponse<UserModel>> GetAllAsync(PageInfo info);

        Task<UserModel> GetByIdAsync(string id);

        Task<UserModel> CreateUserAsync(UserModel model);

        Task<bool> DeleteUserAsync(string id);

        Task<bool> DeleteManyUsersAsync(string[] isList);

        Task<bool> UpdateUserAsync(string id, UserModel model);

        Task<bool> UpdateManyUsersAsync(UserModel[] users);
    }
}