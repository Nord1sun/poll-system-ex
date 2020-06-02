using iess_api.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace iess_api.Interfaces
{
    public interface IGroupManager
    {
        Task<List<GroupInfo>> GetAllGroups();

        Task<GroupInfo> GetByIdAsync(string id);

        Task<string> GetIdByName(string group);

        Task<GroupModel> CreateAsync(GroupModel group);

        Task<bool> UpdateAsync(string id, GroupModel group);

        Task<bool> DeleteByIdAsync(string id);

        Task<bool> DeleteManyAsync(string[] idList);
    }
}
