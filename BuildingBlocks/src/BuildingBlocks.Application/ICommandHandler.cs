namespace BuildingBlocks.Application;

/// <summary>
/// Handles a command of type <typeparamref name="TCommand"/> that yields no value on success.
/// </summary>
/// <remarks>
/// Implement one handler per command type; it contains the application logic that fulfils the command's intent and
/// returns a <see cref="Result"/> describing success or an expected failure such as <see cref="ErrorCategory.NotFound"/>
/// or <see cref="ErrorCategory.Conflict"/>. Handlers are asynchronous and cooperatively cancellable; there are no
/// synchronous overloads. Expected domain-exception failures are translated to a failed result by a pipeline behavior
/// rather than being caught here.
/// </remarks>
/// <typeparam name="TCommand">The type of command handled by this handler.</typeparam>
public interface ICommandHandler<in TCommand>
	where TCommand : ICommand
{
	/// <summary>
	/// Handles the specified command.
	/// </summary>
	/// <param name="command">The command to handle.</param>
	/// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
	/// <returns>A task whose result is a <see cref="Result"/> indicating success or an expected failure.</returns>
	Task<Result> Handle(TCommand command, CancellationToken cancellationToken);
}

/// <summary>
/// Handles a command of type <typeparamref name="TCommand"/> that yields a value of type <typeparamref name="TResult"/> on success.
/// </summary>
/// <remarks>
/// Implement one handler per command type; it contains the application logic that fulfils the command's intent and
/// returns a <see cref="Result{TResult}"/> carrying the produced value on success or an expected failure otherwise.
/// Handlers are asynchronous and cooperatively cancellable; there are no synchronous overloads.
/// </remarks>
/// <typeparam name="TCommand">The type of command handled by this handler.</typeparam>
/// <typeparam name="TResult">The type of the value produced by the command on success.</typeparam>
public interface ICommandHandler<in TCommand, TResult>
	where TCommand : ICommand<TResult>
{
	/// <summary>
	/// Handles the specified command.
	/// </summary>
	/// <param name="command">The command to handle.</param>
	/// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
	/// <returns>A task whose result is a <see cref="Result{TResult}"/> carrying the produced value on success, or an expected failure otherwise.</returns>
	Task<Result<TResult>> Handle(TCommand command, CancellationToken cancellationToken);
}
