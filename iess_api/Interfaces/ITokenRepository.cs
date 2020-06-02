using iess_api.Models;
using System.Threading.Tasks;

namespace iess_api.Interfaces
{
    public interface ITokenRepository : IRepositoryBase<UserModel>
    {
        Task<UserModel> GetPersonAsync(string username, string password);
        Task<UserModel> GetPersonByNameAsync(string username);
    }
}
