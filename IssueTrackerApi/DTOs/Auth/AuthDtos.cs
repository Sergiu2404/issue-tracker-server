namespace IssueTrackerApi.DTOs.Auth;

public class SignUpDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
}

public class SignInDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AuthResponseDto
{
    public string IdToken { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
}

public class ConfirmSignUpDto
{
    public string Email { get; set; } = string.Empty;
    public string ConfirmationCode { get; set; } = string.Empty;
}
