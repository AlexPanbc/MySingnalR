using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MySingnalR.Controllers
{
    public class ShakeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Title = "PC端";

            return View();
        }
    }
}
