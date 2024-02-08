using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace AssortedPatchesCollection
{
    public static class Extensions
    {
        public static List<T> RemoveAtStay<T>(this List<T> sequence, int index)
        {
            sequence.RemoveAt(index);
            return sequence;
        }

        public static T[] RemoveAtFromArray<T>(this T[] sequence, int index) =>
            (sequence.ToList()).RemoveAtStay(index).ToArray();

        public static List<T> RemoveStay<T>(this List<T> sequence, T item)
        {
            sequence.Remove(item);
            return sequence;
        }

        public static T[] RemoveFromArray<T>(this T[] sequence, T item) =>
            (sequence.ToList()).RemoveStay(item).ToArray();

    }
}