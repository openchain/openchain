using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Openchain.Infrastructure;
using Openchain.SDK.Models;

namespace Openchain.SDK
{
    public class TransactionBuilder
    {
        private readonly ApiClient _apiClient;
        private List<Record> _records;
        private List<MutationSigner> _keys;
        private ByteString _metaData;

        public TransactionBuilder(ApiClient apiClient)
        {
            if (apiClient.Namespace == null)
            {
                throw new Exception("The API client has not been initialized");
            }

            _apiClient = apiClient;
            _records = new List<Record>();
            _keys = new List<MutationSigner>();
            _metaData = ByteString.Empty;
        }

        public TransactionBuilder AddRecord(ByteString key, object value, ByteString version)
        {

            /*if (value != null)
            {
                var valueData = new { data = value };

                value = new ByteString(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(valueData)));
            }*/

            ByteString recordValue;

            if(value is ByteString)
            {
                recordValue = (ByteString)value;
            }else
            {
                recordValue = new ByteString(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value)));
            }

            var newRecord = new Record(key, recordValue, version);

            _records.Add(newRecord);

            return this;
        }

        public TransactionBuilder SetMetaData(object data)
        {
            _metaData = new ByteString(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data)));

            return this;
        }

        public TransactionBuilder AddAccountRecord(AccountStatus previous, Int64 delta)
        {
            var bytes = BitConverter.GetBytes(previous.Balance + delta);

            if(BitConverter.IsLittleEndian)
            {
                bytes = bytes.Reverse().ToArray();
            }

            return AddRecord(previous.AccountKey.Key.ToBinary(), new ByteString(bytes), previous.Version);
        }

        public async Task<TransactionBuilder> UpdateAccountRecord(string account, string asset, Int64 delta)
        {
            if (account.StartsWith("@"))
            {
                account = $"/aka/{account.Slice(1, account.Length)}/";
            }

            var dataRecord = await _apiClient.GetDataRecord(account, "goto");

            var accountResult = string.Empty;

            if (dataRecord.Data == null)
            {
                accountResult = account;
            }
            else
            {
                AddRecord(dataRecord.Key, null, dataRecord.Version);
                accountResult = dataRecord.Data;
            }

            var accountRecord = await _apiClient.GetAccountRecord(accountResult, asset);

            return AddAccountRecord(accountRecord, delta);
        }

        public TransactionBuilder AddSigningKey(MutationSigner key)
        {
            _keys.Add(key);

            return this;
        }

        public ByteString Build()
        {
            var mutation = new Mutation(_apiClient.Namespace, _records, _metaData);

            return new ByteString(MessageSerializer.SerializeMutation(mutation));
        }

        public async Task<TransactionData> Submit()
        {
            var mutation = Build();

            var signatures = new List<SigningKey>();

            _keys.ForEach(key =>
            {
                signatures.Add(new SigningKey
                {
                    PublicKey = key.PublicKey.ToString(),
                    Signature = key.Sign(mutation).ToString()
                });
            });

            return await _apiClient.Submit(mutation, signatures);
        }
    }
}
