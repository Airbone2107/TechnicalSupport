using TechnicalSupport.Application.Features.ProblemTypes.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TechnicalSupport.Application.Features.ProblemTypes.Abstractions
{
    public interface IProblemTypeService
    {
        Task<List<ProblemTypeDto>> GetAllProblemTypesAsync();
        Task<ProblemTypeDto> GetProblemTypeByIdAsync(int id);
        Task<ProblemTypeDto> CreateProblemTypeAsync(CreateProblemTypeDto model);
        Task<ProblemTypeDto> UpdateProblemTypeAsync(int id, UpdateProblemTypeDto model);
        Task<bool> DeleteProblemTypeAsync(int id);
    }
} 