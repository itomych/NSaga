using System;

namespace NSaga.SqlServer.Model
{
    public class SagaData: ICorrelationEntity
    {
        public Guid CorrelationId { get; set; }
        public string BlobData { get; set; }
    }

    public class SagaHeaders: ICorrelationEntity
    {
        public Guid CorrelationId { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
    }
}
