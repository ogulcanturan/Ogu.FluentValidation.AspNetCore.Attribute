using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Linq;
using System.Reflection;

namespace Ogu.FluentValidation.AspNetCore.Attribute
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class ValidateAttribute : ActionFilterAttribute
    {
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
                        m.Name == "Validate" && m.GetParameters()[0].ParameterType == modelType);

                    var validationResult = (ValidationResult)validateMethod.Invoke(validator, new[] { model });

                    if (validationResult.IsValid)
                    {
                        continue;
                    }

                    var invalidFluentValidation = context.HttpContext.RequestServices.GetService(typeof(IInvalidValidationResponse)) as IInvalidValidationResponse;

                    context.Result = invalidFluentValidation?.GetResult(model, validationResult.Errors) ??
                                     new BadRequestObjectResult(validationResult.Errors);
                }
            }

            base.OnActionExecuting(context);
        }
    }
}