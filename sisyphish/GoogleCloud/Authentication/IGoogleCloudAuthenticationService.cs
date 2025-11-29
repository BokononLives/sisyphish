namespace sisyphish.GoogleCloud.Authentication;

public interface IGoogleCloudAuthenticationService
{
    Task<string> GetAccessToken();
}
