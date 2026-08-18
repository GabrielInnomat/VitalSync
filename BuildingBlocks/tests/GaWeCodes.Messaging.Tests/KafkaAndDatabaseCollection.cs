using GaWeCodes.Tests;

namespace GaWeCodes.Messaging.Tests;

[CollectionDefinition(Name)]
public sealed class KafkaAndDatabaseCollection
    : ICollectionFixture<PostgreSqlFixture>, ICollectionFixture<KafkaFixture>
{
    public const string Name = "KafkaAndDatabase";
}
