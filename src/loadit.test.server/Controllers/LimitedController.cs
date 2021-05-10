using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Loadit.Test.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LimitedController : ControllerBase
    {
        private readonly Random _random = new();
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            await Task.Delay(_random.Next(100, 700));
            return Ok();
        }
    }
}