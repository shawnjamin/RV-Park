using System.ComponentModel.DataAnnotations;

namespace RVPark.Models;

public class ResrvationReportViewModel
{
    [DataType(DataType.Date)]
    [Display(Name = "Start Date")]
    public DateTime? StartDate { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "End Date")]
    public DateTime? EndDate { get; set; }

    public List<ResrvationReportRowViewModel> Reservations { get; set; } = new();
}

public class ReservationReportRowViewModel {
    public string ReservationNumber { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string SiteNumber { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public ReservationStatus ReservationStatus { get; set; }

    public string ReportStatus { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}