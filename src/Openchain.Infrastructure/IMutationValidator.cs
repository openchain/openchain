// Copyright 2015 Coinprism, Inc.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Openchain.Infrastructure
{
    /// <summary>
    /// Represents a service capable of verifying a mutation.
    /// </summary>
    public interface IMutationValidator
    {
        /// <summary>
        /// Validates a mutation.
        /// </summary>
        /// <param name="mutation">The mutation to validate.</param>
        /// <param name="authentication">Authentication data provided along with the mutation.</param>
        /// <param name="accounts">Dictionary containing the current version of records being affected.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        Task Validate(ParsedMutation mutation, IReadOnlyList<SignatureEvidence> authentication, IReadOnlyDictionary<AccountKey, AccountStatus> accounts);
    }
}
