using loadit.shared.Result;
using Loadit.Stats;

namespace loadit.shared.Ipc
{
    public interface IServerCommunication
    {
        void CollectStats(InterprocessSnapshot[] samples);
        void WriteTestResult(SummarizedResult data);
        void LogInformation(string message);
        void LogDebug(string message);
        void LogError(string message);
        void LogWarning(string message);
        void LogCritical(string message);
    }
}