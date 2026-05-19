using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using velotracker.Data;
using velotracker.Models;

namespace velotracker.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(double? minDistance, double? maxDistance, int? minElevation, int? maxElevation, string? difficulty, string? searchString)
        {
            var query = _context.Trails.Include(t => t.User).AsQueryable();

            if (minDistance.HasValue)
                query = query.Where(t => t.DistanceKm >= minDistance.Value);

            if (maxDistance.HasValue)
                query = query.Where(t => t.DistanceKm <= maxDistance.Value);

            if (minElevation.HasValue)
                query = query.Where(t => t.ElevationGainM >= minElevation.Value);

            if (maxElevation.HasValue)
                query = query.Where(t => t.ElevationGainM <= maxElevation.Value);

            if (!string.IsNullOrEmpty(difficulty))
                query = query.Where(t => t.Difficulty == difficulty);

            if (!string.IsNullOrEmpty(searchString))
                query = query.Where(t => t.Title.Contains(searchString) || (t.Description != null && t.Description.Contains(searchString)));

            var trails = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();

            ViewData["MinDistance"] = minDistance;
            ViewData["MaxDistance"] = maxDistance;
            ViewData["MinElevation"] = minElevation;
            ViewData["MaxElevation"] = maxElevation;
            ViewData["Difficulty"] = difficulty;
            ViewData["SearchString"] = searchString;

            return View(trails);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
