namespace DynamicExtensions
{
    public interface IObjectMerger
    {
        TOut Merge<T1, T2, TOut>(T1 obj1, T2 obj2)
            where T1 : class
            where T2 : class
            where TOut : class, T1, T2;

        TOut JoinById<TId, T1, T2, TOut>(T1 obj1, T2 obj2)
            where T1 : class, TId
            where T2 : class, TId
            where TOut : class, T1, T2;
    }
}
