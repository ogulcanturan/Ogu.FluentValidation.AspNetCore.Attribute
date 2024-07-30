﻿using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Sample.Api.RequestValidators.Models;

namespace Sample.Api.RequestValidators
{
    public static class RequestValidatorsServiceCollectionExtensions
    {
        public static IServiceCollection AddRequestValidators(this IServiceCollection services)
        {
            // Singleton or Scoped based on your Validator
            services.AddSingleton<IValidator<GetSampleRequestValidatorModel>, GetSampleRequestValidator>();

            return services;
        }
    }
}