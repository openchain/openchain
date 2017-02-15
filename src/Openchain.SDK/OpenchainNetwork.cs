using NBitcoin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Openchain.SDK
{
    public class OpenchainNetwork
    {
        private static Network _network;

        public static Network Network
        {
            get
            {
                if(_network == null)
                {
                    var networkBuilder = new NetworkBuilder();
                    networkBuilder.CopyFrom(Network.Main);

                    networkBuilder.SetName("openchain");
                    networkBuilder.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new[] { (byte)76 });
                    networkBuilder.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new[] { (byte)78 });
                    networkBuilder.SetMagic(0);
                    _network = networkBuilder.BuildAndRegister();
                }

                return _network;
            }
        }
    }
}
