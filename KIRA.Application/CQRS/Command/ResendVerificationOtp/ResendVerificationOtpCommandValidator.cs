using System;
using System.Collections.Generic;
using System.Text;

namespace KIRA.Application.CQRS.Command.ResendVerificationOtp
{
    public sealed class ResendVerificationOtpCommandValidator
    : AbstractValidator<ResendVerificationOtpCommand>
    {
        public ResendVerificationOtpCommandValidator()
        {
            RuleFor(x => x.EmailOrPhoneNumber)
                .NotEmpty()
                .WithMessage("Email or phone number is required.")
                .MaximumLength(254)
                .WithMessage("Email or phone number is too long.")
                .Must(BeValidEmailOrPhoneNumber)
                .When(x => !string.IsNullOrWhiteSpace(x.EmailOrPhoneNumber))
                .WithMessage("Please enter a valid email address or phone number.");
        }

        private static bool BeValidEmailOrPhoneNumber(string value)
        {
            value = value.Trim();

            if (value.Contains('@'))
            {
                return new System.ComponentModel.DataAnnotations.EmailAddressAttribute()
                    .IsValid(value);
            }

            return System.Text.RegularExpressions.Regex.IsMatch(
                value,
                @"^\+?[0-9]{7,15}$");
        }
    }
}
