namespace core.Users;

public class UsersFeature : Feature
{
    public UsersFeature()
    {
        AddDependency<IUsersStore, UsersStore>();
    }
}
