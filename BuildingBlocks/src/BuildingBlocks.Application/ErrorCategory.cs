namespace BuildingBlocks.Application;

/// <summary>
/// Classifies an <see cref="Error"/> by the kind of expected failure it represents.
/// </summary>
/// <remarks>
/// The category conveys failure semantics without coupling the application layer to any transport: the boundary (the
/// BFF for HTTP, the service host for gRPC) maps each category to a transport status code independently. There is
/// deliberately no <c>Unexpected</c> value &#8212; unexpected errors remain exceptions handled globally rather than becoming a
/// <see cref="Result"/> (see ADR-0017).
/// </remarks>
public enum ErrorCategory
{
	/// <summary>
	/// A structural validation failure, translated from a <c>DomainValidationException</c>.
	/// </summary>
	Validation,

	/// <summary>
	/// A broken domain invariant, translated from a <c>BusinessRuleViolationException</c>.
	/// </summary>
	BusinessRule,

	/// <summary>
	/// A requested aggregate or resource could not be found; returned directly by a handler.
	/// </summary>
	NotFound,

	/// <summary>
	/// The operation conflicts with the current state, such as an already-existing item or a concurrency clash; returned directly by a handler.
	/// </summary>
	Conflict,
}
