using System.ComponentModel.DataAnnotations;
using StudyGraph.Domain.Entities;
namespace StudyGraph.Application.DTOs.Requests;

public sealed record StudyTaskListRequest(
    [property: EnumDataType(typeof(StudyTaskStatus))] StudyTaskStatus? Status = null,
    [property: EnumDataType(typeof(StudyTaskPriority))] StudyTaskPriority? Priority = null,
    bool? Overdue = null,
    bool DueDescending = false,
    [property: Range(1, 1000000)] int Page = 1,
    [property: Range(1, 100)] int PageSize = 20);
