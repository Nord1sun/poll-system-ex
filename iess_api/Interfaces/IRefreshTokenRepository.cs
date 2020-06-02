using iess_api.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace iess_api.Interfaces
{
    public interface IRefreshTokenRepository : IRepositoryBase<RefreshTokenModel>
    {
        Task<RefreshTokenModel> GetRefreshToken(string token);
        Task<RefreshTokenModel> GetRefreshTokenByUsername(string username);
    }
}
