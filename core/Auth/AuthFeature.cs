namespace core.Auth;

public class AuthFeature : Feature
{
    public AuthFeature()
    {
        AddSettings<AuthSettings>();
        AddDependency<ICryptoService, CryptoService>();
        AddDependency<IJwtService, JwtService>();
    }
}
