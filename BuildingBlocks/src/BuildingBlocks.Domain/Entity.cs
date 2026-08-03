namespace BuildingBlocks.Domain;

public abstract class Entity<TKey> : EntityBase<TKey>
    where TKey : struct, IEntityKey
{
    protected Entity(TKey id)
    {
        if (id.IsEmpty)
        {
            throw new DomainValidationException("The id of an entity cannot be empty.");
        }

        Id = id;
    }

    public sealed override TKey Id { get; }
}
