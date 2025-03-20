using System;

namespace Ogu.FluentValidation.AspNetCore.Attribute
{
    /// <summary>
    /// An attribute that, when applied to a method, skips model validation for that method.
    /// </summary>
    /// <remarks>
    /// Use this attribute on action methods to bypass any validation logic that would normally be applied.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method)]
    public class SkipValidateAttribute : System.Attribute { }
}