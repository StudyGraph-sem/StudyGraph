using System.ComponentModel.DataAnnotations;
using StudyGraph.Domain.Entities;
namespace StudyGraph.Application.DTOs.Requests;
 
 public sealed record UpdateNoteRequest(
    [property: Required]
    [property: StringLength(200)]
    string Title,
        
    [property: Required]
    string Content
);