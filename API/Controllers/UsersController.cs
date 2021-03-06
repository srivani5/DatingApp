using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using System.Security.Claims;

namespace API.Controllers
{
    [Authorize]
    public class UsersController : ControllerApiBase
    {
        private readonly DataContext _context;
        private readonly IUserRepository _userRepository;
        public IMapper _mapper { get; }
        public UsersController(DataContext context, IUserRepository userRepository, IMapper mapper)
        {
            _mapper = mapper;
            _userRepository = userRepository;
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AppUser>>> GetUsers()
        {
            // return await _context.Users.ToListAsync();
            // var users = await _userRepository.GetAllUsersAsync();
            // var usersToReturn = _mapper.Map<IEnumerable<MemberDTO>>(users);

            var users = await _userRepository.GetMembersAsync();
            return Ok(users);
        }

        [HttpGet("{username}")]
        public async Task<ActionResult<MemberDTO>> GetUser(string username)
        {
            // No need of IEnumerable because we return only one value
            // return await _context.Users.FindAsync(id);
            // var user = await _userRepository.GetUserByUserNameAsync(username);
            // var userToReturn = _mapper.Map<MemberDTO>(user);

            var user = await _userRepository.GetMemberByUserNameAsync(username);
            var userToReturn = _mapper.Map<MemberDTO>(user);
            return Ok(userToReturn);
        }

        [HttpPut]
        public async Task<ActionResult> UpdateUser(MemberUpdateDTO memberUpdateDTO)
        {
            var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await _userRepository.GetUserByUserNameAsync(username);
             _mapper.Map(memberUpdateDTO, user);
            _userRepository.Update(user);
            if(await _userRepository.SaveChangesAsync()) return NoContent();
            return BadRequest("User update failed");
        }
    }
}