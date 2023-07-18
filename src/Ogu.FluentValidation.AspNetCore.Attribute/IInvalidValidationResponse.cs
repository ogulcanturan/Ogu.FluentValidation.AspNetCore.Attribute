using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ogu.FluentValidation.AspNetCore.Attribute
{
    public interface IInvalidValidationResponse
    {
        IActionResult GetResult(object model, List<ValidationFailure> validationFailures);
        Task<IActionResult> GetResultAsync(object model, List<ValidationFailure> validationFailures, CancellationToken cancellationToken = default);
    }
}