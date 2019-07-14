using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NSaga.SqlServer;

namespace NSaga.EF
{
    public class SagaSqlTransaction: ISagaSqlTransaction
    {
        private IDbContextTransaction _dbContextTransaction;

        public SagaSqlTransaction(DbContext dbContext)
        {
            _dbContextTransaction = dbContext.Database.BeginTransaction();
        }

        public void CommitTransaction()
        {
            _dbContextTransaction.Commit();
        }

        public void Dispose()
        {
            _dbContextTransaction.Dispose();
        }

        public void RollbackTransaction()
        {
            _dbContextTransaction.Rollback();
        }
    }
}
