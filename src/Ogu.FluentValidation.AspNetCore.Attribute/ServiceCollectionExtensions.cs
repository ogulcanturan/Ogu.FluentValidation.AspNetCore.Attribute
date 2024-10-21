using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Ogu.FluentValidation.AspNetCore.Attribute;
using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers an implementation of <see cref="IInvalidValidationResponse"/> in the service collection.
        /// This method allows you to provide a custom response for validation failures.
        /// </summary>
        /// <param name="services">The service collection to which the implementation will be added.</param>
        /// <param name="invalidResponse">
        /// A function that takes a list of <see cref="ValidationFailure"/> objects 
        /// and returns an <see cref="IActionResult"/>. 
        /// This function is invoked when validation failures occur.
        /// </param>
        /// <returns>The updated <see cref="IServiceCollection"/> with the registered implementation.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="invalidResponse"/> is <c>null</c>.
        /// </exception>
        public static IServiceCollection AddInvalidValidationResponse(this IServiceCollection services, Func<List<ValidationFailure>, IActionResult> invalidResponse)
        {
            invalidResponse = invalidResponse ?? throw new ArgumentNullException(nameof(invalidResponse));

            services.AddSingleton<IInvalidValidationResponse>(new InvalidValidationResponse(invalidResponse));

            return services;
        }
    }
}