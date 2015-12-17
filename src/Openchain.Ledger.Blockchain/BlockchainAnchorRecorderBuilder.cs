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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NBitcoin;

namespace Openchain.Ledger.Blockchain
{
    public class BlockchainAnchorRecorderBuilder : IComponentBuilder<BlockchainAnchorRecorder>
    {
        private Uri apiUrl;
        private Key key;
        private Network network;
        private long fees;

        public string Name { get; } = "Blockchain";

        public BlockchainAnchorRecorder Build(IServiceProvider serviceProvider)
        {
            if (key != null)
            {
                serviceProvider.GetRequiredService<ILogger>().LogInformation(
                    $"Blockchain anchoring configured to publish at address: {key.PubKey.GetAddress(network).ToString()}");

                return new BlockchainAnchorRecorder(apiUrl, key, network, fees);
            }
            else
            {
                return null;
            }
        }

        public Task Initialize(IServiceProvider serviceProvider, IDictionary<string, string> parameters)
        {
            if (parameters.ContainsKey("key"))
            {
                string anchorKey = parameters["key"];
                if (anchorKey != "")
                {
                    key = Key.Parse(anchorKey);
                    network = Network.GetNetworks()
                        .First(item => item.GetVersionBytes(Base58Type.PUBKEY_ADDRESS)[0] == byte.Parse(parameters["network_byte"]));

                    apiUrl = new Uri(parameters["bitcoin_api_url"]);
                    fees = long.Parse(parameters["fees"]);
                }
            }
            
            return Task.FromResult(0);
        }
    }
}
