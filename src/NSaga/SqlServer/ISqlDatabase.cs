using System;
using System.Collections.Generic;

namespace NSaga.SqlServer
{
    public interface ISagaSqlTransaction: IDisposable
    {
        void CommitTransaction();
        void RollbackTransaction();
    }

    public interface ISagaSqlDatabase
    {
        ISagaSqlTransaction BeginTransaction();
        List<T> GetById<T>(Guid correlationId)
            where T : class, ICorrelationEntity;
        int Update<T>(T dataModel)
            where T : class, ICorrelationEntity;
        void Insert<T>(T dataModel)
            where T : class, ICorrelationEntity;
        void Delete<T>(T dataModel)
            where T : class, ICorrelationEntity;
        void DeleteById<T>(Guid correlationId)
            where T : class, ICorrelationEntity;
    }
}