using Microsoft.AspNetCore.Mvc;

namespace LykkePublicAPI.Controllers
{
    [Route("home/[controller]")]
    public class VersionController : Controller
    {
        [HttpGet]
        public VersionModel Get()
        {
            return new VersionModel
            {
                Version =
                    Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default.Application.ApplicationVersion
            };
        }

        public class VersionModel
        {
            public string Version { get; set; }
        }
    }
}
