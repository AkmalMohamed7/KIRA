using System.Text.RegularExpressions;

namespace KIRA.Application.CQRS.Command.Login
{
    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(x => x.EmailOrPhoneNumber)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Phone number or email is required.")
            .Must(BeValidPhoneNumberOrEmail)
            .WithMessage("Please enter a valid email address or phone number.");

            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage("Password is required.")
                .MaximumLength(128)
                .WithMessage("Password cannot exceed 128 characters.");
        }

        private static bool BeValidPhoneNumberOrEmail(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            value = value.Trim();

            if (value.Contains('@'))
            {
                return new System.ComponentModel.DataAnnotations
                    .EmailAddressAttribute()
                    .IsValid(value);
            }

            return Regex.IsMatch(
                value,
                @"^(?:\+20|20|0)?1[0125]\d{8}$");
        }
    }
}
