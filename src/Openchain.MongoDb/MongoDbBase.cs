using System;
using MongoDB.Driver;

namespace Openchain.MongoDb
{
    public class MongoDbBase : IDisposable
    {
        private string connectionString;
        private string database;
        internal IMongoClient Client
        {
            get;
            set;
        }

        internal IMongoDatabase Database
        {
            get;
            set;
        }

        public MongoDbBase(string connectionString, string database)
        {
            this.connectionString = connectionString;
            this.database = database;
            Client = new MongoClient(connectionString);
            Database = Client.GetDatabase(database);
        }
      
#region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.
                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~SqliteBase() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }
        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        // TODO: uncomment the following line if the finalizer is overridden above.
        // GC.SuppressFinalize(this);
        }
#endregion
    }
}