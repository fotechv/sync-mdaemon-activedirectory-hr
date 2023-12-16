namespace WebApiToken
{
    public interface IJwtAuth
    {
        string Authentication(string username, string password);
        object Authentication2(string username, string password);
    }
}