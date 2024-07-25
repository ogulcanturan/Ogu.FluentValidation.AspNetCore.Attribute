using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Ogu.FluentValidation.AspNetCore.Attribute.Tests.TestData;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Ogu.FluentValidation.AspNetCore.Attribute.Tests
{
    public class SkipValidateAttributeTests
    {
        private readonly ValidateAttribute _attribute;
        private readonly Mock<IValidator<TestModel>> _validatorMock;
        private readonly ActionExecutingContext _context;

        public SkipValidateAttributeTests()
        {
            _validatorMock = new Mock<IValidator<TestModel>>();
            _validatorMock.Setup(v => v.Validate(It.IsAny<TestModel>()))
                .Returns(new ValidationResult(new List<ValidationFailure> { new ValidationFailure("Name", "Name required") }));

            var invalidValidationResponseMock = new Mock<IInvalidValidationResponse>();
            invalidValidationResponseMock.Setup(m => m.GetResultAsync(It.IsAny<object>(), Moq.It.IsAny<List<ValidationFailure>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BadRequestObjectResult("Error"));

            var services = new ServiceCollection()
                .AddSingleton(_validatorMock.Object)
                .AddSingleton(invalidValidationResponseMock.Object);

            var serviceProvider = services.BuildServiceProvider();

            var httpContext = new DefaultHttpContext
            {
                RequestServices = serviceProvider
            };

            var actionContext = new ActionContext
            {
                HttpContext = httpContext,
                RouteData = new Microsoft.AspNetCore.Routing.RouteData(),
                ActionDescriptor = new ControllerActionDescriptor
                {
                    MethodInfo = typeof(TestController).GetMethod(nameof(TestController.SkipValidateTestEndpoint)),
                    ControllerTypeInfo = typeof(TestController).GetTypeInfo()
                }
            };

            var actionArguments = new Dictionary<string, object>
            {
                { "testModel", new TestModel { Name = string.Empty } }
            };

            var controller = new TestController();

            _context = new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                actionArguments,
                controller
            );

            _attribute = new ValidateAttribute(typeof(TestModel));
        }

        [Fact]
        public async Task OnActionExecutionAsync_SkipsValidation_WhenSkipValidateAttributeIsPresent()
        {
            // Act
            await _attribute.OnActionExecutionAsync(_context, () => Task.FromResult(new ActionExecutedContext(_context, new List<IFilterMetadata>(), _context.Controller)));

            // Assert
            _validatorMock.Verify(v => v.ValidateAsync(It.IsAny<TestModel>(), It.IsAny<CancellationToken>()), Times.Never);
            Assert.Null(_context.Result);
        }
    }
}