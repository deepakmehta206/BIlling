using Billing.Application.Common;
using Billing.Application.Dtos;

namespace Billing.Application.Abstractions;

public interface IAuthService
{
    Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<ServiceResult<CurrentUserDto>> MeAsync(CancellationToken cancellationToken);
}
