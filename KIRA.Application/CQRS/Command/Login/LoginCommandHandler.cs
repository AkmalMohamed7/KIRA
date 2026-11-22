using Mapster;
using static KIRA.Application.Abstractions.Authentication.IAuthService;

namespace KIRA.Application.CQRS.Command.Login
{
    public class LoginCommandHandler : IRequestHandler<LoginCommand, ResultT<LoginDTO>>
    {
        private readonly IAuthService _identityService;

        public LoginCommandHandler(IAuthService identityService)
        {
            _identityService = identityService;
        }

        public async Task<ResultT<LoginDTO>> Handle(
            LoginCommand request,
            CancellationToken cancellationToken)
        {
            var identityRequest = request.Adapt<LoginIdentityRequest>();

            return await _identityService.LoginAsync(
                identityRequest,
                cancellationToken);
        }

    }
}
