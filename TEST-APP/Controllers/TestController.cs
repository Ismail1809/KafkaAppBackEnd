using Microsoft.AspNetCore.Mvc;
using TEST_APP.Services;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace TEST_APP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly IMyLoggerWrapper _logger;

        public TestController(IMyLoggerWrapper logger)
        {
            _logger = logger;
        }

        [HttpPost("wrap-log")]
        public IActionResult PostAndWrapLog([FromQuery] string str)
        {
            try
            {
                _logger.WrapLog(str);
                return Ok();
            }
            catch (Exception ex) {
                return BadRequest(ex);
            }
        }
    }
}
