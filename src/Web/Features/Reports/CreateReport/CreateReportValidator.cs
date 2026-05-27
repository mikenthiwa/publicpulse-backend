using FluentValidation;
using Microsoft.Extensions.Options;

namespace Web.Features.Reports.CreateReport;

public class CreateReportValidator : AbstractValidator<CreateReportRequest>
{
    public CreateReportValidator(
        IOptions<CloudinaryOptions> options,
        IReportImageCloudinaryService imageCloudinaryService,
        IHttpContextAccessor httpContextAccessor)
    {
        var cloudinaryOptions = options.Value;

        RuleFor(request => request.Description)
            .NotEmpty()
            .WithMessage("Description is required.");

        RuleFor(request => request.CategoryId)
            .NotEmpty()
            .WithMessage("Category is required.");

        RuleFor(request => request.Images)
            .NotNull()
            .WithMessage("At least one image is required.")
            .NotEmpty()
            .WithMessage("At least one image is required.");

        RuleFor(request => request.Images)
            .Must(images => images.Count <= cloudinaryOptions.MaxImagesPerReport)
            .WithMessage($"A report can include at most {cloudinaryOptions.MaxImagesPerReport} images.")
            .When(request => request.Images is not null);

        RuleFor(request => request.County)
            .NotEmpty()
            .WithMessage("County is required.");

        RuleFor(request => request.RoadName)
            .NotEmpty()
            .WithMessage("Road name is required.");

        RuleForEach(request => request.Images)
            .NotNull()
            .WithMessage("Image metadata is required.")
            .ChildRules(image =>
            {
                image.RuleFor(request => request.PublicId)
                    .NotEmpty()
                    .WithMessage("Cloudinary public ID is required.");

                image.RuleFor(request => request.Version)
                    .NotEmpty()
                    .WithMessage("Cloudinary version is required.");

                image.RuleFor(request => request.Signature)
                    .NotEmpty()
                    .WithMessage("Cloudinary signature is required.");

                image.RuleFor(request => request)
                    .Must(request => IsUploadedForCurrentUser(request, imageCloudinaryService, httpContextAccessor))
                    .WithMessage("Image was not uploaded for the current user.")
                    .When(request => !string.IsNullOrWhiteSpace(request.PublicId));

                image.RuleFor(request => request)
                    .Must(imageCloudinaryService.IsUploadResultValid)
                    .WithMessage("Cloudinary image signature is invalid.")
                    .When(request =>
                        !string.IsNullOrWhiteSpace(request.PublicId)
                        && !string.IsNullOrWhiteSpace(request.Version)
                        && !string.IsNullOrWhiteSpace(request.Signature));
            })
            .When(request => request.Images is not null);
    }

    private static bool IsUploadedForCurrentUser(
        CreateReportImageRequest image,
        IReportImageCloudinaryService imageCloudinaryService,
        IHttpContextAccessor httpContextAccessor)
    {
        var user = httpContextAccessor.HttpContext?.User
            ?? throw new UnauthorizedAccessException("Authenticated user is required.");

        var userId = ReportUserClaims.GetUserId(user);

        return image.PublicId.Trim().StartsWith(
            $"{imageCloudinaryService.GetUserFolder(userId)}/",
            StringComparison.Ordinal);
    }
}
