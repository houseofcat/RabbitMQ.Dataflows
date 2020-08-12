using HouseofCat.RabbitMQ.Pipelines;
using HouseofCat.RabbitMQ.Services;
using HouseofCat.Serialization;
using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using static HouseofCat.Reflection.Generics;

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

        public static TState BuildState<TState, TOut>(ReceivedData data, ISerializationProvider provider) where TState : class, IWorkState, new()
        {
            var state = New<TState>.Instance.Invoke();
            state.ReceivedData = data;
            state.Data = new Dictionary<string, object>
            {
                { "Item", provider.Deserialize<TOut>(data.Data) },
            };

            return state;
        }

        public static TransformBlock<ReceivedData, TState> GetBuildStateBlock<TState, TOut>(ISerializationProvider provider, ExecutionDataflowBlockOptions options) where TState : class, IWorkState, new()
        {
            return new TransformBlock<ReceivedData, TState>(
                (data) =>
                {
                    try
                    { return BuildState<TState, TOut>(data, provider); }
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
                    { return await action(data).ConfigureAwait(false); }
                    catch
                    { return null; }
                }, options);
        }

        public static TransformBlock<TState, TState> GetByteManipulationTransformBlock<TState>(
            Func<ReadOnlyMemory<byte>, byte[]> action,
            ISerializationProvider serializationProvider,
            ExecutionDataflowBlockOptions options,
            bool outbound,
            Predicate<TState> predicate) where TState : class, IWorkState, new()
        {
            return new TransformBlock<TState, TState>(
                (state) =>
                {
                    try
                    {
                        if (outbound)
                        {
                            if (state.SendData?.Length > 0)
                            { state.SendData = action(state.SendData); }
                            else if (state.SendLetter.Body?.Length > 0)
                            { state.SendLetter.Body = action(state.SendLetter.Body); }
                        }
                        else if (predicate(state))
                        {
                            if (state.ReceivedData.ContentType == Constants.HeaderValueForLetter)
                            {
                                if (state.ReceivedData.Letter == null)
                                { state.ReceivedData.Letter = serializationProvider.Deserialize<Letter>(state.ReceivedData.Data); }

                                state.ReceivedData.Letter.Body = action(state.ReceivedData.Letter.Body);
                            }
                            else
                            { state.ReceivedData.Data = action(state.ReceivedData.Data); }
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

        public static TransformBlock<TState, TState> GetByteManipulationTransformBlock<TState>(Func<ReadOnlyMemory<byte>, Task<byte[]>> action, ISerializationProvider serializationProvider, ExecutionDataflowBlockOptions options, bool outbound, Predicate<TState> predicate) where TState : class, IWorkState, new()
        {
            return new TransformBlock<TState, TState>(
                async (state) =>
                {
                    try
                    {
                        if (outbound)
                        {
                            if (state.SendData?.Length > 0)
                            { state.SendData = await action(state.SendData).ConfigureAwait(false); }
                            else if (state.SendLetter.Body?.Length > 0)
                            { state.SendLetter.Body = await action(state.SendLetter.Body).ConfigureAwait(false); }
                        }
                        else if (predicate(state))
                        {
                            if (state.ReceivedData.ContentType == Constants.HeaderValueForLetter)
                            {
                                if (state.ReceivedData.Letter == null)
                                { state.ReceivedData.Letter = serializationProvider.Deserialize<Letter>(state.ReceivedData.Data); }

                                state.ReceivedData.Letter.Body = await action(state.ReceivedData.Letter.Body).ConfigureAwait(false);
                            }
                            else
                            { state.ReceivedData.Data = await action(state.ReceivedData.Data).ConfigureAwait(false); }
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
                        return await action(state).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        state.IsFaulted = true;
                        state.EDI = ExceptionDispatchInfo.Capture(ex);
                        return state;
                    }
                }, options);
        }

        public static TransformBlock<TState, TState> GetWrappedPublishTransformBlock<TState>(IRabbitService service, ExecutionDataflowBlockOptions options) where TState : class, IWorkState, new()
        {
            return new TransformBlock<TState, TState>(
                async (state) =>
                {
                    try
                    {
                        await service.Publisher.PublishAsync(state.SendLetter, true, true).ConfigureAwait(false);
                        state.SendLetterSent = true;
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

        public static ActionBlock<TState> GetWrappedActionBlock<TState>(Func<TState, Task> action, ExecutionDataflowBlockOptions options)
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
