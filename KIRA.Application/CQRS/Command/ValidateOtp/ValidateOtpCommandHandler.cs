using System;
using System.Collections.Generic;
using System.Text;

namespace KIRA.Application.CQRS.Command.ValidateOtp
{
    public class ValidateOtpCommandHandler : IRequestHandler<ValidateOtpCommand, ResultT<string>>
    {
        private readonly IAuthService _identityService;

        public ValidateOtpCommandHandler(IAuthService identityService)
        {
            _identityService = identityService;
        }
        public async Task<ResultT<string>> Handle(ValidateOtpCommand request, CancellationToken cancellationToken)
        {
            return await _identityService
                .ValidateOtpAsync(request.emailOrPhoneNumber,
                request.code, cancellationToken);

        }
    }
}
