namespace BuildingBlocks.Domain;

public abstract class Entity<TKey> : IEntity<TKey>, IEquatable<Entity<TKey>>
    where TKey : struct, IEntityKey
{
    protected Entity(TKey id)
    {
        if (id.Value == Guid.Empty)
            throw new DomainValidationException("The id of an entity cannot be an empty GUID.");
        Id = id;
    }

    public TKey Id { get; }

    public bool Equals(Entity<TKey>? other)
    {
        return other is not null
               && other.GetType() == GetType()
               && Id.Equals(other.Id);
    }

    public sealed override bool Equals(object? obj)
    {
        return Equals(obj as Entity<TKey>);
    }

    public sealed override int GetHashCode()
    {
        return HashCode.Combine(GetType(), Id);
    }

    public static bool operator ==(Entity<TKey>? left, Entity<TKey>? right)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (left is null || right is null)
            return false;

        return left.Equals(right);
    }

    public static bool operator !=(Entity<TKey>? left, Entity<TKey>? right)
    {
        return !(left == right);
    }
}