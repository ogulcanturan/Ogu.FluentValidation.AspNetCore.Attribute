using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace Ogu.FluentValidation.AspNetCore.Attribute
{
    public static class Extensions
    {
        public static IServiceCollection AddInvalidValidationResponse(this IServiceCollection services, Func<List<ValidationFailure>, IActionResult> invalidResponse)
        {
            invalidResponse = invalidResponse ?? throw new ArgumentNullException(nameof(invalidResponse));

            services.AddSingleton<IInvalidValidationResponse>(new InvalidValidationResponse(invalidResponse));

            return services;
        }
    }
}