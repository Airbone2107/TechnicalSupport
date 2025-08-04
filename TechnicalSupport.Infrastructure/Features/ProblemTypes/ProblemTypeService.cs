using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using TechnicalSupport.Application.Features.ProblemTypes.Abstractions;
using TechnicalSupport.Application.Features.ProblemTypes.DTOs;
using TechnicalSupport.Domain.Entities;
using TechnicalSupport.Infrastructure.Persistence;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TechnicalSupport.Infrastructure.Features.ProblemTypes
{
    public class ProblemTypeService : IProblemTypeService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public ProblemTypeService(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<List<ProblemTypeDto>> GetAllProblemTypesAsync()
        {
            return await _context.ProblemTypes
                .ProjectTo<ProblemTypeDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }

        public async Task<ProblemTypeDto> GetProblemTypeByIdAsync(int id)
        {
            var problemType = await _context.ProblemTypes
                .ProjectTo<ProblemTypeDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(p => p.ProblemTypeId == id);
            return problemType;
        }

        public async Task<ProblemTypeDto> CreateProblemTypeAsync(CreateProblemTypeDto model)
        {
            var problemType = _mapper.Map<ProblemType>(model);
            _context.ProblemTypes.Add(problemType);
            await _context.SaveChangesAsync();
            return _mapper.Map<ProblemTypeDto>(problemType);
        }

        public async Task<ProblemTypeDto> UpdateProblemTypeAsync(int id, UpdateProblemTypeDto model)
        {
            var problemType = await _context.ProblemTypes.FindAsync(id);
            if (problemType == null)
            {
                return null;
            }

            _mapper.Map(model, problemType);
            await _context.SaveChangesAsync();
            return _mapper.Map<ProblemTypeDto>(problemType);
        }

        public async Task<bool> DeleteProblemTypeAsync(int id)
        {
            var problemType = await _context.ProblemTypes.FindAsync(id);
            if (problemType == null)
            {
                return false;
            }

            _context.ProblemTypes.Remove(problemType);
            await _context.SaveChangesAsync();
            return true;
        }
    }
} 