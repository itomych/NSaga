using System;

namespace NSaga.SqlServer
{
    public interface ICorrelationEntity
    {
        Guid CorrelationId { get; set; }
    }
}
