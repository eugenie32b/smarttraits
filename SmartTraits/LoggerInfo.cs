namespace SmartTraits
{
    public class LoggerInfo
    {
        public T4GeneratorVerbosity Verbosity { get; set; }

        public string Id { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }

        public LoggerInfo(T4GeneratorVerbosity verbosity, string id, string title, string message)
        {
            Verbosity = verbosity;
            Id = id;
            Title = title;
            Message = message;
        }
    }

    public enum T4GeneratorVerbosity
    {
        None = 0,
        Critical,
        Error,
        Warning,
        Info,
        Debug
    }
}
