using System;
using Microsoft.AspNetCore.Mvc;

namespace LykkePublicAPI.Controllers
{
    [Route("api/[controller]")]
    public class IsAliveController : Controller
    {
        [HttpGet]
        public IsAliveResponse Get()
        {
            return new IsAliveResponse
            {
                Version =
                    Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default.Application.ApplicationVersion,
                Environment = Environment.GetEnvironmentVariable("ENVIRONMENT_INFO")
            };
        }

        public class IsAliveResponse
        {
            public string Version { get; set; }
            public string Environment { get; set; }
        }
    }
}
