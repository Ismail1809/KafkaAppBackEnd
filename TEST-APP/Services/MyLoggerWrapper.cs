namespace TEST_APP.Services
{
    public class MyLoggerWrapper: IMyLoggerWrapper
    {
        private readonly IMyLogger _myLogger;

        public MyLoggerWrapper(IMyLogger myLogger)
        {
            _myLogger = myLogger;
        }

        public void WrapLog(string message)
        {
            _myLogger.Log("* " + message + " *");
        }
    }
}
