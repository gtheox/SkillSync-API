using SkillSync.API.DTOs.AI;

namespace SkillSync.API.Services;

public interface IAIService
{
    Task<MatchResponse> GerarMatchesAsync(MatchRequest request);
}

