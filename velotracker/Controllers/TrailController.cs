using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
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
        public async Task<IActionResult> Import(IFormFile routeFile, string title, string trailType)
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

                var routeData = await _routeService.ParseFileAsync(stream, routeFile.FileName);

                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdStr, out var userId))
                {
                    return Challenge();
                }

                var trail = new Trail
                {
                    UserId = userId,
                    Title = string.IsNullOrWhiteSpace(title) ? "Super trasa" : title,
                    TrailType = trailType,
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

                TempData["ToastMessage"] = "Trasa została pomyślnie zaimportowana!";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Błąd podczas parsowania: {ex.Message}");
                return View();
            }
        }

        [AllowAnonymous]
        public async Task<IActionResult> Download(int id)
        {
            var trail = await _context.Trails
                .Include(t => t.TrailPoints)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (trail == null)
            {
                return NotFound("Nie znaleziono takiej trasy.");
            }

            XNamespace ns = "http://www.topografix.com/GPX/1/1";

            var trackPoints = trail.TrailPoints
                .OrderBy(p => p.SequenceOrder)
                .Select(p => {
                    var element = new XElement(ns + "trkpt",
                        new XAttribute("lat", p.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                        new XAttribute("lon", p.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture))
                    );

                    if (p.ElevationM.HasValue)
                    {
                        element.Add(new XElement(ns + "ele", p.ElevationM.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)));
                    }

                    return element;
                });

            var gpxDoc = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement(ns + "gpx",
                    new XAttribute("version", "1.1"),
                    new XAttribute("creator", "VeloTracker"),
                    new XElement(ns + "trk",
                        new XElement(ns + "name", trail.Title),
                        new XElement(ns + "type", trail.TrailType),
                        new XElement(ns + "trkseg", trackPoints)
                    )
                )
            );

            var memoryStream = new MemoryStream();
            using (var writer = new StreamWriter(memoryStream, Encoding.UTF8, leaveOpen: true))
            {
                gpxDoc.Save(writer);
            }
            memoryStream.Position = 0;

            var safeTitle = string.Join("_", trail.Title.Split(Path.GetInvalidFileNameChars()));
            var fileName = $"{safeTitle}.gpx";

            return File(memoryStream, "application/gpx+xml", fileName);
        }
    }
}