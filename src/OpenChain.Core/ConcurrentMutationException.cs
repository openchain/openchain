using System;

namespace OpenChain.Core
{
    public class ConcurrentMutationException : Exception
    {
        public ConcurrentMutationException(Mutation failedMutation)
            : base(string.Format(
                "Version '{0}' of key '{1}' no longer exists.",
                failedMutation.Version.ToString(),
                failedMutation.Key.ToString()))
        {
            this.FailedMutation = failedMutation;
        }

        public Mutation FailedMutation { get; }
    }
}
