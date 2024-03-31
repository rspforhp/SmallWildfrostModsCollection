using System;
using System.Collections.Generic;

namespace Test
{
    internal static class Program
    {
        public static Dictionary<Type, List<Func<IEnumerable<object>>>> Subscriptions =
            new Dictionary<Type, List<Func<IEnumerable<object>>>>();

        public static event Func<IEnumerable<object>> AddAssetsEvent
        {
            add
            {
                var type = value.GetType().GenericTypeArguments[0].GenericTypeArguments[0];
                if (!Subscriptions.ContainsKey(type))
                {
                    Subscriptions.Add(type,new List<Func<IEnumerable<object>>>());
                }
                Subscriptions[type].Add(value);
            }
            remove
            {
                var type = value.GetType().GenericTypeArguments[0].GenericTypeArguments[0];
                 if (!Subscriptions.ContainsKey(type))
                {
                    Subscriptions.Add(type,new List<Func<IEnumerable<object>>>());
                }
                Subscriptions[type].Remove(value);
            }
        }

        public static void Main(string[] args)
        {
            AddAssetsEvent += (Func<IEnumerable<string>>)delegate
            {
                return new []
                {
                     "test",
                };
            };
        }
    }
}