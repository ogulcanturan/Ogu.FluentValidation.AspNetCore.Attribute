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
    /// <summary>
    ///     An attribute that validates the specified model asynchronously before an action method is invoked.
    /// </summary>
    /// <remarks>
    ///     This attribute can be applied to both methods and classes. 
    ///     It is used to ensure that the models passed to the action method meet validation criteria before further processing.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class ValidateAsyncAttribute : ActionFilterAttribute
    {
        private static readonly Lazy<ConcurrentDictionary<Type, (Type genericValidatorTType, MethodInfo validateMethod)>>
            LazyModelTypeToGenericValidatorTTypeAndValidateMethodInfoTuple =
                new Lazy<ConcurrentDictionary<Type, (Type, MethodInfo)>>(LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        ///     Initializes a new instance of the <see cref="ValidateAttribute"/> class for a single model type.
        /// </summary>
        /// <param name="modelType">The type of the model to validate.</param>
        /// <param name="order">The order in which the action filter attribute is applied. Default is 0.</param>
        /// <param name="isCancellationTokenActive"></param>
        public ValidateAsyncAttribute(Type modelType, int order = 0, bool isCancellationTokenActive = true) : this(new[] { modelType }, order, isCancellationTokenActive) { }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ValidateAsyncAttribute"/> class for multiple model types.
        /// </summary>
        /// <param name="modelTypes">An array of model types to validate.</param>
        /// <param name="order">The order in which the action filter attribute is applied. Default is 0.</param>
        /// <param name="isCancellationTokenActive"></param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="modelTypes"/> is null.</exception>
        public ValidateAsyncAttribute(Type[] modelTypes, int order = 0, bool isCancellationTokenActive = true)
        {
            ModelTypes = modelTypes ?? throw new ArgumentNullException(nameof(modelTypes));
            IsCancellationTokenActive = isCancellationTokenActive;
            Order = order;
        }

        /// <summary>
        ///     An array of model types to validate.
        /// </summary>
        public Type[] ModelTypes { get; }

        /// <summary>
        ///     Indicates whether the active <see cref="ActionContext.HttpContext"/> cancellation token is being used.
        ///     If <c>true</c>, the cancellation token from the current <see cref="ActionContext.HttpContext"/> will be utilized; 
        ///     otherwise, no cancellation token will be applied.
        /// </summary>
        /// <remarks>
        ///     The default value is <c>true</c>
        /// </remarks>
        public bool IsCancellationTokenActive { get; }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!(context.ActionDescriptor is ControllerActionDescriptor controllerActionDescriptor))
            {
                await base.OnActionExecutionAsync(context, next);
                return;
            }

            if (!InternalConstants.ActionUuidToHasSkipValidateAttribute.Value.TryGetValue(controllerActionDescriptor.Id, out var hasSkipValidateAttribute))
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

            var modelTypeToGenericValidatorTTypeAndValidateMethodInfoTuple = LazyModelTypeToGenericValidatorTTypeAndValidateMethodInfoTuple.Value;

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

                await (modelTypeToGenericValidatorTTypeAndValidateMethodInfoTuple.TryGetValue(modelType, out var genericValidatorTTypeAndValidateMethodInfo)
                    ? ProcessCachedAsync(context, genericValidatorTTypeAndValidateMethodInfo, model, cancellationToken)
                    : ProcessAsync(context, modelType, modelTypeToGenericValidatorTTypeAndValidateMethodInfoTuple, model, cancellationToken));
            }

            await base.OnActionExecutionAsync(context, next);
        }

        private static async Task ProcessCachedAsync(ActionExecutingContext context, (Type genericValidatorTType, MethodInfo validateMethodInfo) tuple, object model, CancellationToken cancellationToken)
        {
            var service = context.HttpContext.RequestServices.GetService(tuple.genericValidatorTType);

            var validationResult = await (Task<ValidationResult>)tuple.validateMethodInfo.Invoke(service, new[] { model, cancellationToken });

            await HandleValidationResultAsync(context, validationResult, model, cancellationToken);
        }

        private static async Task ProcessAsync(ActionExecutingContext context, Type modelType, ConcurrentDictionary<Type, (Type, MethodInfo)> modelTypeToGenericValidatorTTypeAndValidateMethodInfoTuple, object model, CancellationToken cancellationToken)
        {
            var validatorType = InternalConstants.IValidatorTType.MakeGenericType(modelType);

            var resolvedValidatorFromDependencyInjection = context.HttpContext.RequestServices.GetRequiredService(validatorType);

            var validateMethod = validatorType.GetMethod(InternalConstants.ValidateAsyncMethodName, new[] { modelType, InternalConstants.CancellationTokenType }) ?? throw new Exception($"The package 'FluentValidation' version '{InternalConstants.IValidatorTType.Assembly.GetName().Version}' is not supported.");

            modelTypeToGenericValidatorTTypeAndValidateMethodInfoTuple.TryAdd(modelType, (validatorType, validateMethod));

            var validationResult = await (Task<ValidationResult>)validateMethod.Invoke(resolvedValidatorFromDependencyInjection, new[] { model, cancellationToken });

            await HandleValidationResultAsync(context, validationResult, model, cancellationToken);
        }

        private static async Task HandleValidationResultAsync(ActionExecutingContext context, ValidationResult validationResult, object model, CancellationToken cancellationToken)
        {
            if (validationResult.IsValid)
            {
                return;
            }

            context.Result = context.HttpContext.RequestServices.GetService(InternalConstants.IInvalidValidationResponseType) is IInvalidValidationResponse invalidResponse
                ? await invalidResponse.GetResultAsync(model, validationResult.Errors, cancellationToken)
                : new BadRequestObjectResult(validationResult.Errors);
        }
    }
}