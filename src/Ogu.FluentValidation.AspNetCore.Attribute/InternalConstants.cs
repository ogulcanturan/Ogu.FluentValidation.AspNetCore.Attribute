using FluentValidation;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Ogu.FluentValidation.AspNetCore.Attribute
{
    internal static class InternalConstants
    {
        internal const string ValidateAsyncMethodName = "ValidateAsync";

        internal const string ValidateMethodName = "Validate";

        internal static readonly Type CancellationTokenType = typeof(CancellationToken);

        internal static readonly Type IValidatorTType = typeof(IValidator<>);

        internal static readonly Type IInvalidValidationResponseType = typeof(IInvalidValidationResponse);

        internal static readonly Lazy<ConcurrentDictionary<string, bool>> ActionUuidToHasSkipValidateAttribute = new Lazy<ConcurrentDictionary<string, bool>>();
    }
}