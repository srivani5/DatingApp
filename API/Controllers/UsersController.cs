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
using API.Helpers;

namespace API.Controllers
{
    [Authorize]
    public class UsersController : ControllerApiBase
    {
        public IMapper _mapper { get; }
        private readonly IPhotoService _photoService;
        private readonly IUnitOfWork _unitOfWork;
        public UsersController(IMapper mapper, IPhotoService photoService, IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            _photoService = photoService;
            _mapper = mapper;
        }

        [Authorize(Roles = "Admin, Member")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MemberDTO>>> GetUsers([FromQuery] UserParams userParams)
        {
            // return await _context.Users.ToListAsync();
            // var users = await _userRepository.GetAllUsersAsync();
            // var usersToReturn = _mapper.Map<IEnumerable<MemberDTO>>(users);

            var username = User.GetUsername();
            var gender = await _unitOfWork.UserRepository.GetMemberGender(username);
            // var user = await _unitOfWork.UserRepository.GetUserByUserNameAsync(username);
            userParams.CurrentUserName = username;
            if (string.IsNullOrEmpty(userParams.Gender))
            {
                userParams.Gender = gender == "male" ? "female" : "male";
            }
            var users = await _unitOfWork.UserRepository.GetMembersAsync(userParams);
            Response.AddPaginationHeader(users.CurrentPage, users.PageSize,
                                            users.TotalCount, users.TotalPages);
            return Ok(users);
        }


        [Authorize(Roles = "Admin, Member")]
        [HttpGet("{username}", Name = "GetUser")]
        public async Task<ActionResult<MemberDTO>> GetUser(string username)
        {
            // No need of IEnumerable because we return only one value
            // return await _context.Users.FindAsync(id);
            // var user = await _userRepository.GetUserByUserNameAsync(username);
            // var userToReturn = _mapper.Map<MemberDTO>(user);

            var user = await _unitOfWork.UserRepository.GetMemberByUserNameAsync(username);
            var userToReturn = _mapper.Map<MemberDTO>(user);
            return Ok(userToReturn);
        }

        [HttpPut]
        public async Task<ActionResult> UpdateUser(MemberUpdateDTO memberUpdateDTO)
        {
            // var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var username = User.GetUsername();
            var user = await _unitOfWork.UserRepository.GetUserByUserNameAsync(username);
            _mapper.Map(memberUpdateDTO, user);
            _unitOfWork.UserRepository.Update(user);
            if (await _unitOfWork.Complete()) return NoContent();
            return BadRequest("User update failed");
        }

        [HttpPost("add-photo")]
        public async Task<ActionResult<PhotoDTO>> AddPhoto(IFormFile file)
        {
            // var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var username = User.GetUsername(); // simple way of getting username using static extensions method
            var user = await _unitOfWork.UserRepository.GetUserByUserNameAsync(username);

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

            if (await _unitOfWork.Complete())
            {
                return CreatedAtRoute("GetUser", new { username = user.UserName }, _mapper.Map<PhotoDTO>(photo));
            }
            return BadRequest("Error adding photo");
        }


        [HttpPut("set-main-photo/{photoId}")]
        public async Task<ActionResult> SetMainPhoto(int photoId)
        {
            var user = await _unitOfWork.UserRepository.GetUserByUserNameAsync(User.GetUsername());
            var photo = user.Photos.FirstOrDefault(x => x.Id == photoId);
            if (photo.IsMain) { return BadRequest("Already this is a main photo"); }
            var currentMain = user.Photos.FirstOrDefault(x => x.IsMain);
            currentMain.IsMain = false;
            photo.IsMain = true;

            if (await _unitOfWork.Complete()) { return NoContent(); }
            return BadRequest("Failed to set photo as main photo");
        }

        [HttpDelete("delete-photo/{photoId}")]
        public async Task<ActionResult> DeletePhoto(int photoId)
        {
            var user = await _unitOfWork.UserRepository.GetUserByUserNameAsync(User.GetUsername());
            var photo = user.Photos.FirstOrDefault(x => x.Id == photoId);
            if (photo == null) { return NotFound("Photo Not Found"); }
            if (photo.IsMain) { return BadRequest("Cannot delete main photo"); }
            if (photo.PublicId != null)
            {
                var result = await _photoService.DeletePhotoAsync(photo.PublicId);
                if (result.Error != null) { return BadRequest(result.Error.Message); }
            }
            user.Photos.Remove(photo);
            if (await _unitOfWork.Complete()) { return Ok(); }
            return BadRequest("Failed to delete photo");
        }
    }
}