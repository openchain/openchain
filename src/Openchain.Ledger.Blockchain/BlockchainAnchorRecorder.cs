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
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using NBitcoin;
using Newtonsoft.Json.Linq;

namespace Openchain.Ledger.Blockchain
{
    public class BlockchainAnchorRecorder : IAnchorRecorder
    {
        private static readonly byte[] anchorMarker = new byte[] { 0x4f, 0x43 };
        private readonly Uri url;
        private readonly Key publishingAddress;
        private readonly Network network;
        private readonly long satoshiFees;

        public BlockchainAnchorRecorder(Uri url, Key publishingAddress, Network network, long satoshiFees)
        {
            this.url = url;
            this.publishingAddress = publishingAddress;
            this.network = network;
            this.satoshiFees = satoshiFees;
        }

        public async Task<bool> CanRecordAnchor()
        {
            using (HttpClient client = new HttpClient())
            {
                BitcoinAddress address = this.publishingAddress.ScriptPubKey.GetDestinationAddress(this.network);
                HttpResponseMessage response = await client.GetAsync(new Uri(url, $"addresses/{address.ToString()}/transactions"));

                string body = await response.Content.ReadAsStringAsync();

                JArray outputs = JArray.Parse(body);

                // If a transaction is unconfirmed, we don't
                foreach (JObject transaction in outputs.Children())
                {
                    if ((string)transaction["inputs"].First()["addresses"].First() != address.ToString())
                        continue;

                    if ((string)transaction["block_hash"] == null)
                        return false;
                }

                return true;
            }
        }

        public async Task RecordAnchor(LedgerAnchor anchor)
        {
            byte[] anchorPayload =
                anchorMarker
                .Concat(BitConverter.GetBytes((ulong)anchor.TransactionCount).Reverse())
                .Concat(anchor.FullStoreHash.ToByteArray())
                .ToArray();

            using (HttpClient client = new HttpClient())
            {
                BitcoinAddress address = this.publishingAddress.ScriptPubKey.GetDestinationAddress(this.network);
                HttpResponseMessage response = await client.GetAsync(new Uri(url, $"addresses/{address.ToString()}/unspents"));

                string body = await response.Content.ReadAsStringAsync();

                JArray outputs = JArray.Parse(body);

                TransactionBuilder builder = new TransactionBuilder();
                builder.AddKeys(publishingAddress.GetBitcoinSecret(network));
                foreach (JObject output in outputs)
                {
                    string transactionHash = (string)output["transaction_hash"];
                    uint outputIndex = (uint)output["output_index"];
                    long amount = (long)output["value"];

                    builder.AddCoins(new Coin(uint256.Parse(transactionHash), outputIndex, new Money(amount), publishingAddress.ScriptPubKey));
                }

                Script opReturn = new Script(OpcodeType.OP_RETURN, Op.GetPushOp(anchorPayload));
                builder.Send(opReturn, 0);
                builder.SendFees(satoshiFees);
                builder.SetChange(this.publishingAddress.ScriptPubKey, ChangeType.All);

                ByteString seriazliedTransaction = new ByteString(builder.BuildTransaction(true).ToBytes());

                await SubmitTransaction(seriazliedTransaction);
            }
        }

        private async Task<ByteString> SubmitTransaction(ByteString transaction)
        {
            using (HttpClient client = new HttpClient())
            {
                StringContent content = new StringContent($"\"{transaction.ToString()}\"");
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                HttpResponseMessage response = await client.PostAsync(new Uri(url, "sendrawtransaction"), content);
                response.EnsureSuccessStatusCode();

                JToken result = JToken.Parse(await response.Content.ReadAsStringAsync());
                return ByteString.Parse((string)result);
            }
        }
    }
}
