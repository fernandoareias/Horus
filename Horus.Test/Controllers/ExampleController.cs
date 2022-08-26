using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Horus.Test.Controllers
{
    [ApiController]
    public class ExampleController : ControllerBase
    {

        [HttpGet("index")]
        public async Task<IActionResult> Index()
        {
            return Ok(new
            {
                status = true,
                message = "Successfully Done!"
            });
        }


    }
}
