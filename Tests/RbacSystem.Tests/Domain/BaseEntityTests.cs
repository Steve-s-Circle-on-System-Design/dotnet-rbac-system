namespace RbacSystem.Tests.Domain;

public class BaseEntityTests
{
    [Fact]
    public void BaseEntity_ShouldHaveNonEmptyId_OnCreation()
    {
        var entity = new TestEntity();
        Assert.NotEqual(Guid.Empty, entity.Id);
    }

    [Fact]
    public void BaseEntity_CreatedAt_ShouldBeUtcNow()
    {
        DateTime before = DateTime.UtcNow.AddSeconds(-1);
        var entity = new TestEntity();
        DateTime after = DateTime.UtcNow.AddSeconds(1);

        Assert.InRange(entity.CreatedAt, before, after);
    }

    private sealed class TestEntity : RbacSystem.Domain.Common.BaseEntity { }
}
