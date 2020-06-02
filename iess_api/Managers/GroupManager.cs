using System;
using System.Linq;
using iess_api.Models;
using iess_api.Interfaces;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace iess_api.Managers
{
    public class GroupManager : IGroupManager
    {
        private readonly IGroupRepository _groupRepository;
        
        private readonly IUserRepository _userRepository;

        public GroupManager(IGroupRepository groupRepository, IUserRepository userRepository) 
        {
            _groupRepository = groupRepository;
            _userRepository = userRepository;
        } 

        public async Task<GroupModel> CreateAsync(GroupModel group)
        {
            var allGroups = await GetAllGroups();

            if (!allGroups.Any(x => x.Name.Equals(group.Name)))
            {
                var result = await _groupRepository.PostAsync(group);
                return result;
            }

            return null;
        }

        public async Task<bool> DeleteByIdAsync(string id)
        {
            try
            {
                return await _groupRepository.DeleteAsync(x => x.Id.Equals(id));
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<List<GroupInfo>> GetAllGroups()
        {
            var groups = await _groupRepository.GetAllAsync();
            var users = await _userRepository.GetAllAsync();
            var queryResult =  from grp in groups.AsQueryable()
                               join user in users.AsQueryable() on grp.Id equals user.GroupName into usersInfo
                               select new GroupInfo { 
                                   Id = grp.Id, 
                                   Name = grp.Name, 
                                   TotalUsers = usersInfo.Count(),
                                   Users = null
                                };
            return queryResult.OrderBy(x => x.Name).ToList();
        }

        public async Task<GroupInfo> GetByIdAsync(string id)
        {
            try
            {
                var users = await _userRepository.GetAllAsync();
                var groups = await GetAllGroups();
                var queryResult =  from grp in groups.AsQueryable()
                                   where grp.Id == id
                                   join user in users.AsQueryable() on grp.Id equals user.GroupName into usersInfo
                                   select new GroupInfo { 
                                       Id = grp.Id, 
                                       Name = grp.Name, 
                                       TotalUsers = usersInfo.Count(),
                                       Users = from userInfo in usersInfo 
                                               select new UserModel {
                                                   UserId = userInfo.UserId,
                                                   Login = userInfo.Login,
                                                   FirstName = userInfo.FirstName,
                                                   LastName = userInfo.LastName,
                                                   GroupName = grp.Name,
                                                   PasswordHash = null,
                                                   Role = userInfo.Role
                                                }
                                    };
                return queryResult.FirstOrDefault();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<bool> UpdateAsync(string id, GroupModel group)
        {
            try
            {
                var currentValue = await _groupRepository.GetAsync(id);
                var allGroups = await GetAllGroups();

                if (currentValue.Name != group.Name && !allGroups.Any(x => x.Name.Equals(group.Name)))
                {
                    currentValue.Name = group.Name;
                    return await _groupRepository.ReplaceAsync(currentValue, x => x.Id.Equals(currentValue.Id));
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> DeleteManyAsync(string[] idList) 
        {
            var values = await Task.WhenAll(idList.Select(async id => await DeleteByIdAsync(id)).ToArray());
            var successResult = values.Count(x => x);
            return idList.Length == successResult;
        }

        public async Task<string> GetIdByName(string group)
        {
            var data = await _groupRepository.GetAllAsync();
            return data.FirstOrDefault(x => x.Name == group)?.Id;
        }
    }
}
