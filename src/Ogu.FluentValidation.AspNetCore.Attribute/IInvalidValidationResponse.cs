using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ogu.FluentValidation.AspNetCore.Attribute
{
    /// <summary>
    /// Defines a contract for handling invalid validation results when the [<see cref="ValidateAttribute"/>] or [<see cref="ValidateAsyncAttribute"/>] attributes are applied.
    /// During action execution, if validation failures are detected, this interface's methods will be invoked.
    /// If the [<see cref="ValidateAsyncAttribute"/>] attribute is applied, the asynchronous method <see cref="GetResultAsync"/> will be called; 
    /// otherwise, the synchronous method <see cref="GetResult"/> is invoked.
    /// If the interface hasn't implemented or not registered, a default <see cref="BadRequestObjectResult"/> will be returned on validation failure.
    /// </summary>
    public interface IInvalidValidationResponse
    {
        /// <summary>
        /// Called when validation failures occur during the execution of a non-async action with the [<see cref="ValidateAttribute"/>].
        /// If validation errors are present, this method is invoked to generate the response.
        /// If not implemented, a default <see cref="BadRequestObjectResult"/> will be returned.
        /// </summary>
        /// <param name="model">The model that failed validation.</param>
        /// <param name="validationFailures">A list of <see cref="ValidationFailure"/> objects representing the validation errors.</param>
        /// <returns>An <see cref="IActionResult"/> that represents the response returned to the client.</returns>
        IActionResult GetResult(object model, List<ValidationFailure> validationFailures);

        /// <summary>
        /// Called when validation failures occur for an async request.
        /// This method is triggered when the [<see cref="ValidateAsyncAttribute"/>] is applied to the request.
        /// Implement this method to customize the async response returned in case of validation failures.
        /// If not implemented, a default <see cref="BadRequestObjectResult"/> will be returned.
        /// </summary>
        /// <param name="model">The model that failed validation.</param>
        /// <param name="validationFailures">A list of <see cref="ValidationFailure"/> objects representing the validation errors.</param>
        /// <param name="cancellationToken">Optional token to cancel the operation.</param>
        /// <returns>A <see cref="Task{IActionResult}"/> representing the async result to be returned as the response.</returns>
        Task<IActionResult> GetResultAsync(object model, List<ValidationFailure> validationFailures, CancellationToken cancellationToken = default);
    }
}