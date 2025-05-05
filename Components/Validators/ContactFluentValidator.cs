using DogTracker.Models;
using FluentValidation;

namespace DogTracker.Components.Validators;

public class ContactFluentValidator : AbstractValidator<Contact>
{
    public ContactFluentValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Le nom est requis.")
            .Length(1, 100);

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Le type est requis.")
            .Length(1, 100);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("L'email est requis.")
            .EmailAddress().WithMessage("L'email n'est pas valide.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Le téléphone est requis.")
            .Length(1, 50);

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("L'adresse est requise.")
            .Length(1, 200);
    }

    public Func<object, string, Task<IEnumerable<string>>> ValidateValue => async (model, propertyName) =>
    {
        var result = await ValidateAsync(ValidationContext<Contact>.CreateWithOptions((Contact)model, x => x.IncludeProperties(propertyName)));
        if (result.IsValid)
            return Array.Empty<string>();
        return result.Errors.Select(e => e.ErrorMessage);
    };
}