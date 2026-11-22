using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.RegularExpressions;

namespace KIRA.Application.CQRS.Command.ResetPassword
{
    public sealed class ResetPasswordCommandValidator
   : AbstractValidator<ResetPasswordCommand>
    {
        public ResetPasswordCommandValidator()
        {
            RuleFor(x => x.EmailOrPhoneNumber)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("Email or phone number is required.")
                .Must(BeValidEmailOrPhoneNumber)
                .WithMessage("Please enter a valid email address or phone number.");

            RuleFor(x => x.ResetToken)
                .NotEmpty()
                .WithMessage("Reset token is required.");

            RuleFor(x => x.NewPassword)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("New password is required.")
                .MinimumLength(8)
                .WithMessage("Password must contain at least 8 characters.");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty()
                .WithMessage("Password confirmation is required.")
                .Equal(x => x.NewPassword)
                .WithMessage("Password and confirmation password do not match.");
        }

        private static bool BeValidEmailOrPhoneNumber(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            value = value.Trim();

            if (value.Contains('@'))
                return new EmailAddressAttribute().IsValid(value);

            return Regex.IsMatch(
                value,
                @"^(?:\+20|20|0)?1[0125]\d{8}$");
        }
    }

}
