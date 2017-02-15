using NBitcoin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Openchain.SDK
{
    public class OpenchainSDK
    {
        public ApiClient Client { get; private set; }

        public Network Network { get; private set; }

        public OpenchainSDK(string serverUrl)
        {
            Client = new ApiClient(serverUrl);

            Client.Initialize().Wait();

            Network = OpenchainNetwork.Network;
        }

        public TransactionBuilder CreateTransactionBuilder()
        {
            return new TransactionBuilder(Client);
        }
    }
}
