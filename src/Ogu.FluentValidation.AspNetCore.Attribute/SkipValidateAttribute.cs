using System;

namespace Ogu.FluentValidation.AspNetCore.Attribute
{
    [AttributeUsage(AttributeTargets.Method)]
    public class SkipValidateAttribute : System.Attribute { }
}