using BOTC.Application;
using BOTC.Domain;
using BOTC.Domain.Events;
using BOTC.Domain.Rooms;
using BOTC.Domain.Users;
using BOTC.Infrastructure;
using BOTC.Presentation.Api.Rooms;
using System.Reflection;
using Xunit;

namespace BOTC.Architecture.Tests;

public sealed class ArchitectureTests
{
    // Assembly anchors — one well-known public type per layer
    private static readonly Assembly DomainAssembly          = typeof(Room).Assembly;
    private static readonly Assembly ApplicationAssembly     = typeof(ApplicationServiceRegistration).Assembly;
    private static readonly Assembly InfrastructureAssembly  = typeof(InfrastructureServiceRegistration).Assembly;
    private static readonly Assembly PresentationApiAssembly = typeof(RoomsEndpoints).Assembly;

    // Contracts is not directly referenced by this test project but is a transitive
    // dependency of Presentation.Api, so its DLL is present in the output directory.
    private static readonly Assembly ContractsAssembly = Assembly.Load("BOTC.Contracts");

    // -------------------------------------------------------------------------
    // Domain layer — pure core, must not depend on any outer layer
    // -------------------------------------------------------------------------

    [Fact]
    public void Domain_MustNotReferenceApplicationLayer()
    {
        var refs = DomainAssembly.GetReferencedAssemblies();

        Assert.DoesNotContain(refs, a => a.Name == "BOTC.Application");
    }

    [Fact]
    public void Domain_MustNotReferenceInfrastructureLayer()
    {
        var refs = DomainAssembly.GetReferencedAssemblies();

        Assert.DoesNotContain(refs, a => a.Name == "BOTC.Infrastructure");
    }

    [Fact]
    public void Domain_MustNotReferencePresentationApiLayer()
    {
        var refs = DomainAssembly.GetReferencedAssemblies();

        Assert.DoesNotContain(refs, a => a.Name == "BOTC.Presentation.Api");
    }

    [Fact]
    public void Domain_MustNotReferenceContractsLayer()
    {
        var refs = DomainAssembly.GetReferencedAssemblies();

        Assert.DoesNotContain(refs, a => a.Name == "BOTC.Contracts");
    }

    [Fact]
    public void Domain_MustNotDependOnEntityFramework()
    {
        var refs = DomainAssembly.GetReferencedAssemblies();

        Assert.DoesNotContain(refs, a => a.Name?.Contains("EntityFramework") == true);
    }

    // -------------------------------------------------------------------------
    // Application layer — orchestrates domain; defines abstractions for infra
    // -------------------------------------------------------------------------

    [Fact]
    public void Application_MustReferenceDomainLayer()
    {
        var refs = ApplicationAssembly.GetReferencedAssemblies();

        Assert.Contains(refs, a => a.Name == "BOTC.Domain");
    }

    [Fact]
    public void Application_MustNotReferenceInfrastructureLayer()
    {
        var refs = ApplicationAssembly.GetReferencedAssemblies();

        Assert.DoesNotContain(refs, a => a.Name == "BOTC.Infrastructure");
    }

    [Fact]
    public void Application_MustNotReferencePresentationApiLayer()
    {
        var refs = ApplicationAssembly.GetReferencedAssemblies();

        Assert.DoesNotContain(refs, a => a.Name == "BOTC.Presentation.Api");
    }

    [Fact]
    public void Application_MustNotReferencePresentationDesktopLayer()
    {
        var refs = ApplicationAssembly.GetReferencedAssemblies();

        Assert.DoesNotContain(refs, a => a.Name == "BOTC.Presentation.Desktop");
    }

    [Fact]
    public void Application_MustNotDependOnEntityFramework()
    {
        var refs = ApplicationAssembly.GetReferencedAssemblies();

        Assert.DoesNotContain(refs, a => a.Name?.Contains("EntityFramework") == true);
    }

    // -------------------------------------------------------------------------
    // Infrastructure layer — implements Application abstractions; may use EF Core
    // -------------------------------------------------------------------------

    [Fact]
    public void Infrastructure_MustReferenceDomainLayer()
    {
        var refs = InfrastructureAssembly.GetReferencedAssemblies();

        Assert.Contains(refs, a => a.Name == "BOTC.Domain");
    }

    [Fact]
    public void Infrastructure_MustReferenceApplicationLayer()
    {
        var refs = InfrastructureAssembly.GetReferencedAssemblies();

        Assert.Contains(refs, a => a.Name == "BOTC.Application");
    }

    [Fact]
    public void Infrastructure_MustNotReferencePresentationApiLayer()
    {
        var refs = InfrastructureAssembly.GetReferencedAssemblies();

        Assert.DoesNotContain(refs, a => a.Name == "BOTC.Presentation.Api");
    }

    [Fact]
    public void Infrastructure_MustNotReferencePresentationDesktopLayer()
    {
        var refs = InfrastructureAssembly.GetReferencedAssemblies();

        Assert.DoesNotContain(refs, a => a.Name == "BOTC.Presentation.Desktop");
    }

