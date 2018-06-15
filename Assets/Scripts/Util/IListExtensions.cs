using System.Collections.Generic;

public static class IListExtensions {
    /// <summary>
    /// Shuffles the element order of the specified list.
    /// </summary>
    public static void Shuffle<T>(this IList<T> ts) {
        var count = ts.Count;
        var last = count - 1;
        for (var i = 0; i < last; ++i) {
            var r = UnityEngine.Random.Range(i, count);
            var tmp = ts[i];
            ts[i] = ts[r];
            ts[r] = tmp;
        }
    }

    /// <summary>
    /// Returns a random element selected from the specified list.
    /// </summary>
    public static T RandomElement<T>(this IList<T> ts) {
        var count = ts.Count;

        if (count == 0)
            return default(T);
        else
            return ts[UnityEngine.Random.Range(0, count)];
    }
}
