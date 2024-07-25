using FluentValidation;

namespace Ogu.FluentValidation.AspNetCore.Attribute.Tests.TestData
{
    public class TestModelValidator : AbstractValidator<TestModel>
    {
        public TestModelValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name required");
        }
    }
}