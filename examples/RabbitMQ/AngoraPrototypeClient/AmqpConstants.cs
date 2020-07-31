namespace RabbitMQ.Core.Prototype
{
    internal static class AmqpConstants
    {
        public const uint FrameHeaderSize = 7;
        public const uint TotalFrameOverhead = FrameHeaderSize + sizeof(byte);
        public const byte FrameEnd = 0xCE;
        public const byte Reserved = 0x00;

        public static class FrameType
        {
            public const byte Method = 1;
            public const byte ContentHeader = 2;
            public const byte ContentBody = 3;
            public const byte Heartbeat = 8;
        }

        public static class ConnectionReplyCode
        {
            public const ushort Success = 200;
            public const ushort ConnectionForced = 320;
            public const ushort InvalidPath = 402;
            public const ushort FrameError = 501;
            public const ushort SyntaxError = 502;
            public const ushort CommandInvalid = 503;
            public const ushort ChannelError = 504;
            public const ushort UnexpectedFrame = 505;
            public const ushort ResourceError = 506;
            public const ushort NotAllowed = 530;
            public const ushort NotImplemented = 540;
            public const ushort InternalError = 541;
        }

        public static class ChannelReplyCode
        {
            public const ushort Success = 200;
            public const ushort ContentTooLarge = 311;
            public const ushort NoConsumers = 313;
            public const ushort AccessRefused = 403;
            public const ushort NotFound = 404;
            public const ushort ResourceLocked = 405;
            public const ushort PreconditionFailed = 406;
        }

        public static class ClassId
        {
            public const ushort Connection = 10;
            public const ushort Channel = 20;
            public const ushort Exchange = 40;
            public const ushort Queue = 50;
            public const ushort Basic = 60;
        }

        public static class MethodId
        {
            public static class Connection
            {
                public const ushort Start = 10;
                public const ushort StartOk = 11;
                public const ushort Secure = 20;
                public const ushort SecureOk = 21;
                public const ushort Tune = 30;
                public const ushort TuneOk = 31;
                public const ushort Open = 40;
                public const ushort OpenOk = 41;
                public const ushort Close = 50;
                public const ushort CloseOk = 51;
            }

            public static class Channel
            {
                public const ushort Open = 10;
                public const ushort OpenOk = 11;
                public const ushort Flow = 20;
                public const ushort FlowOk = 21;
                public const ushort Close = 40;
                public const ushort CloseOk = 41;
            }

            public static class Exchange
            {
                public const ushort Declare = 10;
                public const ushort DeclareOk = 11;
                public const ushort Delete = 20;
                public const ushort DeleteOk = 21;
                public const ushort Bind = 30;
                public const ushort BindOk = 31;
                public const ushort Unbind = 40;
                public const ushort UnbindOk = 51;
            }

            public static class Queue
            {
                public const ushort Declare = 10;
                public const ushort DeclareOk = 11;
                public const ushort Bind = 20;
                public const ushort BindOk = 21;
                public const ushort Purge = 30;
                public const ushort PurgeOk = 31;
                public const ushort Delete = 40;
                public const ushort DeleteOk = 41;
                public const ushort Unbind = 50;
                public const ushort UnbindOk = 51;
            }

            public static class Basic
            {
                public const ushort Qos = 10;
                public const ushort QosOk = 11;
                public const ushort Consume = 20;
                public const ushort ConsumeOk = 21;
                public const ushort Cancel = 30;
                public const ushort CancelOk = 31;
                public const ushort Publish = 40;
                public const ushort Return = 50;
                public const ushort Deliver = 60;
                public const ushort Get = 70;
                public const ushort GetOk = 71;
                public const ushort GetEmpty = 72;
                public const ushort Ack = 80;
                public const ushort Reject = 90;
                public const ushort RecoverAsync = 100;
                public const ushort Recover = 110;
                public const ushort RecoverOk = 111;
                public const ushort Nack = 120;
            }
        }

        public static class Method
        {
            public static class Connection
            {
                public const uint Start = ClassId.Connection << 16 | MethodId.Connection.Start;
                public const uint StartOk = ClassId.Connection << 16 | MethodId.Connection.StartOk;
                public const uint Secure = ClassId.Connection << 16 | MethodId.Connection.Secure;
                public const uint SecureOk = ClassId.Connection << 16 | MethodId.Connection.SecureOk;
                public const uint Tune = ClassId.Connection << 16 | MethodId.Connection.Tune;
                public const uint TuneOk = ClassId.Connection << 16 | MethodId.Connection.TuneOk;
                public const uint Open = ClassId.Connection << 16 | MethodId.Connection.Open;
                public const uint OpenOk = ClassId.Connection << 16 | MethodId.Connection.OpenOk;
                public const uint Close = ClassId.Connection << 16 | MethodId.Connection.Close;
                public const uint CloseOk = ClassId.Connection << 16 | MethodId.Connection.CloseOk;
            }

            public static class Channel
            {
                public const uint Open = ClassId.Channel << 16 | MethodId.Channel.Open;
                public const uint OpenOk = ClassId.Channel << 16 | MethodId.Channel.OpenOk;
                public const uint Flow = ClassId.Channel << 16 | MethodId.Channel.Flow;
                public const uint FlowOk = ClassId.Channel << 16 | MethodId.Channel.FlowOk;
                public const uint Close = ClassId.Channel << 16 | MethodId.Channel.Close;
                public const uint CloseOk = ClassId.Channel << 16 | MethodId.Channel.CloseOk;
            }

            public static class Exchange
            {
                public const uint Declare = ClassId.Exchange << 16 | MethodId.Exchange.Declare;
                public const uint DeclareOk = ClassId.Exchange << 16 | MethodId.Exchange.DeclareOk;
                public const uint Delete = ClassId.Exchange << 16 | MethodId.Exchange.Delete;
                public const uint DeleteOk = ClassId.Exchange << 16 | MethodId.Exchange.DeleteOk;
                public const uint Bind = ClassId.Exchange << 16 | MethodId.Exchange.Bind;
                public const uint BindOk = ClassId.Exchange << 16 | MethodId.Exchange.BindOk;
                public const uint Unbind = ClassId.Exchange << 16 | MethodId.Exchange.Unbind;
                public const uint UnbindOk = ClassId.Exchange << 16 | MethodId.Exchange.UnbindOk;
            }

            public static class Queue
            {
                public const uint Declare = ClassId.Queue << 16 | MethodId.Queue.Declare;
                public const uint DeclareOk = ClassId.Queue << 16 | MethodId.Queue.DeclareOk;
                public const uint Bind = ClassId.Queue << 16 | MethodId.Queue.Bind;
                public const uint BindOk = ClassId.Queue << 16 | MethodId.Queue.BindOk;
                public const uint Purge = ClassId.Queue << 16 | MethodId.Queue.Purge;
                public const uint PurgeOk = ClassId.Queue << 16 | MethodId.Queue.PurgeOk;
                public const uint Delete = ClassId.Queue << 16 | MethodId.Queue.Delete;
                public const uint DeleteOk = ClassId.Queue << 16 | MethodId.Queue.DeleteOk;
                public const uint Unbind = ClassId.Queue << 16 | MethodId.Queue.Unbind;
                public const uint UnbindOk = ClassId.Queue << 16 | MethodId.Queue.UnbindOk;
            }

            public static class Basic
            {
                public const uint Qos = ClassId.Basic << 16 | MethodId.Basic.Qos;
                public const uint QosOk = ClassId.Basic << 16 | MethodId.Basic.QosOk;
                public const uint Consume = ClassId.Basic << 16 | MethodId.Basic.Consume;
                public const uint ConsumeOk = ClassId.Basic << 16 | MethodId.Basic.ConsumeOk;
                public const uint Cancel = ClassId.Basic << 16 | MethodId.Basic.Cancel;
                public const uint CancelOk = ClassId.Basic << 16 | MethodId.Basic.CancelOk;
                public const uint Publish = ClassId.Basic << 16 | MethodId.Basic.Publish;
                public const uint Return = ClassId.Basic << 16 | MethodId.Basic.Return;
                public const uint Deliver = ClassId.Basic << 16 | MethodId.Basic.Deliver;
                public const uint Get = ClassId.Basic << 16 | MethodId.Basic.Get;
                public const uint GetOk = ClassId.Basic << 16 | MethodId.Basic.GetOk;
                public const uint GetEmpty = ClassId.Basic << 16 | MethodId.Basic.GetEmpty;
                public const uint Ack = ClassId.Basic << 16 | MethodId.Basic.Ack;
                public const uint Reject = ClassId.Basic << 16 | MethodId.Basic.Reject;
                public const uint RecoverAsync = ClassId.Basic << 16 | MethodId.Basic.RecoverAsync;
                public const uint Recover = ClassId.Basic << 16 | MethodId.Basic.Recover;
                public const uint RecoverOk = ClassId.Basic << 16 | MethodId.Basic.RecoverOk;
                public const uint Nack = ClassId.Basic << 16 | MethodId.Basic.Nack;
            }
        }
    }
}
