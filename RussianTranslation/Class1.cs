using System;
using System.Collections.Generic;
using System.Linq;
using Deadpan.Enums.Engine.Components.Modding;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

namespace RussianTranslation
{
    public class RussianTranslationMod : WildfrostMod
    {
        public RussianTranslationMod(string modDirectory) : base(modDirectory)
        {
        }

        public override string GUID => "kopie.wildfrost.lang_russian";
        public override string[] Depends => new string[] { };
        public override string Title => "Russian translation mod";
        public override string Description => "Adds a russian language to the game";


        protected override void Load()
        {
            base.Load();
            
            var ru = LocalizationHelper.TryAddLocale(new LocaleIdentifier(SystemLanguage.Russian));
            var ui = LocalizationHelper.GetCollection("UI Text", new LocaleIdentifier(SystemLanguage.Russian));

            #region UI Text
            ui.SetString("demo_wishlist", "Добавьте в список желаемого!");
            ui.SetString("popup_no_target_to_attack", "Нет цели для атаки!");
            ui.SetString("popup_no_allies_to_heal", "Нет союзников для лечения!");
            ui.SetString("popup_no_target_for_status", "Нет целей для {0}");
            ui.SetString("popup_no_space_to_summon", "Нет пространства для призыва!");
            ui.SetString("popup_no_cards_to_draw", "Нет вытягиваемых карт!");
            ui.SetString("popup_no_allies_to_boost", "Ни один союзник не может быть усилен!");
            ui.SetString("popup_no_enemies_to_boost", "Ни один враг не может быть усилен!");
            ui.SetString("popup_requires_junk", "Нуждается в {0} хлама!");
            ui.SetString("popup_no_summoned_allies", "Нет призванных союзников!");
            ui.SetString("popup_cant_split", "Не может разделиться!");
            ui.SetString("popup_play_crown_first", "Сначала сыграйте карты с <sprite name=crown>!");
            ui.SetString("popup_no_allies_to_attack", "Нет союзника для атаки!");
            ui.SetString("popup_cant_move", "Нельзя двигать!");
            ui.SetString("Mods", "Моды");
            
            ui.SetString("tutorial_charm1", "Вы можете экипировать ваши [Брелоки] перетащив их на карту");
            ui.SetString("tutorial_charm2", "Нельзя снять экипированные [Брелоки]!");
            ui.SetString("tutorial_crown", "В отличии от [Брелоков], [Короны] могут быть убраны и поставлены на другие карты между боями");
            ui.SetString("tutorial_injury", "[{0}] был ранен в бою!\n\r\n\rЕсли они выживут следующий бой они востановятся");
            ui.SetString("tutorial_injury_multiple", "Некоторые из ваших [Компаньонов] были ранены в бою!\n\nЕсли они выживут следующий бой они востановятся");
            ui.SetString("tutorial_injured_companion_event", "<size=0.35>[{0}] возвращается [Раненым] из вашего прошлого забега!\n\r[Раненые] компаньоны начинают каждый бой с половиной <sprite name=health> и <sprite name=attack> \n\rЕсли они выживут следующий бой они востановятся");
            ui.SetString("tutorial_companion1", "Вы можете [<action=Inspect>] чтобы [Инспектировать] любую карту |Испекция карт предоставит информацию о их эффектах и статусах");
            ui.SetString("tutorial_companion1_gamepad", "Нажмите [<action=Inspect>] чтобы [Инспектировать] любую карту |Испекция карт предоставит информацию о их эффектах и статусах");
            ui.SetString("tutorial_companion1_touch", "[<action=Inspect>] чтобы [Инспектировать] любую карту |Испекция карт предоставит информацию о их эффектах и статусах");
            ui.SetString("tutorial_companion2", "Выберите [Компаньона] чтобы продолжить!");
            ui.SetString("tutorial_companion2", "[Пнеголовый] это карта [Развалюха]\n\r\n\r[Развалюхи] это предметы которые вы можете поставить чтобы помогать вашим [Компаньонам]");
            ui.SetString("tutorial_battle1_1", "Чтобы начать бой перенесите вашего [Лидера]<sprite name=crown> с вашей [Руки] на [Поле боя]");
            ui.SetString("tutorial_battle1_2", "[{0}] одна из ваших карт [Компаньонов]\n\rТакже перенесите его на [Поле боя]!");
            ui.SetString("tutorial_battle1_3", "Используйте ваш [Хлипкий меч] чтобы вынести этого [Пенгдита]!");
            ui.SetString("tutorial_battle1_4", "[Счетчики карт] <sprite name=counter> уменьшаются на [1] каждый ход\n\r\n\rКогда они достигнут нуля карта атакует!");
            ui.SetString("tutorial_battle1_41", "Враги всегда атакуют первыми!");
            ui.SetString("tutorial_battle1_5", "У вас почти кончились карты!\n\r\n\rВытяните новые карты нажав на [Колокол раздачи]");
            ui.SetString("tutorial_battle1_6", "Вы должны защитить вашего [Лидера]<sprite name=crown>\n\r\n\rЕсли они умрут - игра окончена!");
            ui.SetString("tutorial_battle1_7", "[Колокол волн] показывает количество ходов до следующей волны");
            ui.SetString("tutorial_battle1_8", "Вы должны убить [Снежного рыцаря] чтобы выйграть бой!");
            ui.SetString("tutorial_battle2_1", "Вы также можете [Инспектировать] карты во время боев\n\r\n\r[<action=Inspect>] на [{0}] чтобы инспектировать ее|Если карта смутит вас не забудьте проинспектировать ее");
            ui.SetString("tutorial_battle2_1_gamepad", "Вы также можете [Инспектировать] карты во время боев\n\r\n\rНажмите [<action=Inspect>] на [{0}] чтобы инспектировать ее|Если карта смутит вас не забудьте проинспектировать ее");
            ui.SetString("tutorial_battle2_2", "Поместите [{0}] на другую линию");
            ui.SetString("tutorial_battle2_3", "Перетащите [{0}] спереди вашего [Лидера]<sprite name=crown> чтобы защитить их от атаки!");
            ui.SetString("tutorial_battle2_4", "Передвижение ваших карт не тратит ход, так-что вы все еще можете сыграть карту");
            ui.SetString("tutorial_battle2_41a", "<size=0.35>[{0}] имеет [Шквал], что означает он будет атаковать [всех целей на линии]\n\r\n\rСейчас, атака [{0}]  ударит [обоих] ваших персонажуй!\n\r\n\rПопробуйте переместить [{1}] на другую линию так чтобы [{0}] ударил только их");
            ui.SetString("tutorial_battle2_41b", "<size=0.35>[{0}] имеет [Шквал], что означает он будет атаковать [всех целей на линии]\n\r\n\rСейчас, атака [{0}]  ударит [обоих] ваших персонажуй!\n\r\n\rПопробуйте переместить вашего [Лидера]<sprite name=crown> на другую линию так чтобы [{0}] ударил только их");
            ui.SetString("tutorial_battle2_5", "Вы можете [Отозвать] ваших [Компаньонов] чтобы вылечить их\n\r\n\rПеретащите [{0}] в ваш [Карман сброса]|Вы не можете отозвать своего лидера!");
            ui.SetString("tutorial_battle2_6", "Отзыв карт также не тратит ход!");
            ui.SetString("tutorial_battle3_1", "<size=0.35>Обычно выставить ваших [Компаньонов] на [Поле боя] как можно быстрее хорошая идея!\n\r\n\rИх [Счетчик]<sprite name=counter> начнет падать только на [Поле боя]");
            ui.SetString("tutorial_unplayable_crown_card", "Ваша [Коронованная карта] не может быть сыграна!\n\r\n\rНажмите [Колокол раздачи] чтобы продолжить...");
            ui.SetString("tutorial_retry_or_skip", "Пропустить обучение?|Кажется вы не завершили обучение!\n\r\n\rВы хотите пропустить его или попробовать еще раз?");
                //ui.SetString("tutorial_town", "Пропустить обучение?|Кажется вы не завершили обучение!\n\r\n\rВы хотите пропустить его или попробовать еще раз?");

             #endregion
         
            ui.SetString("language_this", "Русский язык (by Miya)");
        }

        public const string Woodhead_name = "Пнеголовый";


        protected override void Unload()
        {
            base.Unload();
        }
    }
}