using System;
using System.Diagnostics;

namespace Utilities.Logger
{
    internal class Logging : ILogger
    {
        private static Logging instance;
        public static Logging Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Logging();
                }
                return instance;
            }
        }

        public void Information(Type type, string format, params object[] args)
        {
            
            Trace.TraceInformation(format, args);
          
        }

        public void Warning(Type type, string format, params object[] args)
        {
            Trace.TraceWarning(format, args);
        }

        public void Error(Type type, string format, params object[] args)
        {
            Trace.TraceError(format, args);
        }

        public void Error(Type type, Exception exception)
        {
            Error(type, "{0}", exception);
        }
    }
}
