using ContactHub.Application.Abstractions;
using ContactHub.Domain.Entities;
using ContactHub.Infrastructure.Persistence;
using NetArchTest.Rules;

namespace ContactHub.Api.Tests.Architecture;

[TestFixture]
public class ArchitectureTests
{
    private const string Domain = "ContactHub.Domain";
    private const string Application = "ContactHub.Application";
    private const string Infrastructure = "ContactHub.Infrastructure";
    private const string Api = "ContactHub.Api";
    private const string EfCore = "Microsoft.EntityFrameworkCore";

    private static readonly System.Reflection.Assembly DomainAssembly = typeof(Contact).Assembly;
    private static readonly System.Reflection.Assembly ApplicationAssembly = typeof(IAppDbContext).Assembly;
    private static readonly System.Reflection.Assembly InfrastructureAssembly = typeof(AppDbContext).Assembly;

    private static void AssertNoDependency(System.Reflection.Assembly assembly, params string[] forbidden)
    {
        var result = Types.InAssembly(assembly).ShouldNot().HaveDependencyOnAny(forbidden).GetResult();

        Assert.That(
            result.IsSuccessful,
            Is.True,
            string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Test]
    public void Domain_DoesNotDependOnOtherLayers() =>
        AssertNoDependency(DomainAssembly, Application, Infrastructure, Api);

    [Test]
    public void Domain_DoesNotDependOnEfCore() =>
        AssertNoDependency(DomainAssembly, EfCore);

    [Test]
    public void Application_DoesNotDependOnInfrastructureOrApi() =>
        AssertNoDependency(ApplicationAssembly, Infrastructure, Api);

    [Test]
    public void Infrastructure_DoesNotDependOnApi() =>
        AssertNoDependency(InfrastructureAssembly, Api);

    [Test]
    public void Domain_ReferencesNoProjects() =>
        Assert.That(
            DomainAssembly.GetReferencedAssemblies().Select(a => a.Name),
            Has.None.StartsWith("ContactHub").Or.EqualTo(Api));

    [Test]
    public void Application_ReferencesOnlyDomain() =>
        Assert.That(
            ApplicationAssembly.GetReferencedAssemblies().Select(a => a.Name),
            Has.None.EqualTo(Infrastructure).Or.EqualTo(Api));
}
