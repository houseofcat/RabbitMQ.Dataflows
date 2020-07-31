using System.Threading.Tasks;

namespace RabbitMQ.Core.Prototype
{
    internal static class TaskEx
    {
        public static void Ignore(this Task task) { }
    }
}
