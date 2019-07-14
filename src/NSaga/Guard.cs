using System;
using System.Diagnostics;

namespace NSaga
{
    public static class Guard
    {
        [DebuggerHidden]
        public static void ArgumentIsNotNull(object value, string argument)
        {
            if (value == null)
            {
                throw new ArgumentNullException(argument);
            }
        }


        [DebuggerHidden]
        public static void CheckSagaMessage(ISagaMessage sagaMessage, string argumentName)
        {
            if (sagaMessage == null)
            {
                throw new ArgumentNullException(argumentName);
            }

            if (sagaMessage.CorrelationId == default(Guid))
            {
                throw new ArgumentException("CorrelationId was not provided in the message. Please make sure you assign CorrelationId before issuing it to your Saga");
            }
        }
    }
}
