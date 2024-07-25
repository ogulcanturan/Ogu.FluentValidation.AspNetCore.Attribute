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
            if (!(context.ActionDescriptor is ControllerActionDescriptor controllerActionDescriptor))
            {
                return;
            }

            if (controllerActionDescriptor.MethodInfo.GetCustomAttribute<SkipValidateAttribute>() == null)
            {
                foreach (var modelType in ModelTypes)
                {
                    var model = context.ActionArguments.Values.FirstOrDefault(x => modelType.IsInstanceOfType(x));

                    if (model == null)
                    {
                        continue;
                    }

                    var validatorType = typeof(IValidator<>).MakeGenericType(modelType);

                    if (!(context.HttpContext.RequestServices.GetService(validatorType) is IValidator validator))
                    {
                        continue;
                    }

                    var validateMethod = validator.GetType().GetMethods().FirstOrDefault(m =>
                        m.Name == "ValidateAsync" && m.GetParameters()[0].ParameterType == modelType);

                    var validationResult = await (Task<ValidationResult>)validateMethod.Invoke(validator, new[] { model, IsCancellationTokenActive ? context.HttpContext.RequestAborted : CancellationToken.None });

                    if (!validationResult.IsValid)
                    {
                        context.Result = context.HttpContext.RequestServices.GetService(typeof(IInvalidValidationResponse)) is IInvalidValidationResponse invalidFluentValidation
                            ? await invalidFluentValidation.GetResultAsync(model, validationResult.Errors, IsCancellationTokenActive ? context.HttpContext.RequestAborted : CancellationToken.None)
                            : new BadRequestObjectResult(validationResult.Errors);
                    }
                }
            }

            await base.OnActionExecutionAsync(context, next);
        }
    }
}