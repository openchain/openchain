using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using NBitcoin;
using Newtonsoft.Json.Linq;

namespace OpenChain.Ledger.Blockchain
{
    public class BlockchainAnchorRecorder : IAnchorRecorder
    {
        private static readonly byte[] anchorMarker = new byte[] { 0x4f, 0x43 };
        private readonly Uri url;
        private readonly Key publishingAddress;
        private readonly Network network;

        public BlockchainAnchorRecorder(Uri url, Key publishingAddress, Network network)
        {
            this.url = url;
            this.publishingAddress = publishingAddress;
            this.network = network;
        }

        public async Task RecordAnchor(LedgerAnchor anchor)
        {
            byte[] anchorPayload =
                anchorMarker
                .Concat(BitConverter.GetBytes(anchor.TransactionCount).Reverse())
                .Concat(anchor.FullStoreHash.ToByteArray())
                .ToArray();

            HttpClient client = new HttpClient();
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
            builder.SendFees(1000);
            builder.SetChange(this.publishingAddress.ScriptPubKey, ChangeType.All);

            ByteString seriazliedTransaction = new ByteString(builder.BuildTransaction(true).ToBytes());

            await SubmitTransaction(seriazliedTransaction);
        }

        private async Task<string> SubmitTransaction(ByteString transaction)
        {
            HttpClient client = new HttpClient();
            StringContent content = new StringContent($"\"{transaction.ToString()}\"");
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            HttpResponseMessage response = await client.PostAsync(new Uri(url, $"sendrawtransaction"), content);
            response.EnsureSuccessStatusCode();

            JToken result = JToken.Parse(await response.Content.ReadAsStringAsync());
            return (string)result;
        }
    }
}
