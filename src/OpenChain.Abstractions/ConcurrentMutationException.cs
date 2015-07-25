using System;

namespace OpenChain
{
    public class ConcurrentMutationException : Exception
    {
        public ConcurrentMutationException(Record failedMutation)
            : base(string.Format(
                "Version '{0}' of key '{1}' no longer exists.",
                failedMutation.Version.ToString(),
                failedMutation.Key.ToString()))
        {
            this.FailedMutation = failedMutation;
        }

        public Record FailedMutation { get; }
    }
}
