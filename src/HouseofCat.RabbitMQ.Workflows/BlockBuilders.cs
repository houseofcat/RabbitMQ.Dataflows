using HouseofCat.RabbitMQ.Pipelines;
using System;
using System.Net.NetworkInformation;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using static HouseofCat.Compression.Enums;
using static HouseofCat.Encryption.Enums;

namespace HouseofCat.RabbitMQ.Workflows
{
    public static class BlockBuilders
    {
        public static TransformBlock<TState, TState> GetTransformBlockAsync<TState>(Func<TState, Task<TState>> action, ExecutionDataflowBlockOptions options) where TState : class, IWorkState, new()
        {
            return new TransformBlock<TState, TState>(action, options);
        }

        public static TransformBlock<TState, TState> GetTransformBlockAsync<TState>(Func<TState, TState> action, ExecutionDataflowBlockOptions options) where TState : class, IWorkState, new()
        {
            return new TransformBlock<TState, TState>(action, options);
        }

        public static TransformBlock<ReceivedData, TState> GetStateTransformBlock<TState>(Func<ReceivedData, TState> action, ExecutionDataflowBlockOptions options) where TState : class, IWorkState, new()
        {
            return new TransformBlock<ReceivedData, TState>(
                (data) =>
                {
                    try
                    { return action(data); }
                    catch
                    { return null; }
                }, options);
        }

        public static TransformBlock<ReceivedData, TState> GetStateTransformBlock<TState>(Func<ReceivedData, Task<TState>> action, ExecutionDataflowBlockOptions options) where TState : class, IWorkState, new()
        {
            return new TransformBlock<ReceivedData, TState>(
                async (data) =>
                {
                    try
                    { return await action(data); }
                    catch
                    { return null; }
                }, options);
        }

        public static TransformBlock<TState, TState> GetByteManipulationTransformBlock<TState>(Func<ReadOnlyMemory<byte>, byte[]> action, ExecutionDataflowBlockOptions options, bool sending) where TState : class, IWorkState, new()
        {
            return new TransformBlock<TState, TState>(
                (state) =>
                {
                    try
                    {
                        if (sending)
                        {
                            if (state.SendData?.Length > 0)
                            { state.SendData = action(state.SendData); }
                            else if (state.SendLetter.Body?.Length > 0)
                            { state.SendLetter.Body = action(state.SendLetter.Body); }
                        }
                        else
                        {
                            state.ReceivedData.Data = action(state.ReceivedData.Data);
                        }
                        return state;
                    }
                    catch (Exception ex)
                    {
                        state.IsFaulted = true;
                        state.EDI = ExceptionDispatchInfo.Capture(ex);
                        return state;
                    }
                }, options);
        }

        public static TransformBlock<TState, TState> GetByteManipulationTransformBlock<TState>(Func<ReadOnlyMemory<byte>, Task<byte[]>> action, ExecutionDataflowBlockOptions options, bool sending) where TState : class, IWorkState, new()
        {
            return new TransformBlock<TState, TState>(
                async (state) =>
                {
                    try
                    {
                        state.ReceivedData.Data = await action(state.ReceivedData.Data);
                        return state; 
                    }
                    catch (Exception ex)
                    {
                        state.IsFaulted = true;
                        state.EDI = ExceptionDispatchInfo.Capture(ex);
                        return state;
                    }
                }, options);
        }

        public static TransformBlock<TState, TState> GetWrappedTransformBlock<TState>(Func<TState, TState> action, ExecutionDataflowBlockOptions options) where TState : class, IWorkState, new()
        {
            return new TransformBlock<TState, TState>(
                (state) =>
                {
                    try
                    {
                        return action(state);
                    }
                    catch (Exception ex)
                    {
                        state.IsFaulted = true;
                        state.EDI = ExceptionDispatchInfo.Capture(ex);
                        return state;
                    }
                }, options);
        }

        public static TransformBlock<TState, TState> GetWrappedTransformBlock<TState>(Func<TState, Task<TState>> action, ExecutionDataflowBlockOptions options) where TState : class, IWorkState, new()
        {
            return new TransformBlock<TState, TState>(
                async (state) =>
                {
                    try
                    {
                        return await action(state);
                    }
                    catch (Exception ex)
                    {
                        state.IsFaulted = true;
                        state.EDI = ExceptionDispatchInfo.Capture(ex);
                        return state;
                    }
                }, options);
        }

        public static ActionBlock<TState> GetWrappedActionBlock<TState>(Action<TState> action, ExecutionDataflowBlockOptions options)
        {
            return new ActionBlock<TState>(
                (state) =>
                {
                    try
                    { action(state); }
                    catch
                    { }
                }, options);
        }

        public static ActionBlock<TState> GetWrappedActionBlock<TState>(Func<TState, TState> action, ExecutionDataflowBlockOptions options)
        {
            return new ActionBlock<TState>(
                (state) =>
                {
                    try
                    { action(state); }
                    catch
                    { }
                }, options);
        }
    }
}
