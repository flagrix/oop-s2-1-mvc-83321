using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using VgcCollege.Web.Models;

namespace VgcCollege.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View();
    public IActionResult Privacy() => View();

    [Route("AccessDenied")]
    public IActionResult AccessDenied() => View();

    [Route("NotFound")]
    public IActionResult NotFoundPage() => View("NotFound");

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
