namespace search_replace
{
    public static class ExtensionMethods
    {
        /// <summary>
        ///     Compares the <paramref name="value" /> object with the
        ///     <paramref name="testObjects" /> provided, to see if any of the
        ///     <paramref name="testObjects" /> is a match.
        /// </summary>
        /// <param name="value">Source object to check</param>
        /// <param name="testObjects">
        ///     Object or objects that should be compared to value with the
        ///     <see cref="M:System.Objects.Equals" /> method
        /// </param>
        /// <typeparam name="T">Type of the object to be tested</typeparam>
        /// <returns>
        ///     True if any of the <paramref name="testObjects" /> equals the value;
        ///     false otherwise.
        /// </returns>
        public static bool IsAnyOf<T>(this T value, params T[] testObjects)
        {
            return testObjects.Contains
                (value);
        }


        public static bool ContainsAnyOf(this string value, params string[] filterList)
        {
            return filterList.Any(value.Contains);
        }
    }
}