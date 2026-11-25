using System.ComponentModel.DataAnnotations;
using StudyGraph.Domain.Entities;
namespace StudyGraph.Application.DTOs.Requests;

public sealed record ChangeStudyTaskStatusRequest(
    [property: Required, EnumDataType(typeof(StudyTaskStatus))] StudyTaskStatus? Status);
