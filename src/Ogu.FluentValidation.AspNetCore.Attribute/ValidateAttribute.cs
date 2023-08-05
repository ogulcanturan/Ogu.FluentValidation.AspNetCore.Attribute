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
                                m.Name == "Validate" && m.GetParameters()[0].ParameterType == modelType);

                            var validationResult = (ValidationResult)validateMethod.Invoke(validator, new[] { model });

                            if (!validationResult.IsValid)
                            {
                                var invalidFluentValidation = context.HttpContext.RequestServices.GetService(typeof(IInvalidValidationResponse)) as IInvalidValidationResponse;

                                context.Result = invalidFluentValidation?.GetResult(model, validationResult.Errors) ??
                                                 new BadRequestObjectResult(validationResult.Errors);
                            }
                        }
                    }
                }
            }

            base.OnActionExecuting(context);
        }
    }
}