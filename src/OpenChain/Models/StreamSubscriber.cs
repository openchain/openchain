using Newtonsoft.Json.Linq;
using OpenChain.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace OpenChain.Models
{
    public class StreamSubscriber
    {
        private readonly Uri endpoint;
        private readonly ILedgerStore store;

        public StreamSubscriber(Uri endpoint, ILedgerStore store)
        {
            this.endpoint = endpoint;
            this.store = store;
        }

        public async Task Subscribe(CancellationToken cancel)
        {
            while (!cancel.IsCancellationRequested)
            {
                try
                {
                    HttpClient client = new HttpClient();
                    string response = await client.GetStringAsync(this.endpoint);

                    JArray records = JArray.Parse(response);

                    foreach (JObject record in records)
                    {
                        BinaryData rawRecord = BinaryData.Parse((string)record["raw"]);
                        await this.store.AddLedgerRecord(rawRecord);
                    }
                }
                catch (Exception exception)
                {

                }
            }
        }
    }
}
