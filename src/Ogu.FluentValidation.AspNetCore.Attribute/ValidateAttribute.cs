using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Ogu.FluentValidation.AspNetCore.Attribute
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class ValidateAttribute : ActionFilterAttribute
    {
        private static readonly Lazy<ConcurrentDictionary<Type, (Type, MethodInfo)>>
            LazyModelTypeToGenericTypeAndMethodInfoTuple =
                new Lazy<ConcurrentDictionary<Type, (Type, MethodInfo)>>(LazyThreadSafetyMode.ExecutionAndPublication);

        public ValidateAttribute(Type modelType, int order = 0) : this(new[] { modelType }, order) { }

        public ValidateAttribute(Type[] modelTypes, int order = 0)
        {
            ModelTypes = modelTypes ?? throw new ArgumentNullException(nameof(modelTypes));
            Order = order;
        }

        public Type[] ModelTypes { get; }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!(context.ActionDescriptor is ControllerActionDescriptor controllerActionDescriptor))
            {
                base.OnActionExecuting(context);
                return;
            }

            if (!InternalConstants.ActionUuidToHasSkipValidateAttribute.Value.TryGetValue(
                    controllerActionDescriptor.Id, out var hasSkipValidateAttribute))
            {
                hasSkipValidateAttribute = controllerActionDescriptor.MethodInfo.GetCustomAttribute<SkipValidateAttribute>() != null;

                InternalConstants.ActionUuidToHasSkipValidateAttribute.Value.AddOrUpdate(
                    controllerActionDescriptor.Id, _ => hasSkipValidateAttribute, (id, currentValue) => hasSkipValidateAttribute);
            }

            if (hasSkipValidateAttribute)
            {
                base.OnActionExecuting(context);
                return;
            }

            var modelTypeToGenericTypeAndMethodInfoTuple = LazyModelTypeToGenericTypeAndMethodInfoTuple.Value;

            foreach (var modelType in ModelTypes)
            {
                var model = context.ActionArguments.Values.FirstOrDefault(x => modelType.IsInstanceOfType(x));

                if (model == null)
                {
                    continue;
                }

                if (modelTypeToGenericTypeAndMethodInfoTuple.TryGetValue(modelType, out var genericTypeAndMethodInfo))
                {
                    var service = context.HttpContext.RequestServices.GetService(genericTypeAndMethodInfo.Item1);

                    var validationResult = (ValidationResult)genericTypeAndMethodInfo.Item2.Invoke(service, new[] { model });

                    HandleValidationResult(context, validationResult, model);
                }
                else
                {
                    var validatorType = typeof(IValidator<>).MakeGenericType(modelType);

                    var resolvedValidatorFromDependencyInjection = context.HttpContext.RequestServices.GetRequiredService(validatorType);

                    var validateMethod = validatorType.GetMethod(InternalConstants.ValidateMethodName, new[] { modelType }) ?? throw new Exception($"The package 'FluentValidation' version '{typeof(IValidator).Assembly.GetName().Version}' is not supported.");

                    modelTypeToGenericTypeAndMethodInfoTuple.TryAdd(modelType, (validatorType, validateMethod));

                    var validationResult = (ValidationResult)validateMethod.Invoke(resolvedValidatorFromDependencyInjection, new[] { model });

                    HandleValidationResult(context, validationResult, model);
                }
            }

            base.OnActionExecuting(context);
        }

        private static void HandleValidationResult(ActionExecutingContext context, ValidationResult validationResult, object model)
        {
            if (validationResult.IsValid)
            {
                return;
            }

            context.Result = context.HttpContext.RequestServices.GetService(typeof(IInvalidValidationResponse)) is IInvalidValidationResponse invalidResponse
                ? invalidResponse.GetResult(model, validationResult.Errors)
                : new BadRequestObjectResult(validationResult.Errors);
        }
    }
}