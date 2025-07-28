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
        public async Task<IActionResult> GetTickets([FromQuery] PaginationParams paginationParams)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var pagedResultDto = await _ticketService.GetTicketsAsync(paginationParams, userId!);
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
    }
} 