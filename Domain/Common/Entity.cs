namespace Domain.Common;

/// <summary>
/// Base type for entities with a single scalar identity. Equality is identity-based
/// (same <typeparamref name="TId"/> value), not structural — two entities with the
/// same Id are the same entity even if their other properties currently differ.
/// </summary>
/// <remarks>
/// Dapper never materializes a type derived from <see cref="Entity{TId}"/> directly
/// (see docs/DOMAIN_MODEL.md — "Dapper construction strategy"). The protected
/// constructor exists only for derived types' own <c>Create</c>/<c>Reconstitute</c>
/// factories to call.
/// </remarks>
public abstract class Entity<TId>(TId id) : IEquatable<Entity<TId>>
    where TId : notnull
{
    public TId Id { get; protected set; } = id;

    public bool Equals(Entity<TId>? other) =>
        other is not null && (ReferenceEquals(this, other) || Id.Equals(other.Id));

    public override bool Equals(object? obj) => Equals(obj as Entity<TId>);

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !(left == right);
}
