using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Openchain.EntityFrameworkCore
{
    public class EntityFrameworkCoreStorageEngine<TContext> : IStorageEngine where TContext : DbContext
    {
        public TContext Context { get; private set; }

        private readonly ILogger _logger;

        public DbSet<Models.Transaction> Transactions { get { return Context.Set<Models.Transaction>(); } }
        public DbSet<Models.Record> Records { get { return Context.Set<Models.Record>(); } }
        public DbSet<Models.RecordMutation> RecordMutations { get { return Context.Set<Models.RecordMutation>(); } }

        public EntityFrameworkCoreStorageEngine(TContext context, ILoggerFactory loggerFactory)
        {
            Context = context;
            _logger = loggerFactory.CreateLogger<EntityFrameworkCoreStorageEngine<TContext>>();
        }

        public Task Initialize()
        {
            return Task.FromResult(0);
        }

        #region AddTransactions
        public async Task AddTransactions(IEnumerable<ByteString> transactions)
        {
            using (var dbTransaction = Context.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    foreach (ByteString rawTransaction in transactions)
                    {
                        byte[] rawTransactionBuffer = rawTransaction.ToByteArray();
                        Transaction transaction = MessageSerializer.DeserializeTransaction(rawTransaction);
                        byte[] transactionHash = MessageSerializer.ComputeHash(rawTransactionBuffer);

                        byte[] mutationHash = MessageSerializer.ComputeHash(transaction.Mutation.ToByteArray());
                        Mutation mutation = MessageSerializer.DeserializeMutation(transaction.Mutation);

                        await UpdateAccounts(mutation, mutationHash);

                        var newTransaction = new Models.Transaction
                        {
                            TransactionHash = transactionHash,
                            MutationHash = mutationHash,
                            RawData = rawTransactionBuffer
                        };

                        Transactions.Add(newTransaction);

                        await Context.SaveChangesAsync();

                        await AddTransaction(newTransaction.Id, mutationHash, mutation);
                    }

                    dbTransaction.Commit();
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex.Message);

                    dbTransaction.Rollback();

                    throw new Exception(ex.Message);
                }
            }
        }

        protected virtual Task AddTransaction(long transactionId, byte[] transactionHash, Mutation mutation)
        {
            return Task.FromResult(0);
        }

        private async Task UpdateAccounts(Mutation mutation, byte[] transactionHash)
        {
            foreach (Record record in mutation.Records)
            {
                if (record.Value == null)
                {
                    // Read the record and make sure it corresponds to the one supplied
                    var records = Records.Where(r => r.Key == record.Key.ToByteArray()).ToList();

                    var versions = records.Select(r =>
                    {
                        return r.Version;
                    }).ToList();

                    if (versions.Count == 0)
                    {
                        if (!record.Version.Equals(ByteString.Empty))
                            throw new ConcurrentMutationException(record);
                    }
                    else
                    {
                        if (!new ByteString(versions[0]).Equals(record.Version))
                            throw new ConcurrentMutationException(record);
                    }
                }
                else
                {
                    if (!record.Version.Equals(ByteString.Empty))
                    {
                        try
                        {
                            var currentRecord = Records.Where(r => r.Key == record.Key.ToByteArray() && r.Version == record.Version.ToByteArray()).FirstOrDefault();

                            //TODO: null check on current record

                            // Update existing account
                            currentRecord.Value = record.Value.ToByteArray();
                            currentRecord.Version = transactionHash;

                            Context.Update(currentRecord);

                            await Context.SaveChangesAsync();
                        }
                        //catch unique key validation exception here
                        catch (Exception)
                        {
                            throw new ConcurrentMutationException(record);
                        }
                    }
                    else
                    {
                        // Create a new record
                        try
                        {
                            var newRecord = new Models.Record
                            {
                                Key = record.Key.ToByteArray(),
                                Value = record.Value.ToByteArray(),
                                Version = transactionHash
                            };

                            Records.Add(newRecord);

                            await Context.SaveChangesAsync();
                        }
                        //catch unique key validation exception here
                        catch (Exception)
                        {
                            throw new ConcurrentMutationException(record);
                        }
                    }
                }
            }
        }

        #endregion

        #region GetRecords
        public async Task<IReadOnlyList<Record>> GetRecords(IEnumerable<ByteString> keys)
        {
            Dictionary<ByteString, Record> result = new Dictionary<ByteString, Record>();

            foreach (ByteString key in keys)
            {
                var dbRecord = await Records.Where(r => r.Key == key.ToByteArray()).FirstOrDefaultAsync();
                if(dbRecord != null)
                {
                    result[key] = new Record(
                            key,
                            dbRecord.Value == null ? ByteString.Empty : new ByteString(dbRecord.Value),
                            new ByteString(dbRecord.Version));
                }else
                {
                    result[key] = new Record(key, ByteString.Empty, ByteString.Empty);
                }
            }

            return result.Values.ToList().AsReadOnly();
        }
        #endregion

        #region GetLastTransaction

        public async Task<ByteString> GetLastTransaction()
        {
            var transaction = await Transactions.OrderByDescending(t => t.Id).Take(1).FirstOrDefaultAsync();

            return transaction != null ? new ByteString(transaction.TransactionHash) : ByteString.Empty;
        }
        #endregion

        #region GetTransactions
        public async Task<IReadOnlyList<ByteString>> GetTransactions(ByteString from)
        {
            if (from != null)
            {
                var checkpoint = await Transactions.Where(e => e.TransactionHash == from.ToByteArray()).FirstOrDefaultAsync();
                return Transactions.Where(e => e.Id > checkpoint.Id).OrderBy(e => e.Id).ToList().Select(t =>
                {
                    return new ByteString(t.RawData);
                }).ToList().AsReadOnly();
            }else
            {
                return Transactions.OrderBy(e => e.Id).ToList().Select(t =>
                {
                    return new ByteString(t.RawData);
                }).ToList().AsReadOnly();
            }
        }

        public void Dispose()
        {
            if(Context != null)
            {
                Context.Dispose();
            }
        }

        #endregion
    }
}
