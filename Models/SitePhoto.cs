using System.ComponentModel.DataAnnotations;

namespace RVPark.Models;

public class SitePhoto
{
    public int Id { get; set; }

    public int SiteId { get; set; }
    
    public Site? Site { get; set; }

    [Required]
    public string Url { get; set; } = string.Empty;

    [StringLength(255)]
    public string? Caption { get; set; }
}