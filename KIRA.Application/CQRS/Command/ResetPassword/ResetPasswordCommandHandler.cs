using System;
using System.Collections.Generic;
using System.Text;

namespace KIRA.Application.CQRS.Command.ResetPassword
{
    public sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, ResultT<bool>>
    {
        private readonly IAuthService _identityService;
        public ResetPasswordCommandHandler(IAuthService identityService)
        {
            _identityService = identityService;
        }
        public async Task<ResultT<bool>> Handle(ResetPasswordCommand request,
            CancellationToken cancellationToken)
        {
            return await _identityService
                .ResetPasswordAsync(request.EmailOrPhoneNumber,
                request.ResetToken, request.NewPassword,
                request.ConfirmPassword, cancellationToken);

        }
    }
}
