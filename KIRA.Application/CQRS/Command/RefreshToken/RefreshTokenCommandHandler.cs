using System;
using System.Collections.Generic;
using System.Text;

namespace KIRA.Application.CQRS.Command.RefreshToken
{
    public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, ResultT<RefreshTokenResponse>>
    {
        private readonly IAuthService _identityService;

        public RefreshTokenCommandHandler(IAuthService identityService)
        {
            _identityService = identityService;
        }

        public async Task<ResultT<RefreshTokenResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            return await _identityService.RefreshTokenAsync(
                request.RefreshToken,
                cancellationToken);
        }
    }
}
