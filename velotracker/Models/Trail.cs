using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace velotracker.Models
{
    public class Trail
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        [Required]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public double DistanceKm { get; set; }

        [Required]
        public int ElevationGainM { get; set; }

        [Required]
        public string TrailType { get; set; } = string.Empty;

        [Required]
        public string VerificationStatus { get; set; } = "pending";

        [Required]
        public double StartLatitude { get; set; }

        [Required]
        public double StartLongitude { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<TrailPoint> TrailPoints { get; set; } = new List<TrailPoint>();
    }
}