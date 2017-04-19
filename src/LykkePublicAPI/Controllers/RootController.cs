using Microsoft.AspNetCore.Mvc;

namespace LykkePublicAPI.Controllers
{
    [Route("")]
    public class RootController : Controller
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Redirect("~/swagger/ui");
        }
    }
}
