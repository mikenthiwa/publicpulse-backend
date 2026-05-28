using FluentValidation;
using Microsoft.Extensions.Options;
using Web.Infrastructure.Identity;

namespace Web.Features.Reports.CreateReport;

public class CreateReportValidator : AbstractValidator<CreateReportRequest>
{
    public CreateReportValidator(
        IOptions<CloudinaryOptions> options,
        IReportImageCloudinaryService imageCloudinaryService,
        ICurrentUser currentUser)
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

        RuleFor(request => request.Latitude)
            .InclusiveBetween(-90, 90)
            .When(request => request.Latitude.HasValue)
            .WithMessage("Latitude must be between -90 and 90.");

        RuleFor(request => request.Longitude)
            .InclusiveBetween(-180, 180)
            .When(request => request.Longitude.HasValue)
            .WithMessage("Longitude must be between -180 and 180.");

        RuleFor(request => request)
            .Must(request => request.Latitude.HasValue == request.Longitude.HasValue)
            .WithMessage("Latitude and longitude must be provided together.");

        RuleFor(request => request.LocationLabel)
            .MaximumLength(500)
            .When(request => request.LocationLabel is not null);

        RuleFor(request => request.LocationSource)
            .MaximumLength(100)
            .When(request => request.LocationSource is not null);

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
                    .Must(request => IsUploadedForCurrentUser(request, imageCloudinaryService, currentUser))
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
        ICurrentUser currentUser)
    {
        return image.PublicId.Trim().StartsWith(
            $"{imageCloudinaryService.GetUserFolder(currentUser.UserId)}/",
            StringComparison.Ordinal);
    }
}
