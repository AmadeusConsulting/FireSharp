namespace FireSharp.Logging
{
    public interface ILogManager
    {
        ILog GetLogger<T>();

        ILog GetLogger<T>(T clazz) where T : class;

        ILog GetLogger(string name);
    }
}