using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using velotracker.Models;

namespace velotracker.Services
{
    public class RouteData
    {
        public double DistanceKm { get; set; }
        public int ElevationGainM { get; set; }
        public TimeSpan Duration { get; set; }
        public List<TrailPoint> Points { get; set; } = new List<TrailPoint>();
    }

    public class RouteParsingService
    {
        public async Task<RouteData> ParseFileAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default)
        {
            if (fileName.EndsWith(".gpx", StringComparison.OrdinalIgnoreCase))
            {
                return await ParseGpxAsync(fileStream, cancellationToken);
            }
            if (fileName.EndsWith(".tcx", StringComparison.OrdinalIgnoreCase))
            {
                return await ParseTcxAsync(fileStream, cancellationToken);
            }
            throw new NotSupportedException("Niewspierany format pliku.");
        }

        private async Task<RouteData> ParseGpxAsync(Stream stream, CancellationToken cancellationToken)
        {
            var doc = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken);
            XNamespace ns = doc.Root?.GetDefaultNamespace() ?? "";

            var trkpts = doc.Descendants(ns + "trkpt").ToList();
            if (!trkpts.Any()) return new RouteData();

            var points = new List<PointData>();
            foreach (var pt in trkpts)
            {
                if (double.TryParse(pt.Attribute("lat")?.Value, out double lat) &&
                    double.TryParse(pt.Attribute("lon")?.Value, out double lon))
                {
                    double? ele = null;
                    if (double.TryParse(pt.Element(ns + "ele")?.Value, out double e))
                        ele = e;

                    DateTime? time = null;
                    if (DateTime.TryParse(pt.Element(ns + "time")?.Value, out DateTime t))
                        time = t;

                    points.Add(new PointData { Lat = lat, Lon = lon, Ele = ele, Time = time });
                }
            }
            return await Task.Run(() => CalculateRouteData(points), cancellationToken);
        }

        private async Task<RouteData> ParseTcxAsync(Stream stream, CancellationToken cancellationToken)
        {
            var doc = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken);
            XNamespace ns = doc.Root?.GetDefaultNamespace() ?? "";

            var trackpoints = doc.Descendants(ns + "Trackpoint").ToList();
            if (!trackpoints.Any()) return new RouteData();

            var points = new List<PointData>();
            foreach (var pt in trackpoints)
            {
                var position = pt.Element(ns + "Position");
                if (position != null)
                {
                    if (double.TryParse(position.Element(ns + "LatitudeDegrees")?.Value, out double lat) &&
                        double.TryParse(position.Element(ns + "LongitudeDegrees")?.Value, out double lon))
                    {
                        double? ele = null;
                        if (double.TryParse(pt.Element(ns + "AltitudeMeters")?.Value, out double e))
                            ele = e;

                        DateTime? time = null;
                        if (DateTime.TryParse(pt.Element(ns + "Time")?.Value, out DateTime t))
                            time = t;

                        points.Add(new PointData { Lat = lat, Lon = lon, Ele = ele, Time = time });
                    }
                }
            }

            return await Task.Run(() => CalculateRouteData(points), cancellationToken);
        }

        private RouteData CalculateRouteData(List<PointData> points)
        {
            var result = new RouteData();
            if (points.Count < 2) return result;

            double totalDistance = 0;
            double elevationGain = 0;

            for (int i = 1; i < points.Count; i++)
            {
                var prev = points[i - 1];
                var curr = points[i];

                totalDistance += CalculateDistance(prev.Lat, prev.Lon, curr.Lat, curr.Lon);

                if (prev.Ele.HasValue && curr.Ele.HasValue && curr.Ele > prev.Ele)
                {
                    elevationGain += (curr.Ele.Value - prev.Ele.Value);
                }
            }

            var firstTime = points.FirstOrDefault(p => p.Time.HasValue)?.Time;
            var lastTime = points.LastOrDefault(p => p.Time.HasValue)?.Time;

            TimeSpan duration = TimeSpan.Zero;
            if (firstTime.HasValue && lastTime.HasValue && lastTime > firstTime)
            {
                duration = lastTime.Value - firstTime.Value;
            }

            result.DistanceKm = totalDistance;
            result.ElevationGainM = (int)Math.Round(elevationGain);
            result.Duration = duration;

            int order = 1;
            foreach (var p in points)
            {
                result.Points.Add(new TrailPoint
                {
                    Latitude = p.Lat,
                    Longitude = p.Lon,
                    ElevationM = p.Ele,
                    SequenceOrder = order++
                });
            }

            return result;
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var R = 6371; // promień Ziemi
            var dLat = Deg2Rad(lat2 - lat1);
            var dLon = Deg2Rad(lon2 - lon1);
            var a =
                Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(Deg2Rad(lat1)) * Math.Cos(Deg2Rad(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double Deg2Rad(double deg)
        {
            return deg * (Math.PI / 180);
        }

        private class PointData
        {
            public double Lat { get; set; }
            public double Lon { get; set; }
            public double? Ele { get; set; }
            public DateTime? Time { get; set; }
        }
    }
}