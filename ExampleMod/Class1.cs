using System;
using System.Collections.Generic;
using System.Linq;
using Deadpan.Enums.Engine.Components.Modding;

namespace ExampleMod
{
    public class UnityExplorerMod : WildfrostMod
    {
        public UnityExplorerMod(string modDirectory) : base(modDirectory)
        {
            Cards = new List<CardDataBuilder>()
            {
                new CardDataBuilder(this).CreateUnit("testUnit1", "Burrown")
                    .SetSprites("chrome.png", "white.png")
                    .SetStats(8, 5, 3)
            };
        }

        protected override void Load()
        {
            base.Load();
        }

        protected override void Unload()
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
        public override string[] Depends => Array.Empty<string>();
        public override string Title => "Example mod";
        public override string Description => "Mod for making mods.";
    }
}