using System;
using System.Collections.Generic;
using System.Text;

namespace KIRA.Application.CQRS.Command.ResendVerificationOtp
{
    public sealed class ResendVerificationOtpCommandHandler
    : IRequestHandler<ResendVerificationOtpCommand, ResultT<bool>>
    {
        private readonly IAuthService _identityService;

        public ResendVerificationOtpCommandHandler(
            IAuthService   identityService)
        {
            _identityService = identityService;
        }

        public async Task<ResultT<bool>> Handle(
            ResendVerificationOtpCommand request,
            CancellationToken cancellationToken)
        {
            return await _identityService.ResendVerificationOtpAsync(
                request.EmailOrPhoneNumber.Trim(),
                cancellationToken);
        }
    }
}
