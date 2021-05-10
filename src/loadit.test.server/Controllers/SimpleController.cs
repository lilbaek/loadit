using Microsoft.AspNetCore.Mvc;

namespace Loadit.Test.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SimpleController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok();
        }
        
        [HttpPost]
        public IActionResult Post()
        {
            return Ok();
        }
    }
}