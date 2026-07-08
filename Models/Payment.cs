using System.ComponentModel.DataAnnotations;

namespace RVPark.Models;

public class Payment
{
    public int Id { get; set; }

    [Display(Name = "Bill")]
    public int BillId { get; set; }

    [Display(Name = "Payment Method")]
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

    [StringLength(255)]
    [Display(Name = "Stripe Transaction ID")]
    public string? StripeTransactionId { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    [Range(typeof(decimal), "0.00", "999999.99")]
    [DataType(DataType.Currency)]
    public decimal Amount { get; set; }

    [Display(Name = "Paid At")]
    public DateTime PaidAt { get; set; }

    public Bill? Bill { get; set; }
}
