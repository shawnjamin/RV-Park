using System.ComponentModel.DataAnnotations;

namespace RVPark.Models;

public class EmployeeReservationFormViewModel
{
    [Required]
    [Display(Name = "Customer")]
    public int CustomerId { get; set; }

    [Required]
    [Display(Name = "Site")]
    public int SiteId { get; set; }

    [Required]
    [Display(Name = "Check-In")]
    public DateTime StartDate { get; set; }

    [Required]
    [Display(Name = "Check-Out")]
    public DateTime EndDate { get; set; }

    [Range(1, int.MaxValue)]
    [Display(Name = "Adults")]
    public int AdultCount { get; set; } = 1;

    [Range(0, int.MaxValue)]
    [Display(Name = "Children")]
    public int ChildCount { get; set; }

    [Range(0, int.MaxValue)]
    [Display(Name = "Pets")]
    public int PetCount { get; set; }

    [StringLength(1000)]
    [Display(Name = "Special Requests or Notes")]
    public string? SpecialRequestsOrNotes { get; set; }

    [Required]
    [Display(Name = "Payment Method")]
    public PaymentMethod PaymentMethod { get; set; }

    [StringLength(1000)]
    [Display(Name = "Payment Notes or Reference")]
    public string? PaymentNotes { get; set; }
}