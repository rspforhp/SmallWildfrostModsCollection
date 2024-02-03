using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DeadExtensions;
using Deadpan.Enums.Engine.Components.Modding;
using HarmonyLib;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Speedrun
{
    public class SpeedrunMod : WildfrostMod
    {
        public SpeedrunBehaviour Instance;

        public class SpeedrunBehaviour : MonoBehaviour
        {
            private void OnGUI()
            {
                unsafe
                {
                    var s = UnityEngine.Random.state;
                    uint* x128Int = (uint*)&s;
                    GUILayout.TextArea("Unity seed: " + FromInt128(x128Int));
                    if (Campaign.Data != null)
                    {
                        GUILayout.TextArea("Campaign seed: " + Campaign.Data.Seed);
                        if (GUILayout.Button("Randomize"))
                        {
                            Campaign.Data.Seed = Dead.Random.Seed();
                            SaveSystem.SaveProgressData("nextSeed", Campaign.Data.Seed);
                            SaveSystem.SaveProgressData("seed", Campaign.Data.Seed);
                            if (SelectLeader.FindObjectOfType<SelectLeader>() is { } leader)
                            {
                                this.StartCoroutine(Do(leader));
                            }
                        }

                        if (GUILayout.Button("Update leaders"))
                        {
                            updateLeaders:
                            if (SelectLeader.FindObjectOfType<SelectLeader>() is { } leader)
                            {
                                this.StartCoroutine(Do(leader));
                            }
                        }
                    }
                }
            }

            private IEnumerator Do(SelectLeader leader)
            {
                leader.Cancel();
                leader.Run(PatchTest1.InitialTribes[leader]);
                leader.SetSeed(Campaign.Data.Seed);
                yield return leader.GenerateLeaders(false);
                CardPopUp.Clear();
                leader.FlipUpLeaders();
            }
            private unsafe string FromInt128(uint* s)
            {
                string res = "";
                for (int i = 0; i < 4; i++)
                {
                    res += s[i];
                }

                return res;
            }
            private static System.Random GetFrom(Type t)
            {
                return (System.Random)t.GetField("random", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            }
            private static System.Random GetFrom<T>()
            {
                return (System.Random)typeof(T).GetField("random", BindingFlags.Static | BindingFlags.NonPublic)
                    .GetValue(null);
            }
        }



        [HarmonyPatch(typeof(SelectLeader), nameof(SelectLeader.Run), new Type[] { typeof(List<ClassData>) })]
        internal class PatchTest1
        {
            internal static Dictionary<SelectLeader, List<ClassData>> InitialTribes =
                new();

            [HarmonyPostfix]
            static void TestPatch(SelectLeader __instance,List<ClassData> tribes)
            {
                InitialTribes[__instance] = tribes;
            }
        }

        public SpeedrunMod(string modDirectory) : base(modDirectory)
        {
        }

        protected override void Load()
        {
            base.Load();
            Instance = new GameObject("Speedrun").AddComponent<SpeedrunBehaviour>();
            GameObject.DontDestroyOnLoad(Instance);
        }

        protected override void Unload()
        {
            base.Unload();
            Instance.Destroy();
        }


        public override string GUID => "kopie.wildfrost.speedrunmod";
        public override string[] Depends => Array.Empty<string>();
        public override string Title => "Speedrun Mod";
        public override string Description => "Mod for speedrunners.";
    }
}