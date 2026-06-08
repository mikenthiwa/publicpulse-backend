using FluentValidation;

namespace Web.Features.Reports.ListReport;

public sealed class ListReportValidator : AbstractValidator<ListReportRequest>
{
    public ListReportValidator()
    {
        RuleFor(request => request.PageNumber)
            .GreaterThanOrEqualTo(1)
            .When(request => request.PageNumber.HasValue);

        RuleFor(request => request.PageSize)
            .InclusiveBetween(1, ListReportRequest.MaximumPageSize)
            .When(request => request.PageSize.HasValue);

        RuleFor(request => request)
            .Must(HaveRepresentableOffset)
            .WithMessage("The requested page is too large.");
    }

    private static bool HaveRepresentableOffset(ListReportRequest request)
    {
        var pageNumber = request.PageNumber ?? ListReportRequest.DefaultPageNumber;
        var pageSize = request.PageSize ?? ListReportRequest.DefaultPageSize;

        return ((long)pageNumber - 1) * pageSize <= int.MaxValue;
    }
}
