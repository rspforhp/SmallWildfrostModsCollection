using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Deadpan.Enums.Engine.Components.Modding;
using HarmonyLib;
using UnityEngine;

namespace CharmsCollection
{
    public class CharmsCollectionMod : WildfrostMod
    {
        public CharmsCollectionMod(string modDirectory) : base(modDirectory)
        {
            Instance = this;
        }

        public override void Load()
        {
            if (Upgrades == null)
            {
                Upgrades = new();
                Upgrades.Add(new CardUpgradeDataBuilder(this).CreateCharm("charm_speed").WithImage("charm_speed.png")
                    .AddPool().SetConstraints(new TargetConstraintDoesAttack()).WithTitle("Speed charm")
                    .WithText("Cards with speed always attack before others."));
            }


            base.Load();
        }

        private static CharmsCollectionMod Instance;


        [HarmonyPatch(typeof(Battle),
            nameof(Battle.ProcessUnits), typeof(Character))]
        internal static class SpeedPriority
        {
            [HarmonyPostfix]
            internal static IEnumerator TestPatch(IEnumerator original, Battle __instance, Character character)
            {
                // Store list of processed entities, in case the order changes *during* processing
                var processed = new List<Entity>();

                // Run pre process units event
                Events.InvokePreProcessUnits(character);

                // Loop here so that the process can be restarted if dirtied (if positions have changed)
                bool dirty;
                do
                {
                    dirty = false;

                    // Get a list of entities to process
                    var entities = Battle.GetAllUnits(character).ToList();
                    if (character == __instance.enemy)
                    {
                        var allUnits = Battle.GetAllUnits(__instance.player);
                        foreach (var uni in allUnits)
                        {
                            if(uni.data.upgrades
                               .FindAll(a => a.name == Extensions.PrefixGUID("charm_speed", Instance)).Count>0)
                            entities.Add(uni);
                        }

                        var orderedEnumerable = entities.OrderBy(delegate(Entity entity)
                        {
                            return -entity.data.upgrades
                                .FindAll(a => a.name == Extensions.PrefixGUID("charm_speed", Instance)).Count;
                        });
                        entities = orderedEnumerable.ToList();
                    }
                    else if (character == __instance.player)
                    {
                        entities.RemoveAll(delegate(Entity entity)
                        {
                           return entity.data.upgrades.FindAll(a => a.name == Extensions.PrefixGUID("charm_speed", Instance)).Count>0;
                        });
                    }

                    // Remove already processed entities
                    entities.RemoveMany(processed);

                    // Store positions
                    var positions = entities.ToDictionary(e => e, e => e.actualContainers.ToArray());

                    // Process all entities!
                    foreach (var entity in entities)
                    {
                        // Make sure entity still exists
                        if (!entity.IsAliveAndExists())
                        {
                            dirty = true;
                            Debug.Log("BATTLE PROCESS LIST DIRTIED! An entity in the list no longer exists");
                            break;
                        }

                        // Check for dirtying list, if this entity is in the wrong position
                        var currentContainers = entity.actualContainers.ToArray();
                        if (!positions.ContainsKey(entity) || !positions[entity].ContainsAll(currentContainers))
                        {
                            dirty = true;
                            Debug.Log(
                                $"BATTLE PROCESS LIST DIRTIED! [{entity.name}] was expected at [{positions[entity]}], but was actually at [{currentContainers}]");
                            break;
                        }

                        // Check if this entity's leader has been killed
                        var leaderCount = __instance.minibosses.Count(a => a.owner.team == entity.owner.team);
                        if (leaderCount <= 0)
                        {
                            Debug.Log($"{entity.name}'s Leader No Longer Exists! Skipping Processing...");
                            continue;
                        }

                        // Otherwise, process the entity
                        yield return __instance.ProcessUnit(entity);
                        processed.Add(entity);
                        if (__instance.cancelTurn) break;
                    }
                } while (dirty && !__instance.cancelTurn);

                // Run post process units event
                Events.InvokePostProcessUnits(character);
            }
        }


        private List<CardUpgradeDataBuilder> Upgrades;

        public override List<T> AddAssets<T, Y>()
        {
            var dataName = typeof(Y).Name;
            if (dataName == nameof(CardUpgradeData)) return Upgrades.Cast<T>().ToList();
            return null;
        }

        public override string GUID => "kopie.wildfrost.charmscollection";
        public override string[] Depends => Array.Empty<string>();
        public override string Title => "Charms collection";
        public override string Description => "Mod that adds some unique charms.\n\n\n\n Adds speed charm - attack before other cards.\n";
    }
}