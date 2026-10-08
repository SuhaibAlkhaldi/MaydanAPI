using FluentValidation;
using Maydan.Application.DTOs.ServiceRequests;
using Maydan.Application.Interfaces;

namespace Maydan.Application.Validators;

public class CreateServiceRequestDtoValidator : AbstractValidator<CreateServiceRequestDto>
{
    public CreateServiceRequestDtoValidator(IUnitOfWork unitOfWork)
    {
        RuleFor(x => x.ProjectId).GreaterThan(0).WithMessage("Project is required.");
        RuleFor(x => x.ServiceId).GreaterThan(0).WithMessage("Service is required.");
        RuleFor(x => x.StartDate).NotEmpty().WithMessage("Start date is required.");
        RuleFor(x => x.EndDate).NotEmpty().WithMessage("End date is required.");
        RuleFor(x => x).Must(x => x.EndDate >= x.StartDate).WithMessage("EndDate must be the same or after StartDate.");
        RuleFor(x => x.AttendanceFrequency).GreaterThan(0).WithMessage("Attendance frequency must be greater than zero.");
        RuleFor(x => x.CityId).GreaterThan(0).WithMessage("City is required.");
        // Latitude/Longitude are optional if the Association for the CityId has coordinates; otherwise they are required.
        //RuleFor(x => x).MustAsync(async (dto, ct) =>
        //{
        //    if (dto.Latitude.HasValue && dto.Longitude.HasValue)
        //    {
        //        return true;
        //    }

        //    // find association for the city
        //    var associations = await unitOfWork.Associations.GetAllAsync(ct);
        //    var assoc = associations.FirstOrDefault(a => a.CityId == dto.CityId);
        //    if (assoc == null) return false;

        //    // association must have coordinates if dto doesn't
        //    return assoc.Latitude.HasValue && assoc.Longitude.HasValue;
        //}).WithMessage("Latitude and Longitude are required if the association for the selected city has no coordinates or no association is found.");

        // Ensure the Start/End dates do not exceed the Project duration
        RuleFor(x => x).MustAsync(async (dto, ct) =>
        {
            var project = await unitOfWork.Projects.GetByIdAsync(dto.ProjectId, ct);
            if (project == null) return false;
            return dto.StartDate >= project.StartDate && dto.EndDate <= project.EndDate;
        }).WithMessage("Service request dates must be within the project's start and end dates.");
    }
}
