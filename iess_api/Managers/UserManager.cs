using System;
using System.Linq;
using iess_api.Models;
using iess_api.Interfaces;
using System.Threading.Tasks;

namespace iess_api.Managers
{
    public class UserManager : IUserManager
    {
        private readonly IUserRepository _repository;

        private readonly IRefreshTokenRepository _refreshTokenRepository;
        
        private readonly ITokenManager _tokenManager;

        private readonly IGroupManager _groupManager;

        public UserManager(IUserRepository rep, IRefreshTokenRepository refreshTokenRepository, ITokenManager tokenManager, IGroupManager groupManager)
        {
            _repository = rep;
            _refreshTokenRepository = refreshTokenRepository;
            _tokenManager = tokenManager;
            _groupManager = groupManager;
        }

        public async Task<UserModel> CreateUserAsync(UserModel model)
        {
            var users = await _repository.GetAllAsync();
            
            if (!users.Any(x => x.Login.Equals(model.Login)))
            {
                model.CreationDate = DateTime.Now;
                model.LastLogin = DateTime.Now;

                var password = model.PasswordHash;
                model.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.PasswordHash);

                var user = await _repository.PostAsync(model);
                var claims = _tokenManager.GetClaimsAsync(user);
                var tokenModel = _tokenManager.GetTokenAsync(claims);
                
                var result = await _refreshTokenRepository.PostAsync(new RefreshTokenModel
                {
                    Username = user.Login,
                    Token = tokenModel.RefreshToken
                });
                user.PasswordHash = null;
                return user;
            }

            return null;
        }

        public async Task<bool> DeleteUserAsync(string id)
        {
            try 
            {
                var result = await _repository.DeleteAsync(x => x.UserId.Equals(id));
                return result;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> DeleteManyUsersAsync(string[] idList) 
        {
            var values = await Task.WhenAll(idList.Select(async id => await DeleteUserAsync(id)).ToArray());
            var successResult = values.Count(x => x);
            return idList.Length == successResult;
        }

        public async Task<PageResponse<UserModel>> GetAllAsync(PageInfo info)
        {
            var data = await _repository.GetAllUsersAsync(info);
            var groups = await _groupManager.GetAllGroups();
            var users = data.Items.ToList();
            users.ForEach(user => {
                var group = groups.FirstOrDefault(x => x.Id == user.GroupName);
                user.GroupName = group?.Name ?? "<undefined>";
            });
            data.Items = users;
            return data;
        }

        public async Task<UserModel> GetByIdAsync(string id)
        {
            try 
            {
                var user = await _repository.GetAsync(id);
                var group = await _groupManager.GetByIdAsync(user.GroupName);
                user.GroupName = group?.Name ?? "<undefined>";
                user.PasswordHash = null;
                return user;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<bool> UpdateUserAsync(string id, UserModel model)
        {
            try 
            {
                var currUser = await _repository.GetAsync(id);
                var users = await _repository.GetAllAsync();
                
                model.UserId = id;
                
                if (!string.IsNullOrEmpty(model.FirstName) && !currUser.FirstName.Equals(model.FirstName))
                {
                    currUser.FirstName = model.FirstName;
                }
                
                if (!string.IsNullOrEmpty(model.LastName) && !currUser.LastName.Equals(model.LastName))
                {
                    currUser.LastName = model.LastName;
                }
                
                if (!string.IsNullOrEmpty(model.PasswordHash) && 
                    !currUser.PasswordHash.Equals(BCrypt.Net.BCrypt.HashPassword(model.PasswordHash)))
                {
                    currUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.PasswordHash);
                }

                if (!string.IsNullOrEmpty(model.GroupName) && !currUser.GroupName.Equals(model.GroupName))
                {
                    currUser.GroupName = model.GroupName;
                }
                
                if (!string.IsNullOrEmpty(model.Login) && !users.Any(x => x.Login.Equals(model.Login)) &&
                    !currUser.Login.Equals(model.Login))
                {
                    currUser.Login = model.Login;
                }
                
                if (!string.IsNullOrEmpty(model.Role) && !currUser.Role.Equals(model.Role))
                {
                    currUser.Role = model.Role;
                }
                
                var result = await _repository.ReplaceAsync(currUser, x => x.UserId.Equals(model.UserId));

                return result;
            }
            catch (Exception) 
            {
                return false;
            }
        }
        public async Task<bool> UpdateManyUsersAsync(UserModel[] users)
        {   
            var values = await Task.WhenAll(users.Select(async user => await UpdateUserAsync(user.UserId, user)).ToArray());
            var successResult = values.Count(x => x);
            return users.Length == successResult;
        }
    }
}