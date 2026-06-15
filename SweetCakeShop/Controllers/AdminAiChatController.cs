using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SweetCakeShop.Constants;

namespace SweetCakeShop.Controllers
{
    [Authorize(Roles = nameof(Roles.Admin))]
    public class AdminAiChatController : Controller
    {
        [HttpGet]
        public IActionResult Index() => RedirectToAction("Index", "AdminDashboard");
    }
}
