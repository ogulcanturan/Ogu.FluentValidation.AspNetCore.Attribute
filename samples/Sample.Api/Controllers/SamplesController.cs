using Microsoft.AspNetCore.Mvc;
using Ogu.FluentValidation.AspNetCore.Attribute;
using Sample.Api.RequestValidators.Models;

namespace Sample.Api.Controllers
{
    [Route("api/[controller]")]
    public class SamplesController : ControllerBase
    {
        [HttpPost]
        [Validate(typeof(GetSampleRequestValidatorModel))]
        public IActionResult GetSample(GetSampleRequestValidatorModel model)
        {
            return Ok();
        }
    }
}