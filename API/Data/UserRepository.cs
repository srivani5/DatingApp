using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class UserRepository : IUserRepository
    {
        private readonly DataContext _context;
        private readonly IMapper _mapper;
        public UserRepository(DataContext context, IMapper mapper)
        {
            _mapper = mapper;
            _context = context;

        }
        public Task<AppUser> DeleteUser()
        {
            throw new System.NotImplementedException();
        }

        public async Task<IEnumerable<AppUser>> GetAllUsersAsync()
        {
            return await _context.Users
            .Include(p => p.Photos)
            .ToListAsync();
        }

        public async Task<MemberDTO> GetMemberByUserNameAsync(string username)
        {
            return await _context.Users.Where(x => x.UserName == username)
                                .ProjectTo<MemberDTO>(_mapper.ConfigurationProvider).SingleOrDefaultAsync();
        }

        public Task<string> GetMemberGender(string username)
        {
            return _context.Users
                    .Where(x => x.UserName == username)
                    .Select(x => x.Gender).FirstOrDefaultAsync();
        }

        public async Task<PagedList<MemberDTO>> GetMembersAsync(UserParams userParams)
        {
            // var query = _context.Users.ProjectTo<MemberDTO>(_mapper.ConfigurationProvider)
            //                             .AsNoTracking();
            var query = _context.Users.AsQueryable();
            query = query.Where(u => u.UserName != userParams.CurrentUserName);
            query = query.Where(u => u.Gender == userParams.Gender);

            var minDob = DateTime.Today.AddYears(-userParams.maxAge-1);
            var maxDob = DateTime.Today.AddYears(-userParams.minAge);
            
            query = query.Where(u => u.DateOfBirth >= minDob && u.DateOfBirth <= maxDob);
            query = userParams.OrderBy switch
            {
                "created" => query.OrderByDescending(u => u.Created),
                _ => query.OrderByDescending(u => u.LastActive)
            };
            return await PagedList<MemberDTO>.CreateAsync(query.ProjectTo<MemberDTO>
            (_mapper.ConfigurationProvider).AsNoTracking(), userParams.PageNumber, userParams.pageSize);
        }

        public async Task<AppUser> GetUserById(int id)
        {
            return await _context.Users.FindAsync(id);
        }

        public Task<AppUser> GetUserByUserNameAsync(string username)
        {
            return _context.Users
            .Include(p => p.Photos)
            .SingleOrDefaultAsync(x => x.UserName == username);
        }

        // commented for implementating of unit of work
        // public async Task<bool> SaveChangesAsync()
        // {
        //     return await _context.SaveChangesAsync() > 0;
        // }

        public void Update(AppUser user)
        {
            _context.Entry(user).State = EntityState.Modified;
        }
    }
}