using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Deadpan.Enums.Engine.Components.Modding;
using HarmonyLib;
using MonoMod.Cil;
using Newtonsoft.Json;
using UnityEngine;

namespace AssortedPatchesCollection
{
    public class CustomCardPopup
    {
        private string name;
        public WildfrostMod FromMod;

        public string iconName;
        public string iconTintHex;
        public string title;
        public Color titleColour;
        public string body;
        public Color bodyColour;
        public string note;
        public Color noteColour;
        public Sprite panelSprite;
        public Color panelColor;

        public CardData cardMention;


        public CustomCardPopup(WildfrostMod mod, string name, CardData cardMention)
        {
            this.name = name;
            this.FromMod = mod;
            this.cardMention = cardMention;
        }

        public override string ToString()
        {
            return cardMention?.name;
        }

        public CustomCardPopup(WildfrostMod mod, string name, string iconName = default, string iconTintHex = default,
            string title = default, Color titleColour = default, string body = default, Color bodyColour = default,
            string note = default, Color noteColour = default, Sprite panelSprite = default, Color panelColor = default)
        {
            if (titleColour == default(Color)) titleColour = new Color(1f, 0.7921569f, 0.3411765f, 1f);
            if (bodyColour == default(Color)) bodyColour = Color.white;
            if (noteColour == default(Color)) noteColour = Color.gray;
            this.FromMod = mod;
            this.name = name;
            this.iconName = iconName;
            this.iconTintHex = iconTintHex;
            this.title = title;
            this.titleColour = titleColour;
            this.body = body;
            this.bodyColour = bodyColour;
            this.note = note;
            this.noteColour = noteColour;
            this.panelSprite = panelSprite;
            this.panelColor = panelColor;
        }

        public string Name => FromMod.GUID + "." + name;
    }

    public class MainMod : WildfrostMod
    {
        [HarmonyPatch(typeof(CardPopUpTarget), nameof(CardPopUpTarget.Pop), new Type[] { })]
        internal static class CustomPopupHandler2
        {
            internal static CardPopUpPanel AddPanel(CustomCardPopup customPopup)
            {
                if (!MonoBehaviourRectSingleton<CardPopUp>.instance.activePanels.ContainsKey(customPopup.Name))
                {
                    var panel2 = MonoBehaviourRectSingleton<CardPopUp>.instance.GetPanel<CardPopUpPanel>();
                    panel2.gameObject.name = customPopup.Name;
                    panel2.gameObject.name = customPopup.Name;
                    panel2.SetRoutine(customPopup.iconName, customPopup.iconTintHex, customPopup.title,
                        customPopup.titleColour, customPopup.body, customPopup.bodyColour, customPopup.note,
                        customPopup.noteColour, customPopup.panelSprite, customPopup.panelColor);
                    MonoBehaviourRectSingleton<CardPopUp>.instance.activePanels.Add(customPopup.Name, (Tooltip)panel2);
                    return panel2;
                }

                return null;
            }


            [HarmonyPostfix]
            internal static void TestPatch(CardPopUpTarget __instance)
            {
                if (__instance.IsCard)
                {
                    var cardData = __instance.card.entity.data;
                    if (cardData.customData != null)
                    {
                        CardPopUp.AssignToCard(__instance.card);
                        Instance.WriteWarn($"{cardData.name} custom data not null");
                        if (cardData.customData.TryGetValue(customPopupsKey, out var pop))
                        {
                            Instance.WriteWarn($"Found custom popups " + pop);
                            if (pop is List<CustomCardPopup> list)
                            {
                                foreach (var popup in list)
                                {
                                    Instance.WriteWarn($"{cardData.name} {popup}");
                                    if (popup.cardMention && popup.cardMention != null)
                                    {
                                        if (__instance.current.Add(popup.cardMention.name))
                                            CardPopUp.AddPanel(popup.cardMention);
                                    }
                                    else if (__instance.current.Add(popup.Name))
                                    {
                                        AddPanel(popup);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(InspectSystem), nameof(InspectSystem.CreatePopups), new Type[] { })]
        internal static class CustomPopupHandler
        {
            internal static CardPopUpPanel Popup(InspectSystem __instance, CustomCardPopup customPopup, Transform group)
            {
                if (__instance.popups.All(a => a.name != customPopup.Name))
                {
                    // Create pop up panel
                    var panel = InspectSystem.Instantiate(__instance.popUpPrefab, group);
                    panel.gameObject.name = customPopup.Name;
                    panel.SetRoutine(customPopup.iconName, customPopup.iconTintHex, customPopup.title,
                        customPopup.titleColour, customPopup.body, customPopup.bodyColour, customPopup.note,
                        customPopup.noteColour, customPopup.panelSprite, customPopup.panelColor);


                    // Add to lists
                    //__instance.currentPoppedKeywords.Add(keyword);
                    __instance.popups.Add(panel);

                    // Return created popup panel
                    return panel;
                }

                return null;
            }

            [HarmonyPostfix]
            internal static void TestPatch(InspectSystem __instance)
            {
                if (__instance.inspect.display is Card display)
                {
                    var cardData = display.entity.data;
                    if (cardData.customData != null)
                    {
                        Instance.WriteWarn($"{cardData.name} custom data not null");
                        if (cardData.customData.TryGetValue(customPopupsKey, out var pop))
                        {
                            Instance.WriteWarn($"Found custom popups " + pop);
                            if (pop is List<CustomCardPopup> list)
                            {
                                foreach (var popup in list)
                                {
                                    Instance.WriteWarn($"{cardData.name} {popup}");
                                    if (popup.cardMention && popup.cardMention != null)
                                        __instance.Popup(popup.cardMention, (Transform)__instance.rightPopGroup);
                                    else Popup(__instance, popup, (Transform)__instance.rightPopGroup);
                                }
                            }
                        }
                    }
                }
            }
        }


        //List<CustomCardPopup>
        public const string customPopupsKey = "customPopups";


        [HarmonyPatch(typeof(DataFileBuilder<CardData, CardDataBuilder>),
            nameof(DataFileBuilder<CardData, CardDataBuilder>.OnAfterAllModBuildsEvent))]
        internal static class AddWMITF2
        {
            [HarmonyPostfix]
            internal static void TestPatch(ref CardData d)
            {
                var data = d;
                if (data.customData == null) data.customData = new Dictionary<string, object>();
                if (data.ModAdded != null &&
                    data.TryGetCustomData(customPopupsKey, out var pop, new List<CustomCardPopup>()))
                {
                    if (pop.All(a => a.Name != Instance.GUID + "." + "WMITF"))
                    {
                        pop.Add(new CustomCardPopup(Instance, "WMITF", title: data.title,
                            body: $"Card added by: {data.ModAdded.Title}", note: $"{data.ModAdded.GUID}"));
                    }
                }
            }
        }


        [HarmonyPatch(typeof(CardDataBuilder), nameof(CardDataBuilder.IsPet),
            new Type[] { typeof(ChallengeData), typeof(bool) })]
        internal static class AddToUnlocks
        {
            [HarmonyPrefix]
            internal static bool TestPatch(ref CardDataBuilder __instance, ref CardData ____data,
                ref CardDataBuilder __result, ChallengeData challenge, bool value = true)
            {
                if (value && (challenge == null || !challenge))
                {
                    Instance.WriteWarn($"{____data.name}'s pet unlock data is null, instantly unlocking");
                    AutoUnlockedPets.Add(____data.name);
                    PetHutSequence.OnStart += delegate(PetHutSequence sequence) { sequence.AddSlot(null); };
                    __result = __instance;
                    return false;
                }

                return true;
            }

            internal static List<string> AutoUnlockedPets = new List<string>();
        }

        [HarmonyPatch(typeof(MetaprogressionSystem), nameof(MetaprogressionSystem.GetUnlockedPets), new Type[] { })]
        internal static class FixExtra
        {
            [HarmonyPrefix]
            internal static bool TestPatch(MetaprogressionSystem __instance, ref string[] __result)
            {
                List<string> stringList1 = MetaprogressionSystem.Get<List<string>>("pets");
                List<string> stringList2 =
                    SaveSystem.LoadProgressData<List<string>>("petHutUnlocks", (List<string>)null);
                int length = Mathf.Min(stringList1.Count, 1 + (stringList2?.Count ?? 0));
                string[] unlockedPets = new string[length];
                for (int index = 0; index < length; ++index)
                    unlockedPets[index] = stringList1[index];
                var iter = AddToUnlocks.AutoUnlockedPets.ToArray();
                foreach (var card in iter)
                {
                    Instance.WriteWarn($"{card}'s pet unlock data is null, instantly adding to unlocks pets");
                    Instance.WriteWarn($"{Instance.Get<CardData>(card)} instance of it");
                }

                __result = unlockedPets.AddRangeToArray(iter);

                return false;
            }
        }


        internal static MainMod Instance;

        public MainMod(string modDirectory) : base(modDirectory)
        {
            Instance = this;
        }

        public override void Load()
        {
            base.Load();
        }

        public override void Unload()
        {
            base.Unload();
        }


        public override string GUID => "!kopie.wildfrost.assorted";
        public override string[] Depends => Array.Empty<string>();
        public override string Title => "!Assorted patches";

        public override string Description =>
            "Mod that tweaks some stuff in the game.\n\n\n Pets with null unlock data will show up unlocked instantly.\n Custom popups library. \n What mod is this from for every modded card.";
    }
}