using System.ComponentModel.DataAnnotations;

namespace RVPark.Models;

public class Reservation
{
    public int Id { get; set; }

    [Required]
    [StringLength(32)]
    [Display(Name = "Reservation Number")]
    public string ReservationNumber { get; set; } = string.Empty;

    [Display(Name = "Customer")]
    public int CustomerId { get; set; }

    [Display(Name = "Site")]
    public int SiteId { get; set; }

    [StringLength(1000)]
    [Display(Name = "Special Requests or Notes")]
    public string? SpecialRequestsOrNotes { get; set; }

    [Range(0, int.MaxValue)]
    [Display(Name = "Adults")]
    public int AdultCount { get; set; }

    [Range(0, int.MaxValue)]
    [Display(Name = "Children")]
    public int ChildCount { get; set; }

    [Range(0, int.MaxValue)]
    [Display(Name = "Pets")]
    public int PetCount { get; set; }

    [StringLength(1000)]
    [Display(Name = "Pet Notes")]
    public string? PetNotes { get; set; }

    [DataType(DataType.DateTime)]
    [Display(Name = "Start Date")]
    public DateTime StartDate { get; set; }

    [DataType(DataType.DateTime)]
    [Display(Name = "End Date")]
    public DateTime EndDate { get; set; }

    public ReservationStatus Status { get; set; } = ReservationStatus.PendingPayment;

    [Display(Name = "Created At")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Display(Name = "Cancelled At")]
    public DateTime? CancelledAt { get; set; }

    [Display(Name = "Checked In At")]
    public DateTime? CheckedInAt { get; set; }

    [Display(Name = "Checked Out At")]
    public DateTime? CheckedOutAt { get; set; }

    public Customer? Customer { get; set; }

    public Site? Site { get; set; }

    public ICollection<Bill> Bills { get; set; } = new List<Bill>();
}
