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
    /// <summary>
    ///     An attribute that validates the specified model synchronously before an action method is invoked.
    /// </summary>
    /// <remarks>
    ///     This attribute can be applied to both methods and classes. 
    ///     It is used to ensure that the models passed to the action method meet validation criteria before further processing.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class ValidateAttribute : ActionFilterAttribute
    {
        private static readonly Lazy<ConcurrentDictionary<Type, (Type genericValidatorTType, MethodInfo validateMethod)>>
            LazyModelTypeToGenericValidatorTTypeAndValidateMethodInfoTuple =
                new Lazy<ConcurrentDictionary<Type, (Type, MethodInfo)>>(LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        ///     Initializes a new instance of the <see cref="ValidateAttribute"/> class for a single model type.
        /// </summary>
        /// <param name="modelType">The type of the model to validate.</param>
        /// <param name="order">The order in which the action filter attribute is applied. Default is 0.</param>
        public ValidateAttribute(Type modelType, int order = 0) : this(new[] { modelType }, order) { }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ValidateAttribute"/> class for multiple model types.
        /// </summary>
        /// <param name="modelTypes">An array of model types to validate.</param>
        /// <param name="order">The order in which the action filter attribute is applied. Default is 0.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="modelTypes"/> is null.</exception>
        public ValidateAttribute(Type[] modelTypes, int order = 0)
        {
            ModelTypes = modelTypes ?? throw new ArgumentNullException(nameof(modelTypes));
            Order = order;
        }

        /// <summary>
        ///     An array of model types to validate.
        /// </summary>
        public Type[] ModelTypes { get; }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!(context.ActionDescriptor is ControllerActionDescriptor controllerActionDescriptor))
            {
                base.OnActionExecuting(context);
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
                base.OnActionExecuting(context);
                return;
            }

            var modelTypeToGenericValidatorTTypeAndValidateMethodInfoTuple = LazyModelTypeToGenericValidatorTTypeAndValidateMethodInfoTuple.Value;

            foreach (var modelType in ModelTypes)
            {
                var model = context.ActionArguments.Values.FirstOrDefault(x => modelType.IsInstanceOfType(x));

                if (model == null)
                {
                    continue;
                }

                if (modelTypeToGenericValidatorTTypeAndValidateMethodInfoTuple.TryGetValue(modelType, out var genericValidatorTTypeAndValidateMethodInfo))
                {
                    ProcessCached(context, genericValidatorTTypeAndValidateMethodInfo, model);
                }
                else
                {
                    Process(context, modelType, modelTypeToGenericValidatorTTypeAndValidateMethodInfoTuple, model);
                }
            }

            base.OnActionExecuting(context);
        }

        private static void ProcessCached(ActionExecutingContext context, (Type genericValidatorTType, MethodInfo validateMethodInfo) tuple, object model)
        {
            var service = context.HttpContext.RequestServices.GetService(tuple.genericValidatorTType);

            var validationResult = (ValidationResult)tuple.validateMethodInfo.Invoke(service, new[] { model });

            HandleValidationResult(context, validationResult, model);
        }

        private static void Process(ActionExecutingContext context, Type modelType, ConcurrentDictionary<Type, (Type, MethodInfo)> modelTypeToGenericValidatorTTypeAndValidateMethodInfoTuple, object model)
        {
            var validatorType = InternalConstants.IValidatorTType.MakeGenericType(modelType);

            var resolvedValidatorFromDependencyInjection = context.HttpContext.RequestServices.GetRequiredService(validatorType);

            var validateMethod = validatorType.GetMethod(InternalConstants.ValidateMethodName, new[] { modelType }) ?? throw new Exception($"The package 'FluentValidation' version '{InternalConstants.IValidatorTType.Assembly.GetName().Version}' is not supported.");

            modelTypeToGenericValidatorTTypeAndValidateMethodInfoTuple.TryAdd(modelType, (validatorType, validateMethod));

            var validationResult = (ValidationResult)validateMethod.Invoke(resolvedValidatorFromDependencyInjection, new[] { model });

            HandleValidationResult(context, validationResult, model);
        }

        private static void HandleValidationResult(ActionExecutingContext context, ValidationResult validationResult, object model)
        {
            if (validationResult.IsValid)
            {
                return;
            }

            context.Result = context.HttpContext.RequestServices.GetService(InternalConstants.IInvalidValidationResponseType) is IInvalidValidationResponse invalidResponse
                ? invalidResponse.GetResult(model, validationResult.Errors)
                : new BadRequestObjectResult(validationResult.Errors);
        }
    }
}