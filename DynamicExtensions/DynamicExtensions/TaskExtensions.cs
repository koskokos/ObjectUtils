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

        public static async Task<TOut> Map<TIn, TOut>(this Task<TIn> item, Func<TIn, TOut> mapper) => mapper(await item);
        public static async Task<TOut> Map<TIn, TOut>(this Task<TIn> item, Func<TIn, Task<TOut>> mapper) => await mapper(await item);

        public static async Task Then<TIn>(this Task<TIn> item, Action<TIn> action) => action(await item);
        public static async Task Then<TIn>(this Task<TIn> item, Func<TIn, Task> action) => await action(await item);

        public static Task<T> AsTask<T>(this T @object) => Task.FromResult(@object);
    }
}
