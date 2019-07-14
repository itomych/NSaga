using System;
using System.Linq;
using System.Collections.Generic;
using NSaga.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace NSaga.EF
{
    public class SagaSqlDatabase<TDbContext> : ISagaSqlDatabase
    {
        private readonly DbContext _dbContext;
        public SagaSqlDatabase(TDbContext dbContext)
        {
            _dbContext = dbContext as DbContext;
        }

        public List<T> GetById<T>(Guid correlationId)
             where T : class, ICorrelationEntity
        {
            IQueryable<T> query = _dbContext.Set<T>();
            return query.Where(_ => _.CorrelationId.Equals(correlationId)).ToList();
        }

        public int Update<T>(T dataModel)
            where T : class, ICorrelationEntity
        {
            if (dataModel == null)
                return 0;

            IQueryable<T> query = _dbContext.Set<T>();
            var existing = query.FirstOrDefault(_ => _.CorrelationId.Equals(dataModel.CorrelationId));

            if (existing != null)
            {
                _dbContext.Entry(existing).CurrentValues.SetValues(dataModel);
                _dbContext.SaveChanges();
                return 1;
            }
            return 0;
        }

        public void Insert<T>(T dataModel)
            where T : class, ICorrelationEntity
        {
            _dbContext.Set<T>().Add(dataModel);
            _dbContext.SaveChanges();
        }

        public void Delete<T>(T dataModel)
            where T : class, ICorrelationEntity
        {
            _dbContext.Set<T>().Remove(dataModel);
            _dbContext.SaveChanges();
        }

        public void DeleteById<T>(Guid correlationId)
            where T : class, ICorrelationEntity
        {
            IQueryable<T> query = _dbContext.Set<T>();
            var existing = query.FirstOrDefault(_ => _.CorrelationId.Equals(correlationId));

            if (existing != null)
            {
                _dbContext.Set<T>().RemoveRange(existing);
                _dbContext.SaveChanges();
            }
        }

        ISagaSqlTransaction ISagaSqlDatabase.BeginTransaction()
        {
            return new SagaSqlTransaction(_dbContext);
        }
    }
}
