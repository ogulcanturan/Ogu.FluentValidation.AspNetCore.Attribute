using Microsoft.AspNetCore.Mvc;

namespace Ogu.FluentValidation.AspNetCore.Attribute.Tests.TestData
{
    public class TestController : ControllerBase
    {
        [HttpGet]
        public IActionResult TestEndpoint(TestModel testModel)
        {
            return Ok();
        }

        [HttpGet]
        [SkipValidate]
        public IActionResult SkipValidateTestEndpoint(TestModel testModel)
        {
            return Ok();
        }
    }
}