using Microsoft.AspNetCore.Mvc;

namespace EliteFIPServer.Controllers {
    [Route("/")]
    public class HomeController : Controller {

        [HttpGet]
        public ContentResult Index() {
            return Content("Elite FIP Panel Server running");
        }
    }
}
