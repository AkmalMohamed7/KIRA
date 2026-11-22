using System;
using System.Collections.Generic;
using System.Text;

namespace KIRA.Application.CQRS.Command.VerifyEmail
{
    public class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, ResultT<bool>>
    {
        private readonly IAuthService _identityService;

        public VerifyEmailCommandHandler(
            IAuthService identityService)
        {
            _identityService = identityService;
        }

        public async Task<ResultT<bool>> Handle(
            VerifyEmailCommand request,
            CancellationToken cancellationToken)
        {
            return await _identityService.VerifyEmailAsync(
                request.Email,
                request.Code,
                cancellationToken);
        }
    }
}
