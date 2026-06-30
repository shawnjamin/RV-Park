using System.ComponentModel.DataAnnotations;

namespace RVPark.Models;

public class RvSite
{
    public int Id { get; set; }

    [Required]
    [StringLength(20)]
    [Display(Name = "Site Number")]
    public string SiteNumber { get; set; } = string.Empty;

    [Range(1, 100)]
    [Display(Name = "Max RV Length")]
    public int MaxRvLength { get; set; }

    [Range(typeof(decimal), "0.01", "999.99")]
    [DataType(DataType.Currency)]
    [Display(Name = "Nightly Rate")]
    public decimal NightlyRate { get; set; }

    [Required]
    [StringLength(40)]
    [Display(Name = "Hookup Type")]
    public string HookupType { get; set; } = string.Empty;

    [Display(Name = "Available")]
    public bool IsAvailable { get; set; } = true;
}
