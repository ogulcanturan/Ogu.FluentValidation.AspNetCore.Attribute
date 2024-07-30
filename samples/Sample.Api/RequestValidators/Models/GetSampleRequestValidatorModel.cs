using Microsoft.AspNetCore.Mvc;
using Sample.Api.Requests;

namespace Sample.Api.RequestValidators.Models
{
    public class GetSampleRequestValidatorModel
    {
        [FromBody]
        public GetSampleRequest Body { get; set; }
    }
}