using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace KIRA.Application.CQRS.Command.ForgetPassword
{
    public class ForgetPasswordCommandValidator : AbstractValidator<ForgetPasswordCommand>
    {
        public ForgetPasswordCommandValidator()
        {
            RuleFor(x => x.EmailOrPhoneNumber)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("Phone number or email is required.")
                .Must(BeValidPhoneNumberOrEmail)
                .WithMessage("Please enter a valid email address or phone number.");
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
