namespace RbacSystem.Domain.Common;

public static class EntityId
{
    public static string New()
    {
        return Guid.NewGuid().ToString("D");
    }
}
