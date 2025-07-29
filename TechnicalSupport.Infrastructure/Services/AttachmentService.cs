using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TechnicalSupport.Application.Features.Attachments.DTOs;
using TechnicalSupport.Application.Interfaces;
using TechnicalSupport.Infrastructure.Persistence;
using TechnicalSupport.Domain.Entities;



namespace TechnicalSupport.Infrastructure.Services
{
    public class AttachmentService : IAttachmentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public AttachmentService(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<List<AttachmentDto>> GetAttachmentsForTicketAsync(int ticketId, string userId)
        {
            await CheckUserTicketAccess(ticketId, userId);

            var attachments = await _context.Attachments
                .Where(a => a.TicketId == ticketId)
                .Include(a => a.UploadedBy)
                .OrderByDescending(a => a.UploadedAt)
                .ToListAsync();

            return _mapper.Map<List<AttachmentDto>>(attachments);
        }

        public async Task<List<AttachmentDto>> UploadAttachmentsAsync(int ticketId, List<IFormFile> files, string userId)
        {
            await CheckUserTicketAccess(ticketId, userId);

            var attachments = new List<Attachment>();
            var uploadPath = Path.Combine("wwwroot", "attachments", ticketId.ToString());
            Directory.CreateDirectory(uploadPath);

            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
                    var filePath = Path.Combine(uploadPath, uniqueFileName);

                    await using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    var attachment = new Attachment
                    {
                        TicketId = ticketId,
                        UploadedById = userId,
                        OriginalFileName = file.FileName,
                        StoredPath = Path.Combine("attachments", ticketId.ToString(), uniqueFileName).Replace("\\", "/"), // Dùng / cho web
                        FileType = file.ContentType,
                        UploadedAt = DateTime.UtcNow
                    };
                    attachments.Add(attachment);
                }
            }

            await _context.Attachments.AddRangeAsync(attachments);
            await _context.SaveChangesAsync();

            foreach (var att in attachments)
            {
                await _context.Entry(att).Reference(a => a.UploadedBy).LoadAsync();
            }

            return _mapper.Map<List<AttachmentDto>>(attachments);
        }

        private async Task CheckUserTicketAccess(int ticketId, string userId)
        {
            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket == null)
            {
                throw new KeyNotFoundException($"Ticket with Id {ticketId} not found.");
            }

            var user = await _context.Users.FindAsync(userId);
            var userRoles = await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
                .ToListAsync();

            bool isAdminOrTech = userRoles.Contains("Admin") || userRoles.Contains("Technician");

            if (ticket.CustomerId != userId && ticket.AssigneeId != userId && !isAdminOrTech)
            {
                throw new AuthenticationException("User does not have permission to access this ticket.");
            }
        }

        public async Task<FileDownloadDto?> GetAttachmentForDownloadAsync(int attachmentId, string userId)
        {
            var attachment = await _context.Attachments.FindAsync(attachmentId);
            if (attachment == null) return null;

            await CheckUserTicketAccess(attachment.TicketId, userId);

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", attachment.StoredPath);
            if (!File.Exists(filePath)) return null;

            var memory = new MemoryStream();
            await using (var stream = new FileStream(filePath, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            return new FileDownloadDto
            {
                FileContents = memory.ToArray(),
                ContentType = attachment.FileType ?? "application/octet-stream",
                FileName = attachment.OriginalFileName
            };
        }

        public async Task<bool> DeleteAttachmentAsync(int attachmentId, string userId)
        {
            var attachment = await _context.Attachments.FindAsync(attachmentId);
            if (attachment == null) return false;

            var userRoles = await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
                .ToListAsync();

            if (attachment.UploadedById != userId && !userRoles.Contains("Admin"))
            {
                throw new AuthenticationException("User does not have permission to delete this attachment.");
            }

            // Xóa file vật lý
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", attachment.StoredPath);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            // Xóa bản ghi trong DB
            _context.Attachments.Remove(attachment);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
