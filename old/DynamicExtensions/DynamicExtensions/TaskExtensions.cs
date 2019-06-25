using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicExtensions
{
    public static class TaskExtensions
    {
        public static Task<IEnumerable<T>> Merge<T>(this IEnumerable<Task<IEnumerable<T>>> items) => Task.WhenAll(items).Map(kek => kek.SelectMany(i => i));

        public static Task<IEnumerable<T>> Distinct<T>(this Task<IEnumerable<T>> items) => items.Map(Enumerable.Distinct);

        public static Task<T[]> ToArray<T>(this Task<IEnumerable<T>> items) => items.Map(Enumerable.ToArray);

        public static async Task<TOut> Map<TIn, TOut>(this Task<TIn> item, Func<TIn, TOut> mapper) => mapper(await item.ConfigureAwait(false));
        public static async Task<TOut> Map<TIn, TOut>(this Task<TIn> item, Func<TIn, Task<TOut>> mapper) => await mapper(await item.ConfigureAwait(false)).ConfigureAwait(false);

        public static async Task Then<TIn>(this Task<TIn> item, Action<TIn> action) => action(await item.ConfigureAwait(false));
        public static async Task Then<TIn>(this Task<TIn> item, Func<TIn, Task> action) => await action(await item.ConfigureAwait(false)).ConfigureAwait(false);

        public static async Task Then(this Task item, Action action)
        {
            await item.ConfigureAwait(false);
            action();
        }
        public static async Task Then(this Task item, Func<Task> action)
        {
            await item.ConfigureAwait(false);
            await action().ConfigureAwait(false);
        }
        
        public static async Task<TOut> Then<TOut>(this Task item, Func<Task<TOut>> generator)
        {
            await item.ConfigureAwait(false);
            return await generator().ConfigureAwait(false);
        }

        public static async Task<TOut> Then<TOut>(this Task item, Func<TOut> generator)
        {
            await item.ConfigureAwait(false);
            return generator();
        }

        public static async Task Catch<TException>(this Task item, Action<TException> action) where TException : Exception
        {
            try
            {
                await item.ConfigureAwait(false);
            }
            catch (TException e)
            {
                action(e);
            }
        }

        public static async Task Catch<TException>(this Task item, Func<TException, Task> action) where TException : Exception
        {
            try
            {
                await item.ConfigureAwait(false);
            }
            catch (TException e)
            {
                await action(e).ConfigureAwait(false);
            }
        }

        public static Task Catch(this Task item, Action<Exception> action) => Catch<Exception>(item, action);
        
        public static Task Catch(this Task item, Func<Exception, Task> action) => Catch<Exception>(item, action);

        public static async Task Finally(this Task item, Action finalizer)
        {
            try
            {
                await item.ConfigureAwait(false);
            }
            finally
            {
                finalizer();
            }
        }

        public static async Task Finally(this Task item, Func<Task> finalizer)
        {
            try
            {
                await item.ConfigureAwait(false);
            }
            finally
            {
                await finalizer();
            }
        }

        public static Task<T> AsTask<T>(this T @object) => Task.FromResult(@object);
    }
}
