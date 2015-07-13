using System;

namespace OpenChain.Core
{
    public class ConcurrentMutationException : Exception
    {
        public ConcurrentMutationException(KeyValuePair failedMutation)
            : base(string.Format(
                "Version '{0}' of key '{1}' no longer exists.",
                failedMutation.Version.ToString(),
                failedMutation.Key.ToString()))
        {
            this.FailedMutation = failedMutation;
        }

        public KeyValuePair FailedMutation { get; }
    }
}
