using AutoMapper;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using TechnicalSupport.Application.Common;
using TechnicalSupport.Application.Features.Tickets.DTOs;
using TechnicalSupport.Application.Interfaces;
using TechnicalSupport.Domain.Entities;

namespace TechnicalSupport.Api.Services
{
    /// <summary>
    /// Mock implementation of ITicketService for testing purposes.
    /// Returns hardcoded data without interacting with the database.
    /// </summary>
    public class MockTicketService : ITicketService
    {
        private readonly IMapper _mapper;
        private readonly List<TicketDto> _mockTickets;
        private int _nextTicketId = 100; // Start mock IDs high to avoid confusion

        public MockTicketService(IMapper mapper)
        {
            _mapper = mapper;
            _mockTickets = CreateMockData();
        }

        public Task<TicketDto> CreateTicketAsync(CreateTicketModel model, string userId)
        {
            var user = new UserDto { Id = userId, DisplayName = "Mock Client", Email = "client@example.com" };
            var status = new StatusDto { StatusId = model.StatusId, Name = "Open" }; // Assume open status

            var newTicket = new TicketDto
            {
                TicketId = _nextTicketId++,
                Title = model.Title,
                Description = model.Description,
                Priority = model.Priority ?? "Medium",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Status = status,
                Customer = user,
                Assignee = null
            };

            _mockTickets.Add(newTicket);
            return Task.FromResult(newTicket);
        }

        public Task<TicketDto?> GetTicketByIdAsync(int id)
        {
            var ticket = _mockTickets.FirstOrDefault(t => t.TicketId == id) ?? _mockTickets.First();
            // Always return a ticket to simulate finding one
            return Task.FromResult<TicketDto?>(ticket);
        }

        public Task<PagedResult<TicketDto>> GetTicketsAsync(TicketFilterParams filterParams, string userId)
        {
            IEnumerable<TicketDto> filteredTickets = _mockTickets;

            if (filterParams.StatusId.HasValue)
            {
                filteredTickets = filteredTickets.Where(t => t.Status.StatusId == filterParams.StatusId.Value);
            }
            if (!string.IsNullOrEmpty(filterParams.Priority))
            {
                filteredTickets = filteredTickets.Where(t => t.Priority.Equals(filterParams.Priority, StringComparison.OrdinalIgnoreCase));
            }
            if (!string.IsNullOrEmpty(filterParams.AssigneeId))
            {
                filteredTickets = filteredTickets.Where(t => t.Assignee?.Id == filterParams.AssigneeId);
            }
            if (!string.IsNullOrEmpty(filterParams.SearchQuery))
            {
                var searchTerm = filterParams.SearchQuery.ToLower();
                filteredTickets = filteredTickets.Where(t => t.Title.ToLower().Contains(searchTerm) || t.Description.ToLower().Contains(searchTerm));
            }
            
            var items = filteredTickets
                .Skip((filterParams.PageNumber - 1) * filterParams.PageSize)
                .Take(filterParams.PageSize)
                .ToList();
                
            var pagedResult = new PagedResult<TicketDto>(
                items,
                filteredTickets.Count(),
                filterParams.PageNumber,
                filterParams.PageSize);

            return Task.FromResult(pagedResult);
        }

        public Task<TicketDto?> UpdateTicketStatusAsync(int id, UpdateStatusModel model)
        {
            var ticket = _mockTickets.FirstOrDefault(t => t.TicketId == id);
            if (ticket == null)
            {
                // To ensure a realistic response, use the first ticket if not found
                ticket = _mockTickets.First();
            }

            ticket.Status = new StatusDto { StatusId = model.StatusId, Name = "Updated In Mock" };
            ticket.UpdatedAt = DateTime.UtcNow;

            return Task.FromResult<TicketDto?>(ticket);
        }

        public Task<CommentDto?> AddCommentAsync(int ticketId, AddCommentModel model, string userId)
        {
            var user = new UserDto { Id = userId, DisplayName = "Mock User", Email = "user@example.com" };

            var newComment = new CommentDto
            {
                CommentId = new Random().Next(100, 1000),
                TicketId = ticketId,
                Content = model.Content,
                CreatedAt = DateTime.UtcNow,
                User = user
            };

            return Task.FromResult<CommentDto?>(newComment);
        }

        public Task<TicketDto?> AssignTicketAsync(int ticketId, AssignTicketModel model, string currentUserId)
        {
            var ticket = _mockTickets.FirstOrDefault(t => t.TicketId == ticketId);
            if (ticket == null)
            {
                return Task.FromResult<TicketDto?>(null);
            }

            var mockAssignee = new UserDto { Id = model.AssigneeId, DisplayName = "Mock Assignee", Email = "assignee@example.com" };
            ticket.Assignee = mockAssignee;
            ticket.UpdatedAt = DateTime.UtcNow;

            return Task.FromResult<TicketDto?>(ticket);
        }

        private List<TicketDto> CreateMockData()
        {
            var clientUser = new UserDto { Id = "client-guid-mock", DisplayName = "John Doe (Client)", Email = "client@example.com" };
            var techUser = new UserDto { Id = "tech-guid-mock", DisplayName = "Jane Smith (Tech)", Email = "tech@example.com" };

            return new List<TicketDto>
            {
                new TicketDto
                {
                    TicketId = 1,
                    Title = "Cannot login to the system (Mock)",
                    Description = "I am unable to login with my credentials. The system shows an 'Invalid credentials' error.",
                    Priority = "High",
                    CreatedAt = DateTime.UtcNow.AddDays(-5),
                    UpdatedAt = DateTime.UtcNow.AddDays(-2),
                    Status = new StatusDto { StatusId = 2, Name = "In Progress" },
                    Customer = clientUser,
                    Assignee = techUser
                },
                new TicketDto
                {
                    TicketId = 2,
                    Title = "Printer is not working (Mock)",
                    Description = "My office printer is not responding. I have checked the power and network cables.",
                    Priority = "Medium",
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    UpdatedAt = DateTime.UtcNow.AddDays(-1),
                    Status = new StatusDto { StatusId = 1, Name = "Open" },
                    Customer = clientUser,
                    Assignee = null
                }
            };
        }
    }
} 