
namespace KIRA.Application.CQRS.Command.Register
{
    /// <summary>
    /// Represents the data required to register a new user.
    /// </summary>
    public sealed record RegisterCommand(
        /// <summary>Full name of the user.</summary>
        string FullName,

        /// <summary>User's email address.</summary>
        string Email,

        /// <summary>User's account password.</summary>
        string Password,

        /// <summary>User's phone number.</summary>
        string PhoneNumber,

        /// <summary>User's date of birth.</summary>
        DateOnly BirthDate,

        /// <summary>List of the user's selected interests.</summary>
        List<string> Interests,

        /// <summary>User's industry, such as Healthcare or Retail.</summary>
        string Industry,

        /// <summary>Specific business domain within the selected industry.</summary>
        string BusinessDomain,

        /// <summary>User's professional job title.</summary>
        string JobTitle,

        /// <summary>Name of the user's company.</summary>
        string CompanyName
    ) : IRequest<ResultT<Guid>>;
}
