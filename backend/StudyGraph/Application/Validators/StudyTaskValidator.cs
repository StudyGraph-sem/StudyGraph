using System.ComponentModel.DataAnnotations;
namespace StudyGraph.Application.Validators;

public sealed class StudyTaskValidator
{
    public void Validate(object request) => Validator.ValidateObject(request, new ValidationContext(request), true);
}
