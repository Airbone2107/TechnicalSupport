using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechnicalSupport.Api.Common;
using TechnicalSupport.Application.Features.ProblemTypes.Abstractions;
using TechnicalSupport.Application.Features.ProblemTypes.DTOs;
using System.Threading.Tasks;

namespace TechnicalSupport.Api.Features.ProblemTypes
{
    /// <summary>
    /// Quản lý các loại sự cố (problem types) trong hệ thống.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProblemTypesController : ControllerBase
    {
        private readonly IProblemTypeService _problemTypeService;

        /// <summary>
        /// Khởi tạo một instance mới của ProblemTypesController.
        /// </summary>
        public ProblemTypesController(IProblemTypeService problemTypeService)
        {
            _problemTypeService = problemTypeService;
        }

        /// <summary>
        /// Lấy danh sách tất cả các loại sự cố. Endpoint này cho phép truy cập ẩn danh.
        /// </summary>
        /// <returns>Danh sách các loại sự cố.</returns>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var result = await _problemTypeService.GetAllProblemTypesAsync();
            return Ok(ApiResponse.Success(result));
        }

        /// <summary>
        /// Tạo một loại sự cố mới.
        /// </summary>
        /// <param name="model">Thông tin của loại sự cố mới.</param>
        /// <returns>Thông tin chi tiết của loại sự cố vừa tạo.</returns>
        [HttpPost]
        [Authorize(Policy = "ManageProblemTypes")]
        public async Task<IActionResult> Create([FromBody] CreateProblemTypeDto model)
        {
            var result = await _problemTypeService.CreateProblemTypeAsync(model);
            return CreatedAtAction(nameof(GetAll), new { id = result.ProblemTypeId }, ApiResponse.Success(result));
        }

        /// <summary>
        /// Cập nhật một loại sự cố đã có.
        /// </summary>
        /// <param name="id">ID của loại sự cố.</param>
        /// <param name="model">Thông tin cập nhật.</param>
        /// <returns>Thông tin chi tiết của loại sự cố sau khi cập nhật.</returns>
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

        /// <summary>
        /// Xóa một loại sự cố.
        /// </summary>
        /// <param name="id">ID của loại sự cố cần xóa.</param>
        /// <returns>Thông báo thành công.</returns>
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