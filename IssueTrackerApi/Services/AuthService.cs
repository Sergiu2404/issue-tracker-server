using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using IssueTrackerApi.DTOs.Auth;
using IssueTrackerApi.Services.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace IssueTrackerApi.Services;

public class AuthService : IAuthService
{
    private readonly IAmazonCognitoIdentityProvider _cognito;
    private readonly string _clientId;
    private readonly string _clientSecret;

    public AuthService(IAmazonCognitoIdentityProvider cognito, IConfiguration config)
    {
        _cognito = cognito;
        _clientId = config["Aws:CognitoAppClientId"]!;
        _clientSecret = config["Aws:CognitoAppClientSecret"] ?? string.Empty;
    }

    public async Task SignUpAsync(SignUpDto dto)
    {
        var request = new SignUpRequest
        {
            ClientId = _clientId,
            Username = dto.Email,
            Password = dto.Password,
            UserAttributes =
            [
                new AttributeType { Name = "email", Value = dto.Email },
                new AttributeType { Name = "name", Value = dto.Username },
            ]
        };

        if (!string.IsNullOrEmpty(_clientSecret))
            request.SecretHash = ComputeSecretHash(dto.Email);

        await _cognito.SignUpAsync(request);
    }

    public async Task ConfirmSignUpAsync(ConfirmSignUpDto dto)
    {
        var request = new ConfirmSignUpRequest
        {
            ClientId = _clientId,
            Username = dto.Email,
            ConfirmationCode = dto.ConfirmationCode,
        };

        if (!string.IsNullOrEmpty(_clientSecret))
            request.SecretHash = ComputeSecretHash(dto.Email);

        await _cognito.ConfirmSignUpAsync(request);
    }

    public async Task<AuthResponseDto> SignInAsync(SignInDto dto)
    {
        var authParams = new Dictionary<string, string>
        {
            { "USERNAME", dto.Email },
            { "PASSWORD", dto.Password },
        };

        if (!string.IsNullOrEmpty(_clientSecret))
            authParams["SECRET_HASH"] = ComputeSecretHash(dto.Email);

        var request = new InitiateAuthRequest
        {
            AuthFlow = AuthFlowType.USER_PASSWORD_AUTH,
            ClientId = _clientId,
            AuthParameters = authParams,
        };

        var response = await _cognito.InitiateAuthAsync(request);
        var result = response.AuthenticationResult;

        return new AuthResponseDto
        {
            IdToken = result.IdToken,
            AccessToken = result.AccessToken,
            RefreshToken = result.RefreshToken,
            ExpiresIn = result.ExpiresIn,
        };
    }

    // Cognito requires HMAC-SHA256 hash of username+clientId when a client secret is configured.
    private string ComputeSecretHash(string username)
    {
        var message = username + _clientId;
        var keyBytes = Encoding.UTF8.GetBytes(_clientSecret);
        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
        return Convert.ToBase64String(hash);
    }
}
