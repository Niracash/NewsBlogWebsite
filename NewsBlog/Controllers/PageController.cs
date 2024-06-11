using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsBlog.Data;
using NewsBlog.ViewModels;

namespace NewsBlog.Controllers
{
    public class PageController : Controller
    {
        private readonly AppDbContext _db;
        public PageController(AppDbContext db)
        {
            _db = db;
        }
        public IActionResult Weather()
        {
            return View();
        }
        public async Task<IActionResult> About()
        {
            var page = await _db.Pages!.FirstOrDefaultAsync(x => x.Slug == "about");
            var pageViewModel = new PageViewModel()
            {
                Title = page!.Title,
                Description = page.Description,
            };
            return View(pageViewModel);
        }
        public async Task<IActionResult> Contact()
        {
            var page = await _db.Pages!.FirstOrDefaultAsync(x => x.Slug == "contact");
            var pageViewModel = new PageViewModel()
            {
                Title = page!.Title,
                Description = page.Description,
            };
            return View(pageViewModel);
        }
    }
}
