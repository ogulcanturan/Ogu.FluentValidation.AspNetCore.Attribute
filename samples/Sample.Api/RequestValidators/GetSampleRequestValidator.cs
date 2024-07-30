using FluentValidation;
using Sample.Api.RequestValidators.Models;

namespace Sample.Api.RequestValidators
{
    public class GetSampleRequestValidator : AbstractValidator<GetSampleRequestValidatorModel>
    {
        public GetSampleRequestValidator()
        {
            RuleFor(u => u.Body).NotNull().DependentRules(() =>
            {
                RuleFor(u => u.Body.Name).NotEmpty();
            });
        }
    }
}