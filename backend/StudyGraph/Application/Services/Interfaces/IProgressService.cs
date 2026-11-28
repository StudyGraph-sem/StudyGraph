using StudyGraph.Application.DTOs.Requests;
using StudyGraph.Application.DTOs.Responses;
namespace StudyGraph.Application.Services.Interfaces;

public interface IProgressService
{
    RoadmapProgressResponse CalculateWeightedProgress(Guid userId, UserRoadmapProgressSnapshot snapshot);
}
