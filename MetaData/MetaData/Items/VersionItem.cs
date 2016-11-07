namespace MetaData.MetaData.Items
{
    /// <summary>
    /// Capable of storing two different versions of one item.
    /// The alternative would be a Tuple.
    /// </summary>
    /// <typeparam name="T">Type of items to contain</typeparam>
    public class VersionItem<T>
    {
        public T New;
        public T Old;

        public VersionItem(T old, T @new)
        {
            Old = old;
            New = @new;
        }
    }
}