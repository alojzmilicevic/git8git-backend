namespace core.Licensing;

public class LicensingFeature : Feature
{
    public LicensingFeature()
    {
        AddDependency<IDomainsStore, DomainsStore>();
        AddDependency<ILicenseService, LicenseService>();
    }
}
