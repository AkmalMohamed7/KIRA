namespace KIRA.Application.CQRS.Command.Register
{
    public class RegisterCommandValidator
        : AbstractValidator<RegisterCommand>
    {
        public RegisterCommandValidator()
        {
            RuleFor(x => x.FullName)
                .NotEmpty()
                .WithMessage("Full name is required.")
                .MaximumLength(150);

            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("Email is required.")
                .EmailAddress()
                .WithMessage("Invalid email format.")
                .MaximumLength(256);

            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage("Password is required.")
                .MinimumLength(8)
                .WithMessage("Password must be at least 8 characters.")
                .Matches("[A-Z]")
                .WithMessage("Password must contain at least one uppercase letter.")
                .Matches("[a-z]")
                .WithMessage("Password must contain at least one lowercase letter.")
                .Matches("[0-9]")
                .WithMessage("Password must contain at least one digit.");

            RuleFor(x => x.PhoneNumber)
                .NotEmpty()
                .WithMessage("Phone number is required.")
                .MaximumLength(20);

            RuleFor(x => x.BirthDate)
                .NotEmpty()
                .WithMessage("Birth date is required.")
                .LessThan(DateOnly.FromDateTime(DateTime.Today))
                .WithMessage("Birth date must be in the past.");

            RuleFor(x => x.Interests)
                .NotNull()
                .WithMessage("Interests are required.");

            RuleForEach(x => x.Interests)
                .NotEmpty()
                .WithMessage("Interest cannot be empty.")
                .MaximumLength(100);

            RuleFor(x => x.Industry)
                .NotEmpty()
                .WithMessage("Industry is required.")
                .MaximumLength(100);

            RuleFor(x => x.BusinessDomain)
                .NotEmpty()
                .WithMessage("Business domain is required.")
                .MaximumLength(200);

            RuleFor(x => x.JobTitle)
                .NotEmpty()
                .WithMessage("Job title is required.")
                .MaximumLength(100);

            RuleFor(x => x.CompanyName)
                .NotEmpty()
                .WithMessage("Company name is required.")
                .MaximumLength(200);
        }
    }
}