namespace BuildingBlocks.Application;

/// <summary>
/// Represents a command that expresses an intent to change state and yields no value on success.
/// </summary>
/// <remarks>
/// Implement this marker on request types whose only meaningful outcome is success or failure, such as delete, update,
/// or void operations. Each command is handled by exactly one <see cref="ICommandHandler{TCommand}"/> and is dispatched
/// through <see cref="ISender"/>, which returns a <see cref="Result"/>. For commands that must return a value (for
/// example the identifier of a newly created aggregate), use <see cref="ICommand{TResult}"/> instead.
/// </remarks>
public interface ICommand
{
}

/// <summary>
/// Represents a command that expresses an intent to change state and yields a value of type <typeparamref name="TResult"/> on success.
/// </summary>
/// <remarks>
/// Use this marker when a state-changing operation must return data to the caller, most commonly a <c>create</c> that
/// returns the strongly typed identifier of the new aggregate so the frontend can navigate to it. Each command is
/// handled by exactly one <see cref="ICommandHandler{TCommand, TResult}"/> and is dispatched through
/// <see cref="ISender"/>, which returns a <see cref="Result{TResult}"/>.
/// </remarks>
/// <typeparam name="TResult">The type of the value produced by the command on success.</typeparam>
public interface ICommand<TResult>
{
}
