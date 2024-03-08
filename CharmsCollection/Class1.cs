using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Deadpan.Enums.Engine.Components.Modding;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CharmsCollection
{
    public static class Extensions
    {
        public static bool HasEffect<T>(this Entity card) where T : StatusEffectData
        {
            return card.data.startWithEffects.ToList().Any(a => a.data is T);
        }

        public static bool HasAttackEffect<T>(this Entity card) where T : StatusEffectData
        {
            return card.data.attackEffects.ToList().Any(a => a.data is T);
        }
    }

    public class CharmsCollectionMod : WildfrostMod
    {
        public CharmsCollectionMod(string modDirectory) : base(modDirectory)
        {
            Instance = this;
        }

        public class SpeedEffectData : StatusEffectData
        {
        }

        public class BifurcatedEffectData : StatusEffectChangeTargetMode
        {
        }


        public class TargetModeBifurcated : TargetMode
        {
            public override Entity[] GetPotentialTargets(Entity entity, Entity target, CardContainer targetContainer)
            {
                var targets = new HashSet<Entity>();

                // If specific target is set, use it!
                if (target)
                {
                    targets.Add(target);
                }
                else
                {
                    var rows = Battle.instance.GetRowIndices(entity);
                    if (rows.Length > 0)
                    {
                        for (var rowIndex = 0; rowIndex < Battle.instance.rowCount; rowIndex++)
                        {
                            AddTargets(entity, targets, rowIndex);
                        }
                    }
                }

                // Return array of target(s)
                return targets.Count > 0
                    ? targets.ToArray()
                    : null;
            }

            void AddTargets(Entity entity, HashSet<Entity> targets, int rowIndex)
            {
                // Try to find single target in the opposing row
                var enemies = entity.GetEnemiesInRow(rowIndex);
                Entity target = null;
                foreach (var t in enemies)
                {
                    if (t && t.enabled && t.alive && t.canBeHit)
                    {
                        target = t;
                        break;
                    }
                }

                // Add target to results
                if (target)
                {
                    targets.Add(target);
                }
                else
                {
                    // If no target found, try to target the enemy *character*
                    target = GetEnemyCharacter(entity);
                    if (target)
                    {
                        targets.Add(target);
                    }
                }
            }
        }


        public override void Load()
        {
            if (Upgrades == null)
            {
                Effects = new();
                Effects.Add(new StatusEffectDataBuilder(this).Create<SpeedEffectData>("speed"));
                Effects.Add(new StatusEffectDataBuilder(this).Create<BifurcatedEffectData>("bifurcated").FreeModify(
                    delegate(StatusEffectData data)
                    {
                        if (data is BifurcatedEffectData bif)
                        {
                            var d = new TargetModeBifurcated();
                            d.name = "TargetModeBifurcated";
                            bif.targetMode = d;
                        }
                    }));
                Upgrades = new();
                Upgrades.Add(new CardUpgradeDataBuilder(this).CreateCharm("charm_speed").WithImage("charm_speed.png")
                    .AddPool().SetConstraints(new TargetConstraintIsUnit()).SubscribeToAfterAllBuildEvent(
                        delegate(CardUpgradeData data)
                        {
                            data.Edit<CardUpgradeData, CardUpgradeDataBuilder>()
                                .SetEffects(new CardData.StatusEffectStacks(this.Get<StatusEffectData>("speed"), 1));
                        }).WithTitle("Speed charm")
                    .WithText("Cards with speed always attack before others."));
                Upgrades.Add(new CardUpgradeDataBuilder(this).CreateCharm("charm_bifurcated")
                    .WithImage("charm_speed.png")
                    .AddPool().SetConstraints(new TargetConstraintIsUnit()).SubscribeToAfterAllBuildEvent(
                        delegate(CardUpgradeData data)
                        {
                            data.Edit<CardUpgradeData, CardUpgradeDataBuilder>()
                                .SetEffects(
                                    new CardData.StatusEffectStacks(this.Get<StatusEffectData>("bifurcated"), 1));
                        }).WithTitle("Bifurcated charm")
                    .WithText("Attacks in both lines."));
                Upgrades.Add(new CardUpgradeDataBuilder(this).CreateCharm("charm_merge")
                    .WithImage("charm_merge.png")
                    .AddPool().WithTitle("Merge charm")
                    .WithText("Combines with next card in deck view, gaining its stats(averaged out) and effects."));
            }


            base.Load();
        }

        private static CharmsCollectionMod Instance;

        [HarmonyPatch(typeof(CardUpgradeData),
            nameof(CardUpgradeData.Assign), typeof(Entity))]
        internal static class MergeCharm
        {
            [HarmonyPostfix]
            internal static IEnumerator TestPatch(IEnumerator original, CardUpgradeData __instance, Entity entity)
            {
                if (__instance.name !=
                    Deadpan.Enums.Engine.Components.Modding.Extensions.PrefixGUID("charm_merge", Instance))
                {
                    yield return original;
                    yield break;
                }
                bool og = true;
                Instance.WriteLine($"Attaching {__instance} to {entity}");

                var decks = Object.FindObjectsOfType<DeckDisplaySequence>();
                Instance.WriteLine($"Decks {decks} {decks?.Length}");
                foreach (var deckDisplay in decks)
                {
                    var active = deckDisplay.activeCardsGroup.GetGrid(entity.data);

                    IEnumerator CombineSequence(CardData[] source, CardData result)
                    {
                        while (CinemaBarSystem.Top==null)
                        {
                            CinemaBarSystem.Top = CinemaBarSystem.instance.top;
                            Instance.WriteLine($"doing {CinemaBarSystem.Top} top!");
                        }
                        while (CinemaBarSystem.Bottom==null)
                        {
                            CinemaBarSystem.Bottom = CinemaBarSystem.instance.bottom;
                            Instance.WriteLine($"doing {CinemaBarSystem.Top} bottom!");
                        }
                        Instance.WriteLine($"{CinemaBarSystem.Bottom} bottom!");
                        Instance.WriteLine($"{CinemaBarSystem.Top} bottom!");

                        CombineCardSequence combineSequence = null;
                        yield return SceneManager.Load("CardCombine", SceneType.Temporary,
                            scene => { combineSequence = scene.FindObjectOfType<CombineCardSequence>(); });
                        if (combineSequence)
                        {
                            yield return combineSequence.Run(source, result);
                        }

                        yield return SceneManager.Unload("CardCombine");
                        Instance.WriteLine($"Running combine sequence!");
                        yield return deckDisplay.Run();
                        Instance.WriteLine($"Running deck display again!");

                    }

                    if (active.Contains(entity))
                    {
                        Instance.WriteLine($"ACTIVE CARD CONTAINS");
                        var grid = active;
                        Instance.WriteLine($"Starting F");
                        var index = grid.IndexOf(entity);
                        Instance.WriteLine($"Index of card {index}");
                        if (grid.Count <= index + 1)
                        {
                            var drag = CardCharmDragHandler.FindObjectOfType<CardCharmDragHandler>();
                            if (drag)
                            {
                                og = false;
                                Instance.WriteLine($"Not fit.");
                                yield break;
                            }
                        }

                  
                        var d = CloneCard(grid,index,entity);
                        yield return CombineSequence(new[] { entity.data, d.nextData }, d.clone);
                        Instance.WriteLine($"Running combine sequence!.");
                    }
                    else
                    {
                        var reserve = deckDisplay.reserveCardsGroup.GetGrid(entity.data);
                        if (reserve.Contains(entity))
                        {
                            Instance.WriteLine($"RESERVE CARD CONTAINS");
                            var grid = reserve;
                            Instance.WriteLine($"Starting F");
                            var index = grid.IndexOf(entity);
                            Instance.WriteLine($"Index of card {index}");
                            if (grid.Count <= index + 1)
                            {
                                var drag = CardCharmDragHandler.FindObjectOfType<CardCharmDragHandler>();
                                if (drag)
                                {
                                    // Or return to previous holder
                                    if (drag.preHolder)
                                    {
                                        drag.ReturnToHolder();
                                    }

                                    // End dragging state
                                    drag.DragEnd();
                                    og = false;
                                    Instance.WriteLine($"Not fit.");
                                    yield break;
                                }
                            }


                            var d = CloneCard(grid, index, entity);
                            yield return CombineSequence(new[] { entity.data, d.nextData }, d.clone);
                          
                        }
                        else
                        {
                            Instance.WriteLine($"NOT CONTAINS");
                            continue;
                        }
                    }

                    Instance.WriteLine($"END OF CARD CONTAINS");

                    break;
                }

                Instance.WriteLine($"RUNNING VANILLA");

                if (og)
                    yield return original;
            }

            private static (CardData clone, CardData nextData) CloneCard(CardContainerGrid grid, int index, Entity entity)
            {
                var next = grid[index + 1];
                var nextData = next.data;
                CardData clone = entity.data.Clone();
                clone.damage += nextData.damage;
                clone.damage = Mathf.CeilToInt(clone.damage/2f);
                clone.hp += nextData.hp;
                clone.hp = Mathf.CeilToInt(clone.hp/2f);
                if (clone.counter == 0&& !clone.IsItem && clone.damage > 1) clone.counter = clone.damage;
                clone.counter += nextData.counter;
                clone.counter = Mathf.CeilToInt(clone.counter/2f);
                clone.hasAttack |= nextData.hasAttack;
                clone.hasHealth |= nextData.hasHealth;
                clone.startWithEffects =
                    entity.data.startWithEffects.AddRangeToArray(next.data.startWithEffects);
                clone.attackEffects = entity.data.attackEffects.AddRangeToArray(next.data.attackEffects);
                clone.upgrades.AddRange(nextData.upgrades);
                clone.forceTitle = $"{clone.title} + {nextData.title}";
                clone.upgrades.Add(Instance.Get<CardUpgradeData>("charm_merge"));
                clone.traits.AddRange(nextData.traits);
                return (clone, nextData);
            }
        }


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
                            if (uni.data.startWithEffects.ToList()
                                    .FindAll(a => a.data is SpeedEffectData).Count > 0)
                                entities.Add(uni);
                        }

                        var orderedEnumerable = entities.OrderBy(delegate(Entity entity)
                        {
                            return -entity.data.startWithEffects.ToList()
                                .FindAll(a => a.data is SpeedEffectData).Count;
                        });
                        entities = orderedEnumerable.ToList();
                    }
                    else if (character == __instance.player)
                    {
                        entities.RemoveAll(delegate(Entity entity)
                        {
                            return entity.data.startWithEffects.ToList()
                                .FindAll(a => a.data is SpeedEffectData).Count > 0;
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
        private List<StatusEffectDataBuilder> Effects;

        public override List<T> AddAssets<T, Y>()
        {
            var dataName = typeof(Y).Name;
            if (dataName == nameof(CardUpgradeData)) return Upgrades.Cast<T>().ToList();
            if (dataName == nameof(StatusEffectData)) return Effects.Cast<T>().ToList();
            return null;
        }

        public override string GUID => "kopie.wildfrost.charmscollection";
        public override string[] Depends => Array.Empty<string>();
        public override string Title => "Charms collection";

        public override string Description =>
            "Mod that adds some unique charms.\n\n\n\n Adds speed charm - attack before other cards.\n";
    }
}