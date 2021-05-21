using System.Collections.Generic;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    public class MessagesController : ControllerApiBase
    {
        private readonly IUnitOfWork _unitOfWork;
        public MessagesController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // [HttpPost]
        // public async Task<ActionResult<MessageDTO>> CreateMessage(CreateMessageDTO createMessageDTO)
        // {
        // var username = User.GetUsername();

        // if (username == createMessageDTO.RecipientUsername.ToLower()) return BadRequest("You cannot send message to yourself");

        // var sender = await _userRepository.GetUserByUserNameAsync(username);
        // var recipient = await _userRepository.GetUserByUserNameAsync(createMessageDTO.RecipientUsername);

        // if (recipient == null) return NotFound();

        // var message = new Message
        // {
        //     Sender = sender,
        //     Recipient = recipient,
        //     SenderUsername = sender.UserName,
        //     RecipientUsername = recipient.UserName,
        //     Content = createMessageDTO.Content
        // };

        // _messageRepository.AddMessage(message);
        // if(await _messageRepository.SaveAllAsync()) return Ok(_mapper.Map<MessageDTO>(message));
        // return BadRequest("Failed to send message");
        // }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MessageDTO>>> GetMessagesForUser([FromQuery]
                                                MessageParams messageParams)
        {
            messageParams.Username = User.GetUsername();
            var messages = await _unitOfWork.MessageRepository.GetMessagesForUser(messageParams);
            Response.AddPaginationHeader(messages.CurrentPage, messages.PageSize, messages.TotalCount, messages.TotalPages);
            return messages;
        }

        // commenting as this functionality is moved to SignalR
        // [HttpGet("thread/{username}")]
        // // here username is referred to another specific user, not the logged in user
        // public async Task<ActionResult<IEnumerable<MessageDTO>>> GetMessageThread(string username)
        // {
        //     var currentUserName = User.GetUsername();
        //     return Ok(await _messageRepository.GetMessageThread(currentUserName, username ));
        // }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteMessage(int id)
        {
            var username = User.GetUsername();
            var message = await _unitOfWork.MessageRepository.GetMessage(id);

            if (message.SenderUsername != username && message.RecipientUsername != username) return Unauthorized();

            if (message.Sender.UserName == username) { message.SenderDeleted = true; }

            if (message.Recipient.UserName == username) { message.RecipientDeleted = true; }

            if (message.RecipientDeleted && message.SenderDeleted)
            {
                _unitOfWork.MessageRepository.DeleteMessage(message);
            }
            if (await _unitOfWork.Complete()) return Ok();

            return BadRequest("Error in deleting message");

        }
    }
}