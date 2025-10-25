module Cart.Abstractions

open Microsoft.Extensions.Logging

/// <summary>
/// Provides access to the database connection string.
/// </summary>
type ICartDbEnv =
    abstract member ConnectionString: string

/// <summary>
/// Provides access to the application logger.
/// </summary>
type IAppLogger =
    abstract member Logger: ILogger