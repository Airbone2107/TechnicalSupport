using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechnicalSupport.Api.Common;
using TechnicalSupport.Application.Features.ProblemTypes.Abstractions;
using TechnicalSupport.Application.Features.ProblemTypes.DTOs;
using System.Threading.Tasks;

namespace TechnicalSupport.Api.Features.ProblemTypes
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProblemTypesController : ControllerBase
    {
        private readonly IProblemTypeService _problemTypeService;

        public ProblemTypesController(IProblemTypeService problemTypeService)
        {
            _problemTypeService = problemTypeService;
        }

        [HttpGet]
        [AllowAnonymous] // Cho phép tất cả người dùng (kể cả chưa đăng nhập) lấy danh sách để tạo ticket
        public async Task<IActionResult> GetAll()
        {
            var result = await _problemTypeService.GetAllProblemTypesAsync();
            return Ok(ApiResponse.Success(result));
        }

        [HttpPost]
        [Authorize(Policy = "ManageProblemTypes")]
        public async Task<IActionResult> Create([FromBody] CreateProblemTypeDto model)
        {
            var result = await _problemTypeService.CreateProblemTypeAsync(model);
            return CreatedAtAction(nameof(GetAll), new { id = result.ProblemTypeId }, ApiResponse.Success(result));
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "ManageProblemTypes")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateProblemTypeDto model)
        {
            var result = await _problemTypeService.UpdateProblemTypeAsync(id, model);
            if (result == null)
            {
                return NotFound(ApiResponse.Fail("Problem type not found."));
            }
            return Ok(ApiResponse.Success(result));
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "ManageProblemTypes")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _problemTypeService.DeleteProblemTypeAsync(id);
            if (!success)
            {
                return NotFound(ApiResponse.Fail("Problem type not found."));
            }
            return Ok(ApiResponse.Success<object>(null, "Problem type deleted successfully."));
        }
    }
} 