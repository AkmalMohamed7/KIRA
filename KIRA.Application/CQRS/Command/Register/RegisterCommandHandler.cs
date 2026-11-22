namespace KIRA.Application.CQRS.Command.Register
{
    public class RegisterCommandHandler
        : IRequestHandler<RegisterCommand, ResultT<Guid>>
    {
        private readonly IAuthService _authService;

        public RegisterCommandHandler(IAuthService authService)
        {
            _authService = authService;
        }

        public async Task<ResultT<Guid>> Handle(
            RegisterCommand request,
            CancellationToken cancellationToken)
        {
            var registerRequest = new IAuthService.RegisterIdentityRequest(
                request.FullName,
                request.Email,
                request.Password,
                request.PhoneNumber,
                request.BirthDate,
                request.Interests,
                request.Industry,
                request.BusinessDomain,
                request.JobTitle,
                request.CompanyName
            );

            return await _authService.RegisterAsync(
                registerRequest,
                cancellationToken);
        }
    }
}