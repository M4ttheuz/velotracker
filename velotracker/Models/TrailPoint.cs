using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace velotracker.Models
{
	public class TrailPoint
	{
		[Key]
		public int Id { get; set; }

		[Required]
		public int TrailId { get; set; }

		[ForeignKey("TrailId")]
		public Trail Trail { get; set; } = null!;

		[Required]
		public double Latitude { get; set; }

		[Required]
		public double Longitude { get; set; }

		public double? ElevationM { get; set; }

		[Required]
		public int SequenceOrder { get; set; }
	}
}