using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Fcg.Identity.Shared.ValidationAttributes;

public class RequiredIfAttribute : ValidationAttribute
{
    private string PropertyName { get; set; }
    private object[] ExpectedValues { get; set; }
    private bool InvertCondition { get; set; }

    public RequiredIfAttribute(
        string propertyName,
        object expectedValue,
        bool invertCondition = false
    )
    {
        PropertyName = propertyName;
        ExpectedValues = [expectedValue];
        InvertCondition = invertCondition;
    }

    public RequiredIfAttribute(string propertyName, params object[] expectedValues)
    {
        PropertyName = propertyName;
        ExpectedValues = expectedValues;
        InvertCondition = false;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        var instance = context.ObjectInstance;
        var propertyInfo =
            instance.GetType().GetProperty(PropertyName) ?? throw new ArgumentException(
                $"Unknown property: {PropertyName}"
            );

        var propertyValue = propertyInfo.GetValue(instance);
        var conditionMet = ExpectedValues.Any(ev =>
            (ev == null && propertyValue == null) || (ev != null && ev.Equals(propertyValue))
        );

        if (InvertCondition)
        {
            conditionMet = !conditionMet;
        }

        if (conditionMet && string.IsNullOrWhiteSpace(value?.ToString()))
        {
            return new ValidationResult(ErrorMessage ?? $"{context.DisplayName} is required.");
        }

        return ValidationResult.Success;
    }
}
