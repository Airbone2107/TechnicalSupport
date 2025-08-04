using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Filters;
using System.Security.Claims;
using TechnicalSupport.Api.Common;
using TechnicalSupport.Api.SwaggerExamples.Tickets;
using TechnicalSupport.Application.Common;
using TechnicalSupport.Application.Features.Tickets.Abstractions;
using TechnicalSupport.Application.Features.Tickets.DTOs;

namespace TechnicalSupport.Api.Features.Tickets
{
    [ApiController]
    [Route("[controller]")]
    [Authorize(Policy = "RequireAuthenticatedUser")]
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
            var pagedResultDto = await _ticketService.GetTicketsAsync(filterParams);
            return Ok(ApiResponse.Success(pagedResultDto));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTicket(int id)
        {
            try
            {
                var ticketDto = await _ticketService.GetTicketByIdAsync(id);
                if (ticketDto == null)
                {
                    return NotFound(ApiResponse.Fail($"Ticket with Id {id} not found."));
                }
                return Ok(ApiResponse.Success(ticketDto));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpPost]
        [Authorize(Policy = "CreateTickets")]
        public async Task<IActionResult> CreateTicket([FromBody] CreateTicketModel model)
        {
            var ticketDto = await _ticketService.CreateTicketAsync(model);
            return CreatedAtAction(nameof(GetTicket), new { id = ticketDto.TicketId }, ApiResponse.Success(ticketDto, "Ticket created successfully."));
        }
        
        [HttpPost("{id}/claim")]
        [Authorize(Policy = "ClaimTickets")]
        public async Task<IActionResult> ClaimTicket(int id)
        {
            try
            {
                var ticketDto = await _ticketService.ClaimTicketAsync(id);
                return Ok(ApiResponse.Success(ticketDto, "Ticket claimed successfully."));
            }
            catch (KeyNotFoundException ex) { return NotFound(ApiResponse.Fail(ex.Message)); }
            catch (InvalidOperationException ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
            catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail(ex.Message)); }
        }
        
        [HttpPost("{id}/reject-from-group")]
        [Authorize(Policy = "RejectFromGroup")]
        public async Task<IActionResult> RejectFromGroup(int id)
        {
            try
            {
                var ticketDto = await _ticketService.RejectFromGroupAsync(id);
                return Ok(ApiResponse.Success(ticketDto, "Ticket has been returned to the triage queue."));
            }
            catch (KeyNotFoundException ex) { return NotFound(ApiResponse.Fail(ex.Message)); }
            catch (InvalidOperationException ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
            catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail(ex.Message)); }
        }

        [HttpPut("{id}/status")]
        [Authorize(Policy = "UpdateTicketStatus")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusModel model)
        {
            try
            {
                var ticketDto = await _ticketService.UpdateTicketStatusAsync(id, model);
                if (ticketDto == null) return NotFound(ApiResponse.Fail($"Ticket with Id {id} not found."));
                return Ok(ApiResponse.Success(ticketDto, "Ticket status updated successfully."));
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpPost("{id}/comments")]
        [Authorize(Policy = "AddComments")]
        public async Task<IActionResult> AddComment(int id, [FromBody] AddCommentModel model)
        {
            try
            {
                var commentDto = await _ticketService.AddCommentAsync(id, model);
                if (commentDto == null) return NotFound(ApiResponse.Fail($"Ticket with Id {id} not found."));
                return Ok(ApiResponse.Success(commentDto, "Comment added successfully."));
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpPut("{id}/assign")]
        public async Task<IActionResult> AssignTicket(int id, [FromBody] AssignTicketModel model)
        {
            try
            {
                var ticketDto = await _ticketService.AssignTicketAsync(id, model);
                return Ok(ApiResponse.Success(ticketDto, "Ticket assigned successfully."));
            }
            catch (KeyNotFoundException ex) { return NotFound(ApiResponse.Fail(ex.Message)); }
            catch (InvalidOperationException ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
            catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail(ex.Message)); }
        }

        [HttpPut("{id}/assign-group")]
        public async Task<IActionResult> AssignTicketToGroup(int id, [FromBody] AssignGroupModel model)
        {
            try
            {
                var ticketDto = await _ticketService.AssignTicketToGroupAsync(id, model);
                return Ok(ApiResponse.Success(ticketDto, "Ticket assigned to group successfully."));
            }
            catch (KeyNotFoundException ex) { return NotFound(ApiResponse.Fail(ex.Message)); }
            catch (InvalidOperationException ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
            catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail(ex.Message)); }
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "DeleteTickets")]
        public async Task<IActionResult> DeleteTicket(int id)
        {
            var (success, message) = await _ticketService.DeleteTicketAsync(id);
            if (!success) return NotFound(ApiResponse.Fail(message));
            return Ok(ApiResponse.Success<object>(null, message));
        }
    }
} 