using System;
using System.Collections.Generic;
using System.Linq;
using AssortedPatchesCollection;
using Deadpan.Enums.Engine.Components.Modding;
using UnityEngine;

namespace ExampleMod
{
    public class UnityExplorerMod : WildfrostMod
    {
        public UnityExplorerMod(string modDirectory) : base(modDirectory)
        {
        }

        public override void Load()
        {
            if (Cards == null)
            {
                Cards = new List<CardDataBuilder>();
                Cards.Add(new CardDataBuilder(this).CreateUnit("testunit2", "No unlock pet")
                    .SetStats(8, 5, 3).IsPet((ChallengeData)null, true).FreeModify(delegate(CardData data)
                    {
                        Debug.LogWarning($"Custom popups before " + data.customData);
                        data.SetCustomData("customPopups", new List<CustomCardPopup>()
                        {
                            new CustomCardPopup(this, "testCustomPopup", this.Get<CardData>("BoostPet"))
                        });
                        Debug.LogWarning($"Custom popups after " + data.customData);
                    }).SubscribeToAfterAllBuildEvent(
                        delegate(CardData data)
                        {
                            Debug.LogWarning($"Custom popups before " + data.customData);
                            data.SetCustomData("customPopups", new List<CustomCardPopup>()
                            {
                                new CustomCardPopup(this, "testCustomPopup", this.Get<CardData>("BoostPet"))
                            });
                            Debug.LogWarning($"Custom popups after " + data.customData);
                        }
                    ));
            }

            base.Load();
        }

        public override void Unload()
        {
            base.Unload();
        }

        private List<CardDataBuilder> Cards;

        public override List<T> AddAssets<T, Y>()
        {
            var dataName = typeof(Y).Name;
            if (dataName == nameof(CardData)) return Cards.Cast<T>().ToList();
            return null;
        }

        public override string GUID => "kopie.wildfrost.examplemod";
        public override string[] Depends => new[] { "!kopie.wildfrost.assorted" };
        public override string Title => "Example mod";
        public override string Description => "Mod for making mods.";
    }
}