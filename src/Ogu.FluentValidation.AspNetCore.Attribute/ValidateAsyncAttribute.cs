using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Ogu.FluentValidation.AspNetCore.Attribute
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class ValidateAsyncAttribute : ActionFilterAttribute
    {
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
            var controllerActionDescriptor = context.ActionDescriptor as ControllerActionDescriptor;

            if (controllerActionDescriptor == null)
                return;

            if (controllerActionDescriptor.MethodInfo.GetCustomAttribute<SkipValidateAttribute>() == null)
            {
                foreach (var modelType in ModelTypes)
                {
                    var model = context.ActionArguments.Values.FirstOrDefault(x => modelType.IsInstanceOfType(x));

                    if (model != null)
                    {
                        var validatorType = typeof(IValidator<>).MakeGenericType(modelType);
                        var validator = context.HttpContext.RequestServices.GetService(validatorType) as IValidator;

                        if (validator != null)
                        {
                            var validateMethod = validator.GetType().GetMethods().FirstOrDefault(m =>
                                m.Name == "ValidateAsync" && m.GetParameters()[0].ParameterType == modelType);

                            var validationResult = await (Task<ValidationResult>)validateMethod.Invoke(validator, new[] { model, IsCancellationTokenActive ? context.HttpContext.RequestAborted : CancellationToken.None });

                            if (!validationResult.IsValid)
                            {
                                var invalidFluentValidation = context.HttpContext.RequestServices.GetService(typeof(IInvalidValidationResponse)) as IInvalidValidationResponse;

                                context.Result = invalidFluentValidation != null
                                    ? await invalidFluentValidation.GetResultAsync(model, validationResult.Errors, IsCancellationTokenActive ? context.HttpContext.RequestAborted : CancellationToken.None)
                                    : new BadRequestObjectResult(validationResult.Errors);
                            }
                        }
                    }
                }
            }

            await base.OnActionExecutionAsync(context, next);
        }
    }
}