using System;
using Lykke.Common;
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
                Name = AppEnvironment.Name,
                Version = AppEnvironment.Version,
                Env = Environment.GetEnvironmentVariable("ENV_INFO"),
            };
        }

        public class IsAliveResponse
        {
            public string Name { get; set; }
            public string Version { get; set; }
            public string Env { get; set; }
        }
    }
}
