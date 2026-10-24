using Billing.Application.Common;
using Billing.Application.Dtos;

namespace Billing.Application.Abstractions;

public interface IStaffService
{
    Task<ServiceResult<StaffDeskDto>> GetAsync(Guid? restaurantId, CancellationToken cancellationToken);
    Task<ServiceResult<EmployeeDto>> HireAsync(Guid? restaurantId, SaveEmployeeRequest request, CancellationToken cancellationToken);
    Task<ServiceResult<EmployeeDto>> UpdateAsync(Guid? restaurantId, Guid userId, SaveEmployeeRequest request, CancellationToken cancellationToken);
    Task<ServiceResult<MessageDto>> ArchiveAsync(Guid? restaurantId, Guid userId, CancellationToken cancellationToken);
    Task<ServiceResult<LeaveDto>> RequestLeaveAsync(Guid? restaurantId, SaveLeaveRequest request, CancellationToken cancellationToken);
    Task<ServiceResult<LeaveDto>> DecideLeaveAsync(Guid? restaurantId, Guid leaveId, DecideLeaveRequest request, CancellationToken cancellationToken);
}

public interface IAttendanceService
{
    Task<ServiceResult<AttendanceDeskDto>> GetAsync(Guid? restaurantId, CancellationToken cancellationToken);
    Task<ServiceResult<ShiftDto>> SaveShiftAsync(Guid? restaurantId, SaveShiftRequest request, CancellationToken cancellationToken);
    Task<ServiceResult<MessageDto>> ArchiveShiftAsync(Guid? restaurantId, Guid shiftId, CancellationToken cancellationToken);
    Task<ServiceResult<MessageDto>> AssignAsync(Guid? restaurantId, AssignShiftRequest request, CancellationToken cancellationToken);
    Task<ServiceResult<MessageDto>> CheckInAsync(Guid? restaurantId, MarkAttendanceRequest request, CancellationToken cancellationToken);
    Task<ServiceResult<MessageDto>> CheckOutAsync(Guid? restaurantId, MarkAttendanceRequest request, CancellationToken cancellationToken);
}
