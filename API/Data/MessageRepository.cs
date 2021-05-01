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
using System;

namespace API.Data
{
    public class MessageRepository : IMessageRepository
    {
        private readonly DataContext _context;
        private readonly IMapper _mapper;
        public MessageRepository(DataContext context, IMapper mapper)
        {
            _mapper = mapper;
            _context = context;

        }
        public void AddMessage(Message message)
        {
            _context.Messages.Add(message);
        }

        public void DeleteMessage(Message message)
        {
            _context.Messages.Remove(message);
        }

        public async Task<Message> GetMessage(int id)
        {
            return await _context.Messages
                        .Include(u => u.Sender)
                        .Include(m => m.Recipient)
                        .SingleOrDefaultAsync(x=> x.Id == id);
        }

        public Task<PagedList<MessageDTO>> GetMessagesForUser(MessageParams messageParams)
        {
            var query = _context.Messages.OrderBy(m => m.MessageSent)
                        .AsQueryable();
                            
            query = messageParams.Container switch
            {
                "Inbox" => query.Where(u => u.RecipientUsername == messageParams.Username 
                && u.RecipientDeleted == false),
                "Outbox" => query.Where(r => r.SenderUsername == messageParams.Username 
                && r.SenderDeleted == false),
                _ => query.Where(u => u.RecipientUsername == messageParams.Username 
                && u.DateRead == null && u.RecipientDeleted == false)
            };

            var messages = query.ProjectTo<MessageDTO>(_mapper.ConfigurationProvider);
            return PagedList<MessageDTO>.CreateAsync(messages, messageParams.PageNumber,
                                                         messageParams.pageSize);
        }

        public async Task<IEnumerable<MessageDTO>> GetMessageThread(string curentUserName, string recipientName)
        {
            var messages = await _context.Messages
                            .Include(u => u.Sender).ThenInclude(p => p.Photos)
                            .Include(r => r.Recipient).ThenInclude(P => P.Photos)
                            .Where(u => u.Recipient.UserName == curentUserName && u.RecipientDeleted == false
                            && u.Sender.UserName == recipientName
                            || u.Recipient.UserName == recipientName && u.Sender.UserName == curentUserName
                             && u.SenderDeleted == false
                            )
                            .OrderBy(u => u.MessageSent)
                            .ToListAsync();

            var unreadMessages = messages.Where(m => m.DateRead == null && 
                                  m.RecipientUsername == curentUserName).ToList();
            
            if(unreadMessages.Any())
            {
                foreach (var msg in unreadMessages)
                {
                    msg.DateRead = DateTime.Now;
                }
            }

            await _context.SaveChangesAsync();
            return _mapper.Map<IEnumerable<MessageDTO>>(messages);
        }

        public async Task<bool> SaveAllAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}