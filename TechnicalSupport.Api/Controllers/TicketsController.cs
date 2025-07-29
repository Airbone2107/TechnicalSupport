using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Filters;
using System.Security.Claims;
using TechnicalSupport.Api.Common;
using TechnicalSupport.Api.SwaggerExamples.Tickets;
using TechnicalSupport.Application.Common;
using TechnicalSupport.Application.Features.Tickets.DTOs;
using TechnicalSupport.Application.Interfaces;

namespace TechnicalSupport.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class TicketsController : ControllerBase
    {
        private readonly ITicketService _ticketService;

        public TicketsController(ITicketService ticketService)
        {
            _ticketService = ticketService;
        }

        [HttpGet]
        public async Task<IActionResult> GetTickets([FromQuery] TicketFilterParams filterParams)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var pagedResultDto = await _ticketService.GetTicketsAsync(filterParams, userId!);
            return Ok(ApiResponse.Success(pagedResultDto));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTicket(int id)
        {
            var ticketDto = await _ticketService.GetTicketByIdAsync(id);
            if (ticketDto == null)
            {
                return NotFound(ApiResponse.Fail($"Ticket with Id {id} not found."));
            }
            return Ok(ApiResponse.Success(ticketDto));
        }

        [HttpPost]
        [SwaggerRequestExample(typeof(CreateTicketModel), typeof(CreateTicketModelExample))]
        public async Task<IActionResult> CreateTicket([FromBody] CreateTicketModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ticketDto = await _ticketService.CreateTicketAsync(model, userId!);
            return CreatedAtAction(nameof(GetTicket), new { id = ticketDto.TicketId }, ApiResponse.Success(ticketDto, "Ticket created successfully."));
        }

        [HttpPut("{id}/status")]
        [SwaggerRequestExample(typeof(UpdateStatusModel), typeof(UpdateStatusModelExample))]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusModel model)
        {
            var ticketDto = await _ticketService.UpdateTicketStatusAsync(id, model);
            if (ticketDto == null)
            {
                return NotFound(ApiResponse.Fail($"Ticket with Id {id} not found."));
            }
            return Ok(ApiResponse.Success(ticketDto, "Ticket status updated successfully."));
        }

        [HttpPost("{id}/comments")]
        [SwaggerRequestExample(typeof(AddCommentModel), typeof(AddCommentModelExample))]
        public async Task<IActionResult> AddComment(int id, [FromBody] AddCommentModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var commentDto = await _ticketService.AddCommentAsync(id, model, userId!);

            if (commentDto == null)
            {
                return NotFound(ApiResponse.Fail($"Ticket with Id {id} not found."));
            }

            return Ok(ApiResponse.Success(commentDto, "Comment added successfully."));
        }

        // Trong class TicketsController, thêm method sau:
        [HttpPut("{id}/assign")]
        [Authorize(Roles = "Admin,Technician")]
        public async Task<IActionResult> AssignTicket(int id, [FromBody] AssignTicketModel model)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // Lưu ý: currentUserId được truyền vào service để có thể mở rộng logic phân quyền sau này
            var ticketDto = await _ticketService.AssignTicketAsync(id, model, currentUserId!);

            if (ticketDto == null)
            {
                // Điều này có thể do không tìm thấy ticket hoặc assignee không hợp lệ.
                // Trả về NotFound là một cách xử lý chung và an toàn.
                return NotFound(ApiResponse.Fail($"Ticket with Id {id} not found or invalid assignee."));
            }
            return Ok(ApiResponse.Success(ticketDto, "Ticket assigned successfully."));
        }
    }
} 