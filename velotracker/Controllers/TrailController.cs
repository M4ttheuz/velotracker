using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using velotracker.Data;
using velotracker.Models;
using velotracker.Services;

namespace velotracker.Controllers
{
    [Authorize]
    public class TrailController : Controller
    {
        private readonly AppDbContext _context;
        private readonly RouteParsingService _routeService;

        public TrailController(AppDbContext context, RouteParsingService routeService)
        {
            _context = context;
            _routeService = routeService;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var trail = await _context.Trails
                .Include(t => t.User)
                .Include(t => t.TrailPoints)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (trail == null)
            {
                return NotFound();
            }

            return View(trail);
        }

        public IActionResult Import()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Import(IFormFile routeFile, string title, string difficulty)
        {
            if (routeFile == null || routeFile.Length == 0)
            {
                ModelState.AddModelError("", "Wybierz plik do importu.");
                return View();
            }

            var extension = Path.GetExtension(routeFile.FileName).ToLowerInvariant();
            if (extension != ".gpx" && extension != ".tcx")
            {
                ModelState.AddModelError("", "Tylko pliki GPX i TCX są akceptowalne.");
                return View();
            }

            try
            {
                using var stream = routeFile.OpenReadStream();
                var routeData = _routeService.ParseFile(stream, routeFile.FileName);

                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdStr, out var userId))
                {
                    return Challenge();
                }

                var trail = new Trail
                {
                    UserId = userId,
                    Title = title ?? "Super trasa",
                    Difficulty = difficulty ?? "Medium",
                    DistanceKm = routeData.DistanceKm,
                    ElevationGainM = routeData.ElevationGainM,
                    CreatedAt = DateTime.UtcNow,
                    VerificationStatus = "pending",
                    StartLatitude = routeData.Points.FirstOrDefault()?.Latitude ?? 0,
                    StartLongitude = routeData.Points.FirstOrDefault()?.Longitude ?? 0,
                    TrailPoints = routeData.Points
                };

                _context.Trails.Add(trail);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Błąd podczas parsowania: {ex.Message}");
                return View();
            }
        }
    }
}