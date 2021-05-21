using System.Collections.Generic;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Helpers;

namespace API.Interfaces
{
    public interface IUserRepository
    {
        void Update(AppUser user);
        // commented for implementating of unit of work
        // Task<bool> SaveChangesAsync();
        Task<IEnumerable<AppUser>> GetAllUsersAsync();
        Task<AppUser> GetUserByUserNameAsync(string username);
        Task<AppUser> DeleteUser();
        Task<AppUser> GetUserById(int id);
        Task<PagedList<MemberDTO>> GetMembersAsync(UserParams userParams);
        Task<MemberDTO> GetMemberByUserNameAsync(string username);
        Task<string> GetMemberGender(string username);

    }
}