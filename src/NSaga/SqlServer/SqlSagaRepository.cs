using NSaga.SqlServer;
using NSaga.SqlServer.Model;
using System;
using System.Linq;

namespace NSaga
{
    /// <summary>
    /// Implementation of <see cref="ISagaRepository"/> that uses SQL Server to store Saga data.
    /// <para>
    /// Before using you need to execute provided Install.Sql to create tables.
    /// </para>
    /// <para>
    /// This implementation uses PetaPoco micro ORM internally. PetaPoco can work with multiple databases, not just SQL Server.
    /// Though this implementation was tested with SQL Server, I'm pretty sure you will be able to use MySql, Postgress, etc.
    /// To do that you'll have to provide your own <see cref="IConnectionFactory"/> that returns a connection to a required database.
    /// </para>
    /// </summary>
    public sealed class SqlSagaRepository : ISagaRepository
    {
        private readonly ISagaFactory sagaFactory;
        private readonly IMessageSerialiser messageSerialiser;
        private readonly ISagaSqlDatabase database;

        /// <summary>
        /// Initiates an instance of <see cref="SqlSagaRepository"/> with a connection string name.
        /// Actual connection string is taken from your app.config or web.config
        /// </summary>
        /// <param name="connectionFactory">An insantance implementing <see cref="IConnectionFactory"/></param>
        /// <param name="sagaFactory">An instance implementing <see cref="ISagaFactory"/></param>
        /// <param name="messageSerialiser">An instance implementing <see cref="IMessageSerialiser"/></param>
        public SqlSagaRepository(ISagaSqlDatabase database, ISagaFactory sagaFactory, IMessageSerialiser messageSerialiser)
        {
            Guard.ArgumentIsNotNull(database, nameof(database));
            Guard.ArgumentIsNotNull(sagaFactory, nameof(sagaFactory));
            Guard.ArgumentIsNotNull(messageSerialiser, nameof(messageSerialiser));

            this.messageSerialiser = messageSerialiser;
            this.sagaFactory = sagaFactory;
            this.database = database;
        }


        /// <summary>
        /// Finds and returns saga instance with the given correlation ID.
        /// You will get exceptions if TSaga does not match the actual saga data with the provided exception.
        /// 
        /// Actually creates an instance of saga from service locator, retrieves SagaData and Headers from the storage and populates the instance with these.
        /// </summary>
        /// <typeparam name="TSaga">Type of saga we are looking for</typeparam>
        /// <param name="correlationId">CorrelationId to identify the saga</param>
        /// <returns>An instance of the saga. Or Null if there is no saga with this ID.</returns>
        public TSaga Find<TSaga>(Guid correlationId) where TSaga : class, IAccessibleSaga
        {
            Guard.ArgumentIsNotNull(correlationId, nameof(correlationId));

            var persistedData = database.GetById<SagaData>(correlationId).FirstOrDefault();

            if (persistedData == null)
            {
                return null;
            }

            var sagaInstance = sagaFactory.ResolveSaga<TSaga>();
            var sagaDataType = NSagaReflection.GetInterfaceGenericType<TSaga>(typeof(ISaga<>));
            var sagaData = messageSerialiser.Deserialise(persistedData.BlobData, sagaDataType);

            var headersPersisted = database.GetById<SagaHeaders>(correlationId);
            var headers = headersPersisted.ToDictionary(k => k.Key, v => v.Value);

            sagaInstance.CorrelationId = correlationId;
            sagaInstance.Headers = headers;
            NSagaReflection.Set(sagaInstance, "SagaData", sagaData);

            return sagaInstance;
        }


        /// <summary>
        /// Persists the instance of saga into the database storage.
        /// 
        /// Actually stores SagaData and Headers. All other variables in saga are not persisted
        /// </summary>
        /// <typeparam name="TSaga">Type of saga</typeparam>
        /// <param name="saga">Saga instance</param>
        public void Save<TSaga>(TSaga saga) where TSaga : class, IAccessibleSaga
        {
            Guard.ArgumentIsNotNull(saga, nameof(saga));

            var sagaData = NSagaReflection.Get(saga, "SagaData");
            var sagaHeaders = saga.Headers;
            var correlationId = saga.CorrelationId;

            var serialisedData = messageSerialiser.Serialise(sagaData);

            var dataModel = new SagaData()
            {
                CorrelationId = correlationId,
                BlobData = serialisedData,
            };

            using (var transaction = database.BeginTransaction())
            {
                try
                {
                    int updatedRaws = database.Update(dataModel);

                    if (updatedRaws == 0)
                    {
                        // no records were updated - this means no records already exist - need to insert new record
                        database.Insert(dataModel);
                    }

                    // delete all existing headers
                    database.DeleteById<SagaHeaders>(correlationId);

                    // and insert updated ones
                    foreach (var header in sagaHeaders)
                    {
                        var storedHeader = new SagaHeaders()
                        {
                            CorrelationId = correlationId,
                            Key = header.Key,
                            Value = header.Value,
                        };

                        database.Insert(storedHeader);
                    }
                    transaction.CommitTransaction();
                }
                catch (Exception ex)
                {
                    transaction.RollbackTransaction();
                    throw;
                }
            }
        }


        /// <summary>
        /// Deletes the saga instance from the storage
        /// </summary>
        /// <typeparam name="TSaga">Type of saga</typeparam>
        /// <param name="saga">Saga to be deleted</param>
        public void Complete<TSaga>(TSaga saga) where TSaga : class, IAccessibleSaga
        {
            Guard.ArgumentIsNotNull(saga, nameof(saga));

            var correlationId = (Guid)NSagaReflection.Get(saga, "CorrelationId");
            Complete(correlationId);
        }

        /// <summary>
        /// Deletes the saga instance from the storage
        /// </summary>
        /// <param name="correlationId">Correlation Id for the saga</param>
        public void Complete(Guid correlationId)
        {
            Guard.ArgumentIsNotNull(correlationId, nameof(correlationId));

            using (var transaction = database.BeginTransaction())
            {
                try
                {
                    database.DeleteById<SagaHeaders>(correlationId);
                    database.DeleteById<SagaData>(correlationId);
                    transaction.CommitTransaction();
                }
                catch (Exception)
                {
                    transaction.RollbackTransaction();
                    throw;
                }
            }
        }
    }
}
