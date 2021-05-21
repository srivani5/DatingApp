using System;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace API.SignalR
{
    public class MessageHub : Hub
    {
        private readonly IMapper _mapper;
        private readonly PresenceTracker _presenceTracker;
        private readonly IHubContext<PresenceHub> _presenceHub;
        private readonly IUnitOfWork _unitOfWork;

        public MessageHub(IUnitOfWork unitOfWork, IMapper mapper, IHubContext<PresenceHub> presenceHub,
        PresenceTracker presenceTracker)
        {
            _unitOfWork = unitOfWork;
            _presenceHub = presenceHub;
            _presenceTracker = presenceTracker;
            _mapper = mapper;
        }
        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var otherUser = httpContext.Request.Query["user"].ToString();
            var groupName = GenerateGroupName(Context.User.GetUsername(), otherUser);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            var addedToGroup = await AddToGroup(groupName);
            await Clients.Group(groupName).SendAsync("UpdatedGroup", addedToGroup);
            var messages = await _unitOfWork.MessageRepository.GetMessageThread(Context.User.GetUsername(), otherUser);
            if(_unitOfWork.HasChanges()) await _unitOfWork.Complete();
            await Clients.Caller.SendAsync("ReceiveMessageThread", messages);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var removeGroupName = await RemoveFromMessageGroup();
            await Clients.Group(removeGroupName.Name).SendAsync("UpdatedGroup", removeGroupName);
            await base.OnDisconnectedAsync(exception);
        }

        private string GenerateGroupName(string callerName, string other)
        {
            var stringCompare = string.CompareOrdinal(callerName, other) < 0;
            return stringCompare ? $"{callerName}-{other}" : $"{other}-{callerName}";
        }

        public async Task SendMessage(CreateMessageDTO createMessageDTO)
        {
            var username = Context.User.GetUsername();

            if (username == createMessageDTO.RecipientUsername.ToLower())
            {
                throw new HubException("You cannot send message to yourself");
            }

            var sender = await _unitOfWork.UserRepository.GetUserByUserNameAsync(username);
            var recipient = await _unitOfWork.UserRepository.GetUserByUserNameAsync(createMessageDTO.RecipientUsername);

            if (recipient == null)
            {
                throw new HubException("User not found");
            }

            var message = new Message
            {
                Sender = sender,
                Recipient = recipient,
                SenderUsername = sender.UserName,
                RecipientUsername = recipient.UserName,
                Content = createMessageDTO.Content
            };

            var groupName = GenerateGroupName(sender.UserName, recipient.UserName);
            var group = await _unitOfWork.MessageRepository.GetMessageGroup(groupName);

            if (group.Connections.Any(x => x.Username == recipient.UserName))
            {
                message.DateRead = DateTime.UtcNow;
            }
            else
            {
                var connections = await _presenceTracker.GetConnectionsForUser(recipient.UserName);
                if (connections != null)
                {
                    await _presenceHub.Clients.Clients(connections).SendAsync("NewMessageReceived",
                    new { username = sender.UserName, knownAs = sender.KnownAs });
                }
            }

            _unitOfWork.MessageRepository.AddMessage(message);
            if (await _unitOfWork.Complete())
            {
                await Clients.Group(groupName).SendAsync("NewMessage", _mapper.Map<MessageDTO>(message));
            }
        }

        public async Task<Group> AddToGroup(string groupName)
        {
            var group = await _unitOfWork.MessageRepository.GetMessageGroup(groupName);

            var connection = new Connection(Context.ConnectionId, Context.User.GetUsername());

            if (group == null)
            {
                group = new Group(groupName);
                _unitOfWork.MessageRepository.AddGroup(group);
            }
            group.Connections.Add(connection);
            if (await _unitOfWork.Complete()) return group;
            throw new HubException("Failed to join the group");
        }

        public async Task<Group> RemoveFromMessageGroup()
        {
            var connectionId = Context.ConnectionId;
            var existingGroup = await _unitOfWork.MessageRepository.GetGroupForConnection(connectionId);
            var connection = existingGroup.Connections.FirstOrDefault(x => x.ConnectionId == connectionId);
            _unitOfWork.MessageRepository.RemoveConnection(connection);
            if (await _unitOfWork.Complete()) return existingGroup;

            throw new HubException("Failed to remove from group");
        }
    }
}