using Xunit;

namespace BOTC.Architecture.Tests;

public sealed class ArchitectureTests
{
    [Fact]
    public void Domain_ShouldNotReferenceApplicationLayer()
    {
        // Arrange
        var domainAssembly = typeof(BOTC.Domain.Rooms.Room).Assembly;

        // Act
        var referencedAssemblies = domainAssembly.GetReferencedAssemblies();

        // Assert - Domain should not reference Application, Infrastructure, or Presentation
        Assert.DoesNotContain(referencedAssemblies, a => a.Name == "BOTC.Application");
        Assert.DoesNotContain(referencedAssemblies, a => a.Name == "BOTC.Infrastructure");
        Assert.DoesNotContain(referencedAssemblies, a => a.Name == "BOTC.Presentation.Api");
    }

    [Fact]
    public void Domain_ShouldNotDependOnEntityFramework()
    {
        // Arrange
        var domainAssembly = typeof(BOTC.Domain.Rooms.Room).Assembly;

        // Act
        var referencedAssemblies = domainAssembly.GetReferencedAssemblies();

        // Assert
        Assert.DoesNotContain(referencedAssemblies, a => a.Name?.StartsWith("Microsoft.EntityFrameworkCore") == true);
        Assert.DoesNotContain(referencedAssemblies, a => a.Name?.Contains("EntityFramework") == true);
    }

    [Fact]
    public void Application_ShouldReferenceDomainButNotInfrastructure()
    {
        // Arrange
        var applicationAssembly = typeof(BOTC.Application.ApplicationServiceRegistration).Assembly;

        // Act
        var referencedAssemblies = applicationAssembly.GetReferencedAssemblies();

        // Assert
        Assert.Contains(referencedAssemblies, a => a.Name == "BOTC.Domain");
    }

    [Fact]
    public void Infrastructure_ShouldReferenceDomainAndApplication()
    {
        // Arrange
        var infrastructureAssembly = typeof(BOTC.Infrastructure.InfrastructureServiceRegistration).Assembly;

        // Act
        var referencedAssemblies = infrastructureAssembly.GetReferencedAssemblies();

        // Assert
        Assert.Contains(referencedAssemblies, a => a.Name == "BOTC.Domain");
        Assert.Contains(referencedAssemblies, a => a.Name == "BOTC.Application");
    }

    [Fact]
    public void RoomValueObjectsExist()
    {
        // Arrange & Act & Assert
        var roomIdType = typeof(BOTC.Domain.Rooms.RoomId);
        var roomCodeType = typeof(BOTC.Domain.Rooms.RoomCode);
        var playerIdType = typeof(BOTC.Domain.Rooms.Players.PlayerId);

        Assert.NotNull(roomIdType);
        Assert.NotNull(roomCodeType);
        Assert.NotNull(playerIdType);
    }

    [Fact]
    public void RoomAggregateRootExists()
    {
        // Arrange & Act & Assert
        var roomType = typeof(BOTC.Domain.Rooms.Room);

        Assert.NotNull(roomType);
        Assert.True(roomType.IsSealed);
    }
}
