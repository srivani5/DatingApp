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
using Microsoft.AspNetCore.Http;
using API.Services;
using API.Extensions;

namespace API.Controllers
{
    [Authorize]
    public class UsersController : ControllerApiBase
    {
        private readonly DataContext _context;
        private readonly IUserRepository _userRepository;
        public IMapper _mapper { get; }
        private readonly IPhotoService _photoService;
        public UsersController(DataContext context, IUserRepository userRepository, IMapper mapper, IPhotoService photoService)
        {
            _photoService = photoService;
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

        [HttpGet("{username}", Name="GetUser")]
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
            if (await _userRepository.SaveChangesAsync()) return NoContent();
            return BadRequest("User update failed");
        }

        [HttpPost("add-photo")]
        public async Task<ActionResult<PhotoDTO>> AddPhoto(IFormFile file)
        {
            // var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var username = User.GetUsername(); // simple way of getting username using static extensions method
            var user = await _userRepository.GetUserByUserNameAsync(username);

            var result = await _photoService.AddPhotoAsync(file);

            if (result.Error != null)
            {
                return BadRequest(result.Error.Message);
            }

            var photo = new Photo
            {
                Url = result.SecureUrl.AbsoluteUri,
                PublicId = result.PublicId
            };

            if (user.Photos.Count == 0)
            {
                photo.IsMain = true;
            }

            user.Photos.Add(photo);

            if (await _userRepository.SaveChangesAsync()) 
            { 
                return CreatedAtRoute("GetUser", new{username = user.UserName}, _mapper.Map<PhotoDTO>(photo)); 
            }
            return BadRequest("Error adding photo");
        }


        [HttpPut("set-main-photo/{photoId}")]
        public async Task<ActionResult> SetMainPhoto(int photoId)
        {
            var user =  await _userRepository.GetUserByUserNameAsync(User.GetUsername());
            var photo = user.Photos.FirstOrDefault(x => x.Id == photoId);
            if(photo.IsMain){return BadRequest("Already this is a main photo");}
            var currentMain = user.Photos.FirstOrDefault(x => x.IsMain);
            currentMain.IsMain = false;
            photo.IsMain = true;

            if(await _userRepository.SaveChangesAsync()){return NoContent();}
            return BadRequest("Failed to set photo as main photo");
        }

        [HttpDelete("delete-photo/{photoId}")]
        public async Task<ActionResult> DeletePhoto(int photoId)
        {
            var user =  await _userRepository.GetUserByUserNameAsync(User.GetUsername());
            var photo = user.Photos.FirstOrDefault(x => x.Id == photoId);
            if(photo == null){ return NotFound("Photo Not Found"); }
            if(photo.IsMain) { return BadRequest("Cannot delete main photo"); }
            if(photo.PublicId != null)
            {
                var result = await _photoService.DeletePhotoAsync(photo.PublicId);
                if(result.Error != null){ return BadRequest(result.Error.Message); }
            }
            user.Photos.Remove(photo);
            if(await _userRepository.SaveChangesAsync()){ return Ok();}
            return BadRequest("Failed to delete photo");
        }
    }
}