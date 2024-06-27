namespace TEST_APP.Services
{
    public class MyLogger: IMyLogger
    {

        private readonly ILogger<MyLogger> _logger;

        public MyLogger(ILogger<MyLogger> logger)
        {
            _logger = logger;
        }

        public void Log(string message) 
        { 
            _logger.LogInformation(message);
        }
    }
}
