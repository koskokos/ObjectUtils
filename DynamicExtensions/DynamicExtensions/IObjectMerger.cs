namespace Chameleon.Common.Merge
{
    public interface IObjectMerger
    {
        TOut Merge<T1, T2, TOut>(T1 obj1, T2 obj2)
            where T1 : class
            where T2 : class
            where TOut : class, T1, T2;
    }
}
