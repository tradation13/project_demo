namespace IPTS.Helpers
{
    public static class EnumerableHelper
    {
        public static bool HasCommonElement<T>(IEnumerable<T> first, IEnumerable<T> second)
        {
            var set = new HashSet<T>(first);
            return second.Any(item => set.Contains(item));
        }

    }
}
