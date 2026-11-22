using System;
using System.Collections.Generic;
using System.Text;

namespace KIRA.Application.CQRS.Command.ForgetPassword
{
    public class ForgetPasswordCommandHandler : IRequestHandler<ForgetPasswordCommand, ResultT<bool>>
    {
        private readonly IAuthService _identityService;

        public ForgetPasswordCommandHandler(IAuthService identityService)
        {
            _identityService = identityService;
        }

        public async Task<ResultT<bool>> Handle(ForgetPasswordCommand request, CancellationToken cancellationToken)
        {
            return await _identityService
                .ForgetPasswordAsync(request.EmailOrPhoneNumber, cancellationToken);
        }
    }
}
