using Microsoft.AspNetCore.Mvc;

namespace OptionConfigurationExamples.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HomeController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public HomeController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Get()
        {
            var greeting = string.Empty;
            if (_configuration.GetValue<bool>("Features:HomeEndpoint:EnableGreeting"))
            {
                greeting = _configuration.GetValue<string>("Features:HomeEndpoint:GreetingContent");
            }

            var response = new
            {
                greeting
            };

            return Ok(response);
        }
    }
}
