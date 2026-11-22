using System;
using System.Collections.Generic;
using System.Text;

namespace KIRA.Application.Behaviors
{
    public class ValidationBehavior<TRequest, TResponse>
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
        where TResponse : Result
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            if (!_validators.Any())
                return await next();

            var context = new ValidationContext<TRequest>(request);

            var validationResults = await Task.WhenAll(
                _validators.Select(validator =>
                    validator.ValidateAsync(context, cancellationToken)));

            var errors = validationResults
                .SelectMany(result => result.Errors)
                .Where(failure => failure is not null)
                .Select(failure => new Error(
                    failure.PropertyName,
                    failure.ErrorMessage,
                    ErrorType.Validation))
                .ToList();

            if (errors.Count == 0)
                return await next();

            return CreateFailureResponse(errors);
        }

        private static TResponse CreateFailureResponse(List<Error> errors)
        {
            var responseType = typeof(TResponse);

            if (responseType.IsGenericType &&
                responseType.GetGenericTypeDefinition() == typeof(ResultT<>))
            {
                var genericType = responseType.GetGenericArguments()[0];

                var resultType = typeof(ResultT<>).MakeGenericType(genericType);

                var failuresMethod = resultType.GetMethod(
                    nameof(ResultT<object>.Failures));

                return (TResponse)failuresMethod!.Invoke(null, new object[] { errors })!;
            }

            return (TResponse)(object)Result.Failure(errors);
        }
    }
}
