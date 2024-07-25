using Microsoft.AspNetCore.Mvc;

namespace Ogu.FluentValidation.AspNetCore.Attribute.Tests.TestData
{
    public class TestController : ControllerBase
    {
        [HttpGet]
        [ValidateAsync(typeof(TestModel))]
        public IActionResult TestEndpoint(TestModel testModel)
        {
            return Ok();
        }

        [HttpGet]
        [ValidateAsync(typeof(TestModel))]
        [SkipValidate]
        public IActionResult SkipValidateTestEndpoint(TestModel testModel)
        {
            return Ok();
        }
    }
}