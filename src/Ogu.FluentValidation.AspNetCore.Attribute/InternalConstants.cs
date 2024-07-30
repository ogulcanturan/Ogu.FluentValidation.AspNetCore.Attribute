using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Ogu.FluentValidation.AspNetCore.Attribute
{
    public static class InternalConstants
    {
        public const string ValidateAsyncMethodName = "ValidateAsync";
        public const string ValidateMethodName = "Validate";

        internal static readonly Type CancellationTokenType = typeof(CancellationToken);

        internal static readonly Lazy<ConcurrentDictionary<string, bool>> ActionUuidToHasSkipValidateAttribute = new Lazy<ConcurrentDictionary<string, bool>>();
    }
}