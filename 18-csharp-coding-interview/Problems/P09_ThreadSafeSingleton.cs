namespace Interview;

// #9 Thread-safe Singleton  [Intermediate]
// Problem : Implement a thread-safe singleton. Explain why the naive version breaks.
// Approach: Naive version: two threads can both see null and create two instances. Use Lazy<T> (simplest, thread-safe by default) or double-checked locking.
// Complexity: O(1).

// Recommended: Lazy<T>
public sealed class AppConfig
{
    private static readonly Lazy<AppConfig> _instance = new(() => new AppConfig());
    public static AppConfig Instance => _instance.Value;
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
    private AppConfig() { }   // nobody else can call new
}

// Double-checked locking (if they ask for the "classic" version)
public sealed class Logger
{
    private static volatile Logger? _instance;
    private static readonly object _lock = new();
    private Logger() { }

    public static Logger Instance
    {
        get
        {
            if (_instance is null)
            {
                lock (_lock)
                {
                    _instance ??= new Logger();
                }
            }
            return _instance;
        }
    }
}
