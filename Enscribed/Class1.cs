using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Deadpan.Enums.Engine.Components.Modding;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Enscribed
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
       
        public static CardDataBuilder SetBlood(this CardDataBuilder card, int? blood = null)
        {
            card.SubscribeToAfterAllBuildEvent(delegate(CardData data)
            {
                data.traits.Add(new CardData.TraitStacks(EnscrybedMod.Instance.Get<TraitData>("Blood"),
                    blood ?? 0));
            });
            return card;
        }

        public static CardDataBuilder SetStats(this CardDataBuilder card, int? health = null, int? damage = null,
            int counter = 0, int? blood = null)
        {
            return card.WithValue(200).SetHealth(health).SetDamage(damage).SetCounter(counter).SetBlood(blood);
        }
    }


    public class BloodEffectData : TraitData
    {
    }

    public class GuardDogEffectData : StatusEffectData
    {
        public override void Init()
        {
            base.Init();
            
            OnCardMove+= OnOnCardPlayed;
        }
        public static int IndexOnBoard(Entity e)
        {
            var rowCount = 2;
            var rowLength = 3;
            for (var i = 0; i < rowLength; i++)
            {
                for (var j = 0; j < rowCount; j++)
                {
                    if (Battle.instance.GetRow(e.owner, j) is CardSlotLane row)
                    {
                        var slot = row.slots[i];
                        if (slot.GetTop()==e)
                        {
                            return j;
                        }
                    }
                }
            }
            return -1;
        }
        private IEnumerator OnOnCardPlayed(Entity playedCard)
        {
            if (playedCard == target || playedCard.owner == target.owner) yield break;
            var indexOnBoardMy = IndexOnBoard(target);
            var indexOnBoardOf = IndexOnBoard(playedCard);
            if (indexOnBoardOf == -1) yield break;
            if (indexOnBoardMy != indexOnBoardOf)
            {
                if (Battle.instance.GetRow(target.owner, indexOnBoardOf).Count < 3)
                    yield return new ActionMove(target, Battle.instance.GetRow(target.owner, indexOnBoardOf)).Run();
            }
        }
    }

    public class EvolveEffectData : StatusEffectData
    {
        public uint TurnAmount = 1;
        private uint TurnsLeft;
        public CardData EvolveInto;
        public override void Init()
        {
            base.Init();
            TurnsLeft = TurnAmount+1;

            this.OnTurnEnd += OnOnTurnStart;
        }

        private IEnumerator OnOnTurnStart(Entity e)
        {
            //ignore the entity passed here cause its dumb??
            e = target;
            if (--TurnsLeft == 0)
            {
                
                EnscrybedMod.Instance.WriteLine(e.ToString());
                
                {
                    e.flipper.FlipDown();
                    e.data = EvolveInto;
                    e.statusEffects.Remove(this);
                    yield return e.display.UpdateData(true) ;
                    e.flipper.FlipUp();
                }
               
                  
            }
            else { yield break; }
        }
    }
    public class EnscrybedMod : WildfrostMod
    {
        public EnscrybedMod(string modDirectory) : base(modDirectory)
        {
            Instance = this;

            IEnumerable<CardDataBuilder> _1()
            {
                //just make all their counters be 1-2
                yield return new CardDataBuilder(this).CreateUnit("stoat", "Stoat").AddPool().SetSprites("stoat.png","card.png").SetStats(2, 1, 2, 1);
                yield return new CardDataBuilder(this).CreateUnit("wolf_cub", "Wolf Cub").AddPool().SetSprites("wolf_cub.png","card.png").SetStats(1, 1, 2, 1).SubscribeToAfterAllBuildEvent(
                    delegate(CardData data)
                    {
                        data.startWithEffects =
                            data.startWithEffects.AddToArray(new CardData.StatusEffectStacks(this.Get<StatusEffectData>("Evolve into wolf"),1));
                    });
                yield return new CardDataBuilder(this).CreateUnit("wolf", "Wolf").AddPool().SetSprites("wolf.png","card.png").SetStats(2, 3, 2, 2);
                yield return new CardDataBuilder(this).CreateUnit("blood_hound", "Blood Hound").AddPool().SetSprites("blood_hound.png","card.png").SetStats(3, 2, 2, 2).SubscribeToAfterAllBuildEvent(
                    delegate(CardData data)
                    {
                        data.startWithEffects =
                            data.startWithEffects.AddToArray(new CardData.StatusEffectStacks(this.Get<StatusEffectData>("Guarddog"),1));
                    });
                yield return new CardDataBuilder(this).CreateUnit("turtle", "River Snapper").AddPool().SetSprites("turtle.png","card.png").SetStats(6, 1, 2, 2);
                yield return new CardDataBuilder(this).CreateUnit("grizzly", "Grizzly").AddPool().SetSprites("grizzly.png","card.png").SetStats(6, 4, 2, 3);
                yield return new CardDataBuilder(this).CreateUnit("urayuli", "Urayuli").AddPool().SetSprites("urayuli.png","card.png").SetStats(7, 7, 2, 4);
                
                
                
                
                yield return new CardDataBuilder(this).CreateUnit("squirrel", "Squirrel").WithCardType("Summoned").SetSprites("squirrel.png","card.png").SetStats(1,null,0);
                yield return new CardDataBuilder(this).CreateItem("summon_squirrel", "Summon Squirrel").IsPet((ChallengeData)null, true).CanPlayOnBoard().CanPlayOnEnemy(false).CanPlayOnFriendly(true).AddPool("GeneralItemPool").SetSprites("squirrel.png","card.png").SubscribeToAfterAllBuildEvent(
                    delegate(CardData data)
                    {
                        data.playOnSlot = true;
                        data.traits.Add(new CardData.TraitStacks(Get<TraitData>("Noomlin"),1));
                        data.startWithEffects =
                            data.startWithEffects.AddToArray(
                                new CardData.StatusEffectStacks(Get<StatusEffectData>("Squirrel"), 1));
                    });
            }

            AddAssetsEvent += (Func<IEnumerable<CardDataBuilder>>)_1;
            IEnumerable<StatusEffectDataBuilder> _4()
            {
                yield return new StatusEffectDataBuilder(this).Create<GuardDogEffectData>("Guarddog").WithText($"When opponent moves card into a slot, go into it's lane.");
                yield return new StatusEffectDataBuilder(this).Create<StatusEffectSummon>("Squirrel").WithText($"Summon a squirrel.").SetSummonPrefabRef().SubscribeToAfterAllBuildEvent(
                    delegate(StatusEffectData data)
                    {
                        if (data is StatusEffectSummon sum)
                        {
                            sum.gainTrait = Get<StatusEffectData>("Temporary Summoned");
                            sum.summonCard = Get<CardData>("squirrel");
                        }
                    });
                yield return new StatusEffectDataBuilder(this).Create<EvolveEffectData>("Evolve into wolf").WithText($"Evolve into a wolf in 1 turn.").SubscribeToAfterAllBuildEvent(
                    delegate(StatusEffectData data)
                    {
                        if (data is EvolveEffectData sum)
                        {
                            sum.EvolveInto = Get<CardData>("wolf");
                        }
                    });
            }

            AddAssetsEvent += (Func<IEnumerable<StatusEffectDataBuilder>>)_4;


            IEnumerable<TraitDataBuilder> _2()
            {
                yield return new TraitDataBuilder(this).Create<BloodEffectData>("Blood").SubscribeToAfterAllBuildEvent(
                    delegate(TraitData data) { data.keyword = Blood; });
            }

            AddAssetsEvent += (Func<IEnumerable<TraitDataBuilder>>)_2;


           
            IEnumerable<KeywordDataBuilder> _3()
            {
                yield return new KeywordDataBuilder(this).Create("Blood").WithShow()
                    .WithTitleColour(new Color(80, 32, 62)).WithCanStack(true).WithShowName(true).WithTitle("Blood")
                    .WithDescription("Kills X units with least health(summoned units take priority) to play, if not enough units dies instantly. Leaders cant be killed by this. Doesn't use a turn when played.").SubscribeToBuildEvent(
                        delegate(KeywordData data)
                        {
                            Blood = data;
                            WriteLine("Blood keyword");
                        });
            }

            AddAssetsEvent += (Func<IEnumerable<KeywordDataBuilder>>)_3;
        }

        public KeywordData Blood;

        #region FastAssets

        private Dictionary<Type, List<Func<IEnumerable<object>>>> Subscriptions = new();

        private event Func<IEnumerable<object>> AddAssetsEvent
        {
            add
            {
                var type = value.GetType().GenericTypeArguments[0].GenericTypeArguments[0];
                if (!Subscriptions.ContainsKey(type))
                {
                    Subscriptions.Add(type, new List<Func<IEnumerable<object>>>());
                }

                Subscriptions[type].Add(value);
            }
            remove
            {
                var type = value.GetType().GenericTypeArguments[0].GenericTypeArguments[0];
                if (!Subscriptions.ContainsKey(type))
                {
                    Subscriptions.Add(type, new List<Func<IEnumerable<object>>>());
                }

                Subscriptions[type].Remove(value);
            }
        }

        private Dictionary<Type, IEnumerable<object>> GCSafety = new();

        public override List<T1> AddAssets<T1, T2>()
        {
            var BuilderType = typeof(T1);
            if (!Subscriptions.ContainsKey(BuilderType)) return null;
            if (GCSafety.TryGetValue(BuilderType, out var value)) return (List<T1>)value;
            var list = new List<T1>();
            foreach (var func in Subscriptions[BuilderType])
            {
                list.AddRange((IEnumerable<T1>)func());
            }

            GCSafety[BuilderType] = list;
            return (List<T1>)GCSafety[BuilderType];
        }

        #endregion

       


        public override void Load()
        {
            base.Load();
            Events.OnEntityPlace+= OnEventsOnOnEntityPlace;
        }

        private void OnEventsOnOnEntityPlace(Entity entity, CardContainer[] containers, bool freeMove)
        {
            if (freeMove)
                return;
            var f = entity.traits.Find(a=>a.data.name==Instance.Get<TraitData>("Blood").name);
            if (f!=default)
            {
                var amount = f.count;
                var slot = containers[0];
                var entities = Battle.GetCardsOnBoard(slot.owner).OrderBy(a => a.hp.current*(a.statusEffects.Any(b=>b.name=="Temporary Summoned")?0:1)).ToList();
                entities.RemoveAll(a => Battle.instance.minibosses.Contains(a));
                if (entities.Count < amount)
                {
                    ActionQueue.Add(new ActionKill(entity));
                }
                else
                {
                    var c=new Routine.Clump();
                    for (int i = 0; i < amount; i++)
                    {
                        var e = entities[i];
                       c.Add(new Hit(entity, e, 999).Process());
                    }
                    CoroutineManager.Start(c.WaitForEnd());

                }
                slot.owner.freeAction = true;
            }
        }

        public override void Unload()
        {
            base.Unload();
            Events.OnEntityPlace-= OnEventsOnOnEntityPlace;

        }

        internal static EnscrybedMod Instance;


        public override string GUID => "kopie.wildfrost.Enscrybed";
        public override string[] Depends => Array.Empty<string>();
        public override string Title => "Enscryben v0.1";

        public override string Description =>
            "Adds cards from inscryption and a blood cost system.";
    }
}