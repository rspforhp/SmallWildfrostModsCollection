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
        public static bool TryGetCustomDataT<T>(this CardData data, string key, out T value, T defaultValue)
        {
            if (data.customData != null && data.customData.TryGetValue(key, out var obj1) && obj1 is T obj2)
            {
                value = obj2;
                return true;
            }
            value = defaultValue;
            return false;
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