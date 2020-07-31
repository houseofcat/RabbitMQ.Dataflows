using CookedRabbit.Core.Configs;
using System.Threading.Channels;

namespace CookedRabbit.Core
{
    /// <summary>
    /// Global overrides for your consumers.
    /// </summary>
    public class GlobalConsumerOptions
    {
        public bool? NoLocal { get; set; }
        public bool? Exclusive { get; set; }
        public ushort? BatchSize { get; set; } = 5;
        public bool? AutoAck { get; set; }
        public bool? UseTransientChannels { get; set; } = true;

        public string ErrorSuffix { get; set; }

        public BoundedChannelFullMode? BehaviorWhenFull { get; set; } = BoundedChannelFullMode.Wait;

        public ConsumerPipelineOptions GlobalConsumerPipelineSettings { get; set; }
    }
}
