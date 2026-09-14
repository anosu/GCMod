// 缓存测试只隔离 Unity 日志和通知输出，HTTP、序列化、文件及哈希均使用实际实现。
namespace GCMod
{
    internal static class Logger
    {
        public static void Info(string message) { }

        public static void Warn(string message) { }

        public static void Error(string message) { }
    }
}

namespace Utility.Notifications
{
    internal static class Toast
    {
        public static readonly System.Collections.Concurrent.ConcurrentQueue<(
            string Title,
            string Message
        )> InfoMessages = new();

        public static void Info(string title, string message) =>
            InfoMessages.Enqueue((title, message));

        public static void Warning(string title, string message) { }

        public static void Error(string title, string message) { }
    }
}
