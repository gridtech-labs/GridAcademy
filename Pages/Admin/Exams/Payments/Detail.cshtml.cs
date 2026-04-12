using GridAcademy.DTOs.Payment;
using GridAcademy.Services.Payment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridAcademy.Pages.Admin.Exams.Payments;

[Authorize(Roles = "Admin")]
public class DetailModel(IExamPaymentService paymentSvc) : PageModel
{
    public ExamOrderDetail? Order { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        Order = await paymentSvc.GetOrderDetailAsync(id);
        if (Order == null) return NotFound();
        return Page();
    }
}
