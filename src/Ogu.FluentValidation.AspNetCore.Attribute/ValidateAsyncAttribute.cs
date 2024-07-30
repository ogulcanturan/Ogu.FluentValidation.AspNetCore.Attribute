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
using System.Threading.Tasks;

namespace Ogu.FluentValidation.AspNetCore.Attribute
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class ValidateAsyncAttribute : ActionFilterAttribute
    {
        private static readonly Lazy<ConcurrentDictionary<Type, (Type, MethodInfo)>>
            LazyModelTypeToGenericTypeAndMethodInfoTuple =
                new Lazy<ConcurrentDictionary<Type, (Type, MethodInfo)>>(LazyThreadSafetyMode.ExecutionAndPublication);

        public ValidateAsyncAttribute(Type modelType, int order = 0, bool isCancellationTokenActive = true) : this(new[] { modelType }, order, isCancellationTokenActive) { }

        public ValidateAsyncAttribute(Type[] modelTypes, int order = 0, bool isCancellationTokenActive = true)
        {
            ModelTypes = modelTypes ?? throw new ArgumentNullException(nameof(modelTypes));
            IsCancellationTokenActive = isCancellationTokenActive;
            Order = order;
        }

        public Type[] ModelTypes { get; }

        public bool IsCancellationTokenActive { get; }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!(context.ActionDescriptor is ControllerActionDescriptor controllerActionDescriptor))
            {
                await base.OnActionExecutionAsync(context, next);
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
                await base.OnActionExecutionAsync(context, next);
                return;
            }

            var modelTypeToGenericTypeAndMethodInfoTuple = LazyModelTypeToGenericTypeAndMethodInfoTuple.Value;

            var cancellationToken = IsCancellationTokenActive
                ? context.HttpContext.RequestAborted
                : CancellationToken.None;

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

                    var validationResult = await (Task<ValidationResult>)genericTypeAndMethodInfo.Item2.Invoke(service, new[] { model, cancellationToken });

                    await HandleValidationResultAsync(context, validationResult, model, cancellationToken);
                }
                else
                {
                    var validatorType = typeof(IValidator<>).MakeGenericType(modelType);

                    var resolvedValidatorFromDependencyInjection = context.HttpContext.RequestServices.GetRequiredService(validatorType);

                    var validateMethod = validatorType.GetMethod(InternalConstants.ValidateAsyncMethodName, new[] { modelType, InternalConstants.CancellationTokenType }) ?? throw new Exception($"The package 'FluentValidation' version '{typeof(IValidator).Assembly.GetName().Version}' is not supported.");

                    modelTypeToGenericTypeAndMethodInfoTuple.TryAdd(modelType, (validatorType, validateMethod));

                    var validationResult = await (Task<ValidationResult>)validateMethod.Invoke(resolvedValidatorFromDependencyInjection, new[] { model, cancellationToken });

                    await HandleValidationResultAsync(context, validationResult, model, cancellationToken);
                }
            }

            await base.OnActionExecutionAsync(context, next);
        }

        private static async Task HandleValidationResultAsync(ActionExecutingContext context, ValidationResult validationResult, object model, CancellationToken cancellationToken)
        {
            if (validationResult.IsValid)
            {
                return;
            }

            context.Result = context.HttpContext.RequestServices.GetService(typeof(IInvalidValidationResponse)) is IInvalidValidationResponse invalidResponse
                ? await invalidResponse.GetResultAsync(model, validationResult.Errors, cancellationToken)
                : new BadRequestObjectResult(validationResult.Errors);
        }
    }
}