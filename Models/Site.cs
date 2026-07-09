using System.ComponentModel.DataAnnotations;

namespace RVPark.Models;

public class Site
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Site Type")]
    public int SiteTypeId { get; set; }

    [Required]
    [StringLength(20)]
    [Display(Name = "Site Number")]
    public string SiteNumber { get; set; } = string.Empty;

    [Display(Name = "Hookup Type")]
    public HookupType HookupType { get; set; } = HookupType.FullHookup;

    [Range(1, int.MaxValue)]
    [Display(Name = "Size (sq ft)")]
    public int? SizeSqft { get; set; }

    [Url]
    [StringLength(2048)]
    [Display(Name = "Photo URL")]
    public string? PhotoUrl { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public SiteType? SiteType { get; set; }

    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

    public ICollection<SitePhoto> Photos { get; set; } = new List<SitePhoto>();
}
