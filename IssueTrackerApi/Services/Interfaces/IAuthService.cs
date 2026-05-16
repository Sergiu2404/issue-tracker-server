using IssueTrackerApi.DTOs.Auth;

namespace IssueTrackerApi.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> SignInAsync(SignInDto dto);
    Task SignUpAsync(SignUpDto dto);
    Task ConfirmSignUpAsync(ConfirmSignUpDto dto);
}
