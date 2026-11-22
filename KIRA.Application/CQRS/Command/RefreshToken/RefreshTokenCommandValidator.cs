using System;
using System.Collections.Generic;
using System.Text;

namespace KIRA.Application.CQRS.Command.RefreshToken
{
    public class RefreshTokenCommandValidator
        : AbstractValidator<RefreshTokenCommand>
    {
        public RefreshTokenCommandValidator()
        {
            RuleFor(x => x.RefreshToken)
                .NotEmpty()
                .WithMessage("Refresh token is required.")
                .MaximumLength(2000)
                .WithMessage("Refresh token must not exceed 2000 characters.");
        }
    }
}
