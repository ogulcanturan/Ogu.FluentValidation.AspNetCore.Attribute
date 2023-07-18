using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ogu.FluentValidation.AspNetCore.Attribute
{
    internal class InvalidValidationResponse : IInvalidValidationResponse
    {
        private readonly Func<List<ValidationFailure>, IActionResult> _invalidResponse;
        public InvalidValidationResponse(Func<List<ValidationFailure>, IActionResult> invalidResponse)
        {
            _invalidResponse = invalidResponse ?? throw new ArgumentNullException(nameof(invalidResponse));
        }

        public IActionResult GetResult(object model, List<ValidationFailure> validationFailures)
        {
            return _invalidResponse?.Invoke(validationFailures) ?? new BadRequestObjectResult(validationFailures);
        }

        public Task<IActionResult> GetResultAsync(object model, List<ValidationFailure> validationFailures, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_invalidResponse?.Invoke(validationFailures) ?? new BadRequestObjectResult(validationFailures));
        }
    }
}