using System.ComponentModel.DataAnnotations;
using GamesFinder.Orchestrator.Domain.Classes;

namespace GamesFinder.Orchestrator.Utils.Annotations;

public sealed class MinimumInstantGamingId : ValidationAttribute
{
  protected override ValidationResult? IsValid(
    object? value,
    ValidationContext validationContext)
  {
    if (value is null)
      throw new ValidationException("Value cannot be null.");

    if (value is not int number)
      return new ValidationResult("Value must be an integer.");

    var options = validationContext.GetService(typeof(WorkersOptions)) as WorkersOptions;

    if (options is null)
      throw new InvalidOperationException("ScrapingOptions not registered in DI");

    if (number <= options.InstantGamingSkipFirstIds)
    {
      return new ValidationResult($"Value must be <= {options.InstantGamingSkipFirstIds}");
    }

    return ValidationResult.Success;
  }
}