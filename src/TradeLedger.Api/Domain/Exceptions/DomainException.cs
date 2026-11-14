namespace TradeLedger.Api.Domain.Exceptions;

public abstract class DomainException(string message) : Exception(message);

public sealed class EntityNotFoundException(string entityName, object key)
    : DomainException($"{entityName} '{key}' was not found.");

public sealed class InvalidOrderRequestException(string message) : DomainException(message);

public sealed class InsufficientFundsException(decimal required, decimal available)
    : DomainException($"Order requires {required:C} but only {available:C} of buying power is available.");

public sealed class InsufficientHoldingsException(string symbol, int required, int available)
    : DomainException($"Selling {required} shares of {symbol} requires {required} available but only {available} are held.");

public sealed class InvalidOrderStateException(int orderId, string currentStatus)
    : DomainException($"Order {orderId} cannot be cancelled because it is already '{currentStatus}'.");

public sealed class ConcurrencyRetryExhaustedException(string operation)
    : DomainException($"Could not complete '{operation}' after multiple attempts due to concurrent updates.");
