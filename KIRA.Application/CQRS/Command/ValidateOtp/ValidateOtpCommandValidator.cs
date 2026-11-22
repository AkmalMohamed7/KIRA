using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.RegularExpressions;

namespace KIRA.Application.CQRS.Command.ValidateOtp
{
    public sealed class ValidateOtpCommandValidator
         : AbstractValidator<ValidateOtpCommand>
    {
        public ValidateOtpCommandValidator()
        {
            RuleFor(x => x.emailOrPhoneNumber)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("Email or phone number is required.")
                .Must(BeValidEmailOrPhoneNumber)
                .WithMessage("Please enter a valid email address or phone number.");

            RuleFor(x => x.code)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("Verification code is required.")
                .Length(6)
                .WithMessage("Verification code must contain exactly 6 digits.")
                .Matches(@"^[0-9]{6}$")
                .WithMessage("Verification code must contain digits only.");
        }

        private static bool BeValidEmailOrPhoneNumber(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            value = value.Trim();

            if (value.Contains('@'))
            {
                return new EmailAddressAttribute().IsValid(value);
            }

            return Regex.IsMatch(
                value,
                @"^(?:\+20|20|0)?1[0125]\d{8}$");
        }
    }
}
