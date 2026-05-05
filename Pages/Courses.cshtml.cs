using GridAcademy.Common;
using GridAcademy.DTOs.Marketplace;
using GridAcademy.Services.Marketplace;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridAcademy.Pages;

[AllowAnonymous]
public class CoursesModel : PageModel
{
    private readonly IStorefrontService _storefront;

    public CoursesModel(IStorefrontService storefront)
    {
        _storefront = storefront;
    }

    [BindProperty(SupportsGet = true)]
    public string? Category { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Q { get; set; }

    [BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;

    public PagedResult<TestSeriesListDto> Results { get; set; } = new();

    public IReadOnlyList<ExamCategoryDto> Categories { get; set; } = [];

    public async Task OnGetAsync()
    {
        var homepage = await _storefront.GetHomepageAsync();
        Categories = homepage.ExamCategories;

        Results = await _storefront.SearchSeriesAsync(new TestSeriesSearchRequest(
            Query:        Q,
            ExamTypeId:   null,
            CategorySlug: Category,
            SeriesType:   null,
            MinPrice:     null,
            MaxPrice:     null,
            Language:     null,
            SortBy:       "popular",
            Page:         CurrentPage,
            PageSize:     12
        ));
    }
}
