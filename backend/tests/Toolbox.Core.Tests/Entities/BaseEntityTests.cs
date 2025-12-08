using FluentAssertions;
using Toolbox.Core.Entities;

namespace Toolbox.Core.Tests.Entities;

public class BaseEntityTests
{
    [Fact]
    public void BaseEntity_Should_Have_Id_Property()
    {
        // Arrange & Act
        var entity = new TestEntity();

        //Assert
        entity.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void BaseEntity_Should_Set_CreatedAt_On_Initialization()
    {
        //Arrange & Act
        var entity = new TestEntity();

        //Assert
        entity.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void BaseEntity_Should_Update_UpdateAt_When_Modified()
    {
        //Arrange 
        var entity = new TestEntity();
        var originalUpdatedAt = entity.UpdatedAt;

        //Act
        Thread.Sleep(100); // Small delay
        entity.UpdateModifiedTimestamp();

        entity.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }
}

// Helper class for testing abstract BaseEntity
public class TestEntity : BaseEntity
{
    public void UpdateModifiedTimestamp()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}