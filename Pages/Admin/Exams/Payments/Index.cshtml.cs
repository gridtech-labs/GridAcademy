using GridAcademy.Data.Entities.Payment;
using GridAcademy.DTOs.Payment;
using GridAcademy.Services.Payment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridAcademy.Pages.Admin.Exams.Payments;

[Authorize(Roles = "Admin")]
public class IndexModel(IExamPaymentService paymentSvc) : PageModel
{
    public List<ExamOrderListItem> Orders { get; set; } = [];
    public int CurrentPage { get; set; } = 1;
    public const int PageSize = 30;
    public ExamOrderStatus? StatusFilter { get; set; }

    public async Task OnGetAsync(int page = 1, ExamOrderStatus? status = null)
    {
        CurrentPage  = Math.Max(1, page);
        StatusFilter = status;
        Orders = await paymentSvc.GetOrdersAsync(status, CurrentPage, PageSize);
    }
}
