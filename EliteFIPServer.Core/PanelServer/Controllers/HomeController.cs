using Microsoft.AspNetCore.Mvc;

namespace EliteFIPServer
{
    [Route("/")]
    public class HomeController : Controller
    {

        [HttpGet]
        public RedirectResult Index()
        {
            return Redirect("/index.html");
        }
    }
}
