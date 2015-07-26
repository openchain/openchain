using System;

namespace OpenChain
{
    public class ConcurrentMutationException : Exception
    {
        public ConcurrentMutationException(Record failedMutation)
            : base($"Version '{failedMutation.Version}' of key '{failedMutation.Key}' no longer exists.")
        {
            this.FailedMutation = failedMutation;
        }

        public Record FailedMutation { get; }
    }
}
