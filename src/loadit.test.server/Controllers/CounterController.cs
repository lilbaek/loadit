using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Loadit.Test.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CounterController : ControllerBase
    {
        public static int CalledCount = 0;
        
        [HttpGet]
        public IActionResult Get()
        {
            CalledCount++;
            return Ok(CalledCount);
        }

        [HttpGet("result")]
        public IActionResult Result()
        {
            return Ok(CalledCount);
        }
        
        [HttpGet("reset")]
        public IActionResult Reset()
        {
            CalledCount = 0;
            return Ok();
        }
    }
}