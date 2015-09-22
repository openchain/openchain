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

using System.Threading.Tasks;

namespace OpenChain.Ledger
{
    /// <summary>
    /// Provides functionality for recording a database anchor. 
    /// </summary>
    public interface IAnchorRecorder
    {
        /// <summary>
        /// Indicates whether this instance is ready to record a new database anchor.
        /// </summary>
        /// <returns>The <see cref="Task{bool}"/> object representing the asynchronous operation.</returns>
        Task<bool> CanRecordAnchor();

        /// <summary>
        /// Records a database anchor.
        /// </summary>
        /// <param name="anchor">The anchor to be recorded.</param>
        /// <returns>The <see cref="Task"/> object representing the asynchronous operation.</returns>
        Task RecordAnchor(LedgerAnchor anchor);
    }
}
