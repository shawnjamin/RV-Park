using System.ComponentModel.DataAnnotations;

namespace RVPark.Models;

public class Bill
{
    public int Id { get; set; }

    [Display(Name = "Reservation")]
    public int ReservationId { get; set; }

    public BillType Type { get; set; } = BillType.SiteCharge;

    [StringLength(500)]
    public string? Description { get; set; }

    [Range(typeof(decimal), "0.00", "999999.99")]
    [DataType(DataType.Currency)]
    public decimal Amount { get; set; }

    [Display(Name = "Created At")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Reservation? Reservation { get; set; }

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
