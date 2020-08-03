using HouseofCat.RabbitMQ.Pipelines;
using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace HouseofCat.RabbitMQ.Workflows
{
    public static class BlockBuilders
    {
        public static TransformBlock<TState, TState> GetTransformBlockAsync<TState>(Func<TState, Task<TState>> action, ExecutionDataflowBlockOptions options) where TState : IWorkState
        {
            return new TransformBlock<TState, TState>(action, options);
        }

        public static TransformBlock<TState, TState> GetTransformBlockAsync<TState>(Func<TState, TState> action, ExecutionDataflowBlockOptions options) where TState : IWorkState
        {
            return new TransformBlock<TState, TState>(action, options);
        }

        public static TransformBlock<TState, TState> GetWrappedTransformBlockAsync<TState>(Func<TState, Task<TState>> action, ExecutionDataflowBlockOptions options) where TState : IWorkState
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

        public static TransformBlock<TState, TState> GetWrappedTransformBlockAsync<TState>(Func<TState, TState> action, ExecutionDataflowBlockOptions options) where TState : IWorkState
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
    }
}
