using System.ComponentModel.DataAnnotations;

namespace RVPark.Models;

public class SiteType
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Range(typeof(decimal), "0.00", "999999.99")]
    [DataType(DataType.Currency)]
    public decimal Price { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public ICollection<Site> Sites { get; set; } = new List<Site>();
}
