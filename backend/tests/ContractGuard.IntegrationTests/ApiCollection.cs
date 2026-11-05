namespace ContractGuard.IntegrationTests;

// Every test class marked [Collection(ApiCollection.Name)] shares one ApiFactory,
// so the whole run starts a single PostgreSQL container.
[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}