    // -------------------------------------------------------------------------
    // Contracts layer — shared DTO surface; must not depend on any BOTC layer
    // -------------------------------------------------------------------------

    [Fact]
    public void Contracts_MustNotReferenceDomainLayer()
    {
        var refs = ContractsAssembly.GetReferencedAssemblies();

        Assert.DoesNotContain(refs, a => a.Name == "BOTC.Domain");
    }

    [Fact]
    public void Contracts_MustNotReferenceApplicationLayer()
    {
        var refs = ContractsAssembly.GetReferencedAssemblies();

        Assert.DoesNotContain(refs, a => a.Name == "BOTC.Application");
    }

    [Fact]
    public void Contracts_MustNotReferenceInfrastructureLayer()
    {
        var refs = ContractsAssembly.GetReferencedAssemblies();

        Assert.DoesNotContain(refs, a => a.Name == "BOTC.Infrastructure");
    }

    // -------------------------------------------------------------------------
    // Presentation.Api layer — outermost server entry point
    // -------------------------------------------------------------------------

    [Fact]
    public void PresentationApi_MustReferenceApplicationLayer()
    {
        var refs = PresentationApiAssembly.GetReferencedAssemblies();

        Assert.Contains(refs, a => a.Name == "BOTC.Application");
    }

    [Fact]
    public void PresentationApi_MustReferenceContractsLayer()
    {
        var refs = PresentationApiAssembly.GetReferencedAssemblies();

        Assert.Contains(refs, a => a.Name == "BOTC.Contracts");
    }

    [Fact]
    public void PresentationApi_MustNotBeReferencedByDomainLayer()
    {
        var refs = DomainAssembly.GetReferencedAssemblies();

        Assert.DoesNotContain(refs, a => a.Name == "BOTC.Presentation.Api");
    }

    [Fact]
    public void PresentationApi_MustNotBeReferencedByApplicationLayer()
    {
        var refs = ApplicationAssembly.GetReferencedAssemblies();

        Assert.DoesNotContain(refs, a => a.Name == "BOTC.Presentation.Api");
    }

    [Fact]
    public void PresentationApi_MustNotBeReferencedByInfrastructureLayer()
    {
        var refs = InfrastructureAssembly.GetReferencedAssemblies();

        Assert.DoesNotContain(refs, a => a.Name == "BOTC.Presentation.Api");
    }

    // -------------------------------------------------------------------------
    // Domain model characteristics — aggregates, entities, value objects
    // -------------------------------------------------------------------------

    [Fact]
    public void AggregateRoot_BaseClass_MustBeAbstract()
    {
        Assert.True(typeof(AggregateRoot).IsAbstract);
    }

    [Fact]
    public void DomainEvent_BaseType_MustBeAbstract()
    {
        Assert.True(typeof(DomainEvent).IsAbstract);
    }

    [Fact]
    public void AllConcreteAggregateRoots_MustBeSealed()
    {
        var nonSealedAggregates = DomainAssembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                        && typeof(AggregateRoot).IsAssignableFrom(t)
                        && !t.IsSealed)
            .Select(t => t.FullName)
            .ToList();

        Assert.Empty(nonSealedAggregates);
    }

    [Fact]
    public void Room_AggregateRoot_MustInheritFromAggregateRoot()
    {
        Assert.True(typeof(AggregateRoot).IsAssignableFrom(typeof(Room)));
    }

    [Fact]
    public void Room_Properties_MustNotExposePublicSetters()
    {
        var publicSetters = typeof(Room)
            .GetProperties()
            .Where(p => p.SetMethod?.IsPublic == true)
            .Select(p => p.Name)
            .ToList();

        Assert.Empty(publicSetters);
    }

    [Fact]
    public void Player_Entity_MustBeSealed()
    {
        Assert.True(typeof(Player).IsSealed);
    }

    [Fact]
    public void Player_Properties_MustNotExposePublicSetters()
    {
        var publicSetters = typeof(Player)
            .GetProperties()
            .Where(p => p.SetMethod?.IsPublic == true)
            .Select(p => p.Name)
            .ToList();

        Assert.Empty(publicSetters);
    }

    [Fact]
    public void User_Entity_MustBeSealed()
    {
        Assert.True(typeof(User).IsSealed);
    }

    [Fact]
    public void RoomId_ValueObject_MustBeAStruct()
    {
        Assert.True(typeof(RoomId).IsValueType);
    }

    [Fact]
    public void RoomCode_ValueObject_MustBeAStruct()
    {
        Assert.True(typeof(RoomCode).IsValueType);
    }

    [Fact]
    public void PlayerId_ValueObject_MustBeSealed()
    {
        Assert.True(typeof(PlayerId).IsSealed);
    }

    [Fact]
    public void UserId_ValueObject_MustBeSealed()
    {
        Assert.True(typeof(UserId).IsSealed);
    }
}
