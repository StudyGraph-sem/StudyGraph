using System.ComponentModel.DataAnnotations;
using StudyGraph.Domain.Entities;
namespace StudyGraph.Application.DTOs.Requests;

public sealed record UpdateStudyTaskRequest(
    [property: Required, StringLength(200)] string Title,
    [property: StringLength(5000)] string? Description = null,
    [property: EnumDataType(typeof(StudyTaskPriority))] StudyTaskPriority Priority = StudyTaskPriority.Normal,
    DateTimeOffset? DueAt = null);
