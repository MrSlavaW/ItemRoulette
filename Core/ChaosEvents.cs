using System;
using System.Collections.Generic;
using ItemRoulette.Compat;
using ItemRoulette.Configuration;
using RoR2;
using RoR2.Artifacts;
using UnityEngine;
using UnityEngine.Networking;

namespace ItemRoulette.Core
{
    public enum ChaosPolarity
    {
        None,
        Good,
        Bad
    }

    public enum ChaosEventType
    {
        None,

        // Good Events (13)
        DoubleItem,
        TemporaryDuplicates,
        SpectralDuplicates,
        QualityCashback,
        DoubleGold,
        NextStageFreeRolls,
        KillEnemies30,
        AllyMirror,
        Heal50Percent,
        FriendlyClone,
        ElementalAspectDrop,
        TierOddsBoost,
        AlliedDroneSquad,
        ForgeAnomaly,

        // Bad Events (15)
        LoseGold30,
        CostIncrease10,
        RandomLunarOrVoid,
        TierDownSacrifice,
        NextSpinFiftyFifty,
        HallowedCurse,
        DamageHp30,
        ConvertHpToBarrier,
        EnemyItemMimic,
        EvilClone,
        RandomStageArtifact,
        CombatShrineX2,
        LowGravityMoonX2,
        TierOddsPenalty,
        HostileDroneSquad,
        VoidCorrosion
    }

    public class ChaosEventDefinition
    {
        public ChaosEventType Type { get; set; }
        public ChaosPolarity Polarity { get; set; }
        public string TitleRu { get; set; } = "";
        public string TitleEn { get; set; } = "";
        public string DescriptionRu { get; set; } = "";
        public string DescriptionEn { get; set; } = "";

        public string Title
        {
            get
            {
                string token = $"ITEMROULETTE_CHAOS_{Type}_TITLE";
                return Localization.Get(token, TitleRu, TitleEn);
            }
            set { TitleRu = value; TitleEn = value; }
        }

        public string Description
        {
            get
            {
                string token = $"ITEMROULETTE_CHAOS_{Type}_DESC";
                return Localization.Get(token, DescriptionRu, DescriptionEn);
            }
        }

        public void SetDescription(string ru, string en)
        {
            DescriptionRu = ru;
            DescriptionEn = en;
        }

        public void SetDescriptionFromToken(string token, params object[] args)
        {
            DescriptionRu = Localization.FormatForLang("ru", token, args);
            DescriptionEn = Localization.FormatForLang("en", token, args);
        }

        public Color Color { get; set; }
        public string SoundName { get; set; } = "";

        /// <summary>
        /// Shallow copy used for per-execution results, so that the shared static definitions
        /// are never mutated by dynamic descriptions.
        /// </summary>
        public ChaosEventDefinition Clone()
        {
            return (ChaosEventDefinition)MemberwiseClone();
        }
    }

    public struct ChaosChances
    {
        public float GoodPercent;
        public float NeutralPercent;
        public float BadPercent;

        public ChaosChances(float good, float neutral, float bad)
        {
            GoodPercent = good;
            NeutralPercent = neutral;
            BadPercent = bad;
        }
    }

    public static class ChaosEventManager
    {
        public static readonly Color ColorGood = new Color(0.30f, 0.90f, 0.45f, 1f); // Emerald / Gold green
        public static readonly Color ColorBad = new Color(0.95f, 0.28f, 0.35f, 1f);  // Crimson / Void red
        public static readonly Color ColorNeutral = new Color(0.70f, 0.72f, 0.80f, 1f);

        private static readonly Dictionary<ChaosEventType, ChaosEventDefinition> _definitions =
            new Dictionary<ChaosEventType, ChaosEventDefinition>();

        public static void PushLocalPickupNotification(CharacterMaster? master, PickupIndex pickupIndex, bool isRental = false)
        {
            if (master == null || pickupIndex == PickupIndex.none) return;
            try
            {
                bool isLocal = NetworkHelper.IsLocalMaster(master);
                if (isLocal)
                {
                    CharacterMasterNotificationQueue.PushPickupNotification(master, new UniquePickup { pickupIndex = pickupIndex, isRental = isRental });
                }
            }
            catch { }
        }

        static ChaosEventManager()
        {
            RegisterDefinitions();
        }

        private static void RegisterDefinitions()
        {
            // --- 10 ПОЛОЖИТЕЛЬНЫХ СОБЫТИЙ ---
            _definitions[ChaosEventType.DoubleItem] = new ChaosEventDefinition
            {
                Type = ChaosEventType.DoubleItem,
                Polarity = ChaosPolarity.Good,
                TitleRu = "Удвоение предмета",
                TitleEn = "Item Duplication",
                DescriptionRu = "Получена вторая копия выигранного предмета!",
                DescriptionEn = "Received a duplicate copy of the won item!",
                Color = ColorGood,
                SoundName = "Play_UI_coin_collect"
            };

            _definitions[ChaosEventType.TemporaryDuplicates] = new ChaosEventDefinition
            {
                Type = ChaosEventType.TemporaryDuplicates,
                Polarity = ChaosPolarity.Good,
                TitleRu = "Временный арсенал",
                TitleEn = "Temporary Arsenal",
                DescriptionRu = "Получено +10 временных копий предмета на 60 секунд!",
                DescriptionEn = "Granted +10 temporary copies of the item for 60 seconds!",
                Color = ColorGood,
                SoundName = "Play_UI_coin_collect"
            };

            _definitions[ChaosEventType.SpectralDuplicates] = new ChaosEventDefinition
            {
                Type = ChaosEventType.SpectralDuplicates,
                Polarity = ChaosPolarity.Good,
                TitleRu = "Спектральный резонанс",
                TitleEn = "Spectral Resonance",
                DescriptionRu = "Получено +5 спектральных копий предмета до конца этапа!",
                DescriptionEn = "Granted +5 spectral copies of the item until the end of the stage!",
                Color = ColorGood,
                SoundName = "Play_UI_coin_collect"
            };

            _definitions[ChaosEventType.QualityCashback] = new ChaosEventDefinition
            {
                Type = ChaosEventType.QualityCashback,
                Polarity = ChaosPolarity.Good,
                TitleRu = "Кэшбэк по качеству",
                TitleEn = "Rarity Cashback",
                DescriptionRu = "Возврат стоимости крутки с бонусом за редкость!",
                DescriptionEn = "Spin cost refunded with a bonus based on item rarity!",
                Color = ColorGood,
                SoundName = "Play_UI_coin_collect"
            };

            _definitions[ChaosEventType.DoubleGold] = new ChaosEventDefinition
            {
                Type = ChaosEventType.DoubleGold,
                Polarity = ChaosPolarity.Good,
                TitleRu = "Золотой джекпот",
                TitleEn = "Gold Jackpot",
                DescriptionRu = "Баланс золота увеличен в 2 раза!",
                DescriptionEn = "Current gold balance doubled!",
                Color = ColorGood,
                SoundName = "Play_UI_coin_collect"
            };

            _definitions[ChaosEventType.NextStageFreeRolls] = new ChaosEventDefinition
            {
                Type = ChaosEventType.NextStageFreeRolls,
                Polarity = ChaosPolarity.Good,
                TitleRu = "Аванс фортуны",
                TitleEn = "Fortune Advance",
                DescriptionRu = "+3 бесплатные крутки рулетки на следующем этапе!",
                DescriptionEn = "+3 free roulette spins on the next stage!",
                Color = ColorGood,
                SoundName = "Play_UI_coin_collect"
            };

            _definitions[ChaosEventType.KillEnemies30] = new ChaosEventDefinition
            {
                Type = ChaosEventType.KillEnemies30,
                Polarity = ChaosPolarity.Good,
                TitleRu = "Орбитальная зачистка",
                TitleEn = "Orbital Sweep",
                DescriptionRu = "30% монстров на карте мгновенно уничтожено!",
                DescriptionEn = "30% of monsters on the map instantly destroyed!",
                Color = ColorGood,
                SoundName = "Play_UI_coin_collect"
            };

            _definitions[ChaosEventType.AllyMirror] = new ChaosEventDefinition
            {
                Type = ChaosEventType.AllyMirror,
                Polarity = ChaosPolarity.Good,
                TitleRu = "Братская щедрость",
                TitleEn = "Ally Mirror",
                DescriptionRu = "Скопирован предмет того же тира у напарника!",
                DescriptionEn = "Copied a matching-tier item from an ally's inventory!",
                Color = ColorGood,
                SoundName = "Play_UI_coin_collect"
            };

            _definitions[ChaosEventType.Heal50Percent] = new ChaosEventDefinition
            {
                Type = ChaosEventType.Heal50Percent,
                Polarity = ChaosPolarity.Good,
                TitleRu = "Благодать исцеления",
                TitleEn = "Healing Grace",
                DescriptionRu = "Восстановлено 50% максимального здоровья!",
                DescriptionEn = "Restored 50% of maximum health!",
                Color = ColorGood,
                SoundName = "Play_UI_coin_collect"
            };

            _definitions[ChaosEventType.FriendlyClone] = new ChaosEventDefinition
            {
                Type = ChaosEventType.FriendlyClone,
                Polarity = ChaosPolarity.Good,
                TitleRu = "Фантомный соратник",
                TitleEn = "Phantom Ally",
                DescriptionRu = "Призван дружественный клон-доппельгангер!",
                DescriptionEn = "Summoned a friendly doppelganger clone!",
                Color = ColorGood,
                SoundName = "Play_UI_coin_collect"
            };

            _definitions[ChaosEventType.ElementalAspectDrop] = new ChaosEventDefinition
            {
                Type = ChaosEventType.ElementalAspectDrop,
                Polarity = ChaosPolarity.Good,
                TitleRu = "Элементальная сфера",
                TitleEn = "Elemental Aspect",
                DescriptionRu = "Получен аспект элитного монстра (активка)!",
                DescriptionEn = "Acquired an elite monster aspect equipment!",
                Color = ColorGood,
                SoundName = "Play_UI_coin_collect"
            };

            _definitions[ChaosEventType.TierOddsBoost] = new ChaosEventDefinition
            {
                Type = ChaosEventType.TierOddsBoost,
                Polarity = ChaosPolarity.Good,
                TitleRu = "Благословение редкости",
                TitleEn = "Rarity Blessing",
                DescriptionRu = "Шансы выпадения зелёных, красных и босс-вещей повышены до конца этапа!",
                DescriptionEn = "Uncommon, Legendary, and Boss item odds boosted until stage end!",
                Color = ColorGood,
                SoundName = "Play_UI_coin_collect"
            };

            _definitions[ChaosEventType.AlliedDroneSquad] = new ChaosEventDefinition
            {
                Type = ChaosEventType.AlliedDroneSquad,
                Polarity = ChaosPolarity.Good,
                TitleRu = "Дроны поддержки",
                TitleEn = "Support Drones",
                DescriptionRu = "Призван отряд союзных дронов для поддержки в бою!",
                DescriptionEn = "Summoned a squad of allied combat support drones!",
                Color = ColorGood,
                SoundName = "Play_UI_coin_collect"
            };

            // --- 15 НЕГАТИВНЫХ СОБЫТИЙ ---
            _definitions[ChaosEventType.LoseGold30] = new ChaosEventDefinition
            {
                Type = ChaosEventType.LoseGold30,
                Polarity = ChaosPolarity.Bad,
                TitleRu = "Карманник",
                TitleEn = "Pickpocket",
                DescriptionRu = "Потеряно 30% золота из запасов!",
                DescriptionEn = "Lost 30% of current gold!",
                Color = ColorBad,
                SoundName = "Play_heretic_laugh"
            };

            _definitions[ChaosEventType.CostIncrease10] = new ChaosEventDefinition
            {
                Type = ChaosEventType.CostIncrease10,
                Polarity = ChaosPolarity.Bad,
                TitleRu = "Налог на азарт",
                TitleEn = "Gambling Tax",
                DescriptionRu = "Стоимость прокрутки повышена на +10%!",
                DescriptionEn = "Spin cost increased by +10%!",
                Color = ColorBad,
                SoundName = "Play_heretic_laugh"
            };

            _definitions[ChaosEventType.RandomLunarOrVoid] = new ChaosEventDefinition
            {
                Type = ChaosEventType.RandomLunarOrVoid,
                Polarity = ChaosPolarity.Bad,
                TitleRu = "Аномальная мутация",
                TitleEn = "Anomalous Mutation",
                DescriptionRu = "Запущена обязательная фиолетово-синяя крутка!",
                DescriptionEn = "Triggered mandatory Void/Lunar roulette spin!",
                Color = ColorBad,
                SoundName = "Play_heretic_laugh"
            };

            _definitions[ChaosEventType.TierDownSacrifice] = new ChaosEventDefinition
            {
                Type = ChaosEventType.TierDownSacrifice,
                Polarity = ChaosPolarity.Bad,
                TitleRu = "Нисходящая жертва",
                TitleEn = "Tier-Down Sacrifice",
                DescriptionRu = "Потерян предмет на тир ниже выигранного!",
                DescriptionEn = "Sacrificed an item one tier below the won item!",
                Color = ColorBad,
                SoundName = "Play_heretic_laugh"
            };

            _definitions[ChaosEventType.NextSpinFiftyFifty] = new ChaosEventDefinition
            {
                Type = ChaosEventType.NextSpinFiftyFifty,
                Polarity = ChaosPolarity.Bad,
                TitleRu = "Русская рулетка",
                TitleEn = "Russian Roulette",
                DescriptionRu = "Следующий спин будет 50% Удача / 50% Бедствие!",
                DescriptionEn = "Next spin odds forced to 50% Fortune / 50% Calamity!",
                Color = ColorBad,
                SoundName = "Play_heretic_laugh"
            };

            _definitions[ChaosEventType.HallowedCurse] = new ChaosEventDefinition
            {
                Type = ChaosEventType.HallowedCurse,
                Polarity = ChaosPolarity.Bad,
                TitleRu = "Проклятие Н'Куханы",
                TitleEn = "N'kuhana's Curse",
                DescriptionRu = "Получен предмет-проклятие!",
                DescriptionEn = "Inflicted with a cursed detrimental item!",
                Color = ColorBad,
                SoundName = "Play_heretic_laugh"
            };

            _definitions[ChaosEventType.DamageHp30] = new ChaosEventDefinition
            {
                Type = ChaosEventType.DamageHp30,
                Polarity = ChaosPolarity.Bad,
                TitleRu = "Ранение",
                TitleEn = "Sudden Wound",
                DescriptionRu = "Получен внезапный урон на 30% здоровья!",
                DescriptionEn = "Suffered sudden damage equal to 30% max health!",
                Color = ColorBad,
                SoundName = "Play_heretic_laugh"
            };

            _definitions[ChaosEventType.ConvertHpToBarrier] = new ChaosEventDefinition
            {
                Type = ChaosEventType.ConvertHpToBarrier,
                Polarity = ChaosPolarity.Bad,
                TitleRu = "Хрупкий барьер",
                TitleEn = "Fragile Barrier",
                DescriptionRu = "Здоровье снижено до 1 и обращено в тающий барьер!",
                DescriptionEn = "Health reduced to 1 and converted into a decaying barrier!",
                Color = ColorBad,
                SoundName = "Play_heretic_laugh"
            };

            _definitions[ChaosEventType.EnemyItemMimic] = new ChaosEventDefinition
            {
                Type = ChaosEventType.EnemyItemMimic,
                Polarity = ChaosPolarity.Bad,
                TitleRu = "Утечка арсенала",
                TitleEn = "Arsenal Leak",
                DescriptionRu = "Элитный монстр на карте поглотил 50% ваших предметов!",
                DescriptionEn = "An elite monster has absorbed 50% of your items!",
                Color = ColorBad,
                SoundName = "Play_heretic_laugh"
            };

            _definitions[ChaosEventType.EvilClone] = new ChaosEventDefinition
            {
                Type = ChaosEventType.EvilClone,
                Polarity = ChaosPolarity.Bad,
                TitleRu = "Теневой двойник",
                TitleEn = "Shadow Doppelganger",
                DescriptionRu = "Призван враждебный клон с вашим снаряжением!",
                DescriptionEn = "Summoned a hostile clone equipped with your loadout!",
                Color = ColorBad,
                SoundName = "Play_heretic_laugh"
            };

            _definitions[ChaosEventType.RandomStageArtifact] = new ChaosEventDefinition
            {
                Type = ChaosEventType.RandomStageArtifact,
                Polarity = ChaosPolarity.Bad,
                TitleRu = "Проклятие Артефакта",
                TitleEn = "Artifact Trial",
                DescriptionRu = "На текущий этап активирован случайный дебаф-артефакт!",
                DescriptionEn = "A random trial artifact has been activated for this stage!",
                Color = ColorBad,
                SoundName = "Play_heretic_laugh"
            };

            _definitions[ChaosEventType.CombatShrineX2] = new ChaosEventDefinition
            {
                Type = ChaosEventType.CombatShrineX2,
                Polarity = ChaosPolarity.Bad,
                TitleRu = "Алтарь битвы x2",
                TitleEn = "Combat Shrine x2",
                DescriptionRu = "Призвано двойное испытание монстров Алтаря Битвы (x2)!",
                DescriptionEn = "Summoned a double Combat Shrine monster challenge (x2)!",
                Color = ColorBad,
                SoundName = "Play_heretic_laugh"
            };

            _definitions[ChaosEventType.LowGravityMoonX2] = new ChaosEventDefinition
            {
                Type = ChaosEventType.LowGravityMoonX2,
                Polarity = ChaosPolarity.Bad,
                TitleRu = "Лунная гравитация x2",
                TitleEn = "Lunar Low Gravity x2",
                DescriptionRu = "Гравитация на текущем этапе снижена в 2 раза сильнее, чем на Луне (невесомость)!",
                DescriptionEn = "Stage gravity drastically lowered beyond Moon levels (near zero-g)!",
                Color = ColorBad,
                SoundName = "Play_heretic_laugh"
            };

            _definitions[ChaosEventType.TierOddsPenalty] = new ChaosEventDefinition
            {
                Type = ChaosEventType.TierOddsPenalty,
                Polarity = ChaosPolarity.Bad,
                TitleRu = "Проклятие ширпотреба",
                TitleEn = "Commoner's Curse",
                DescriptionRu = "Шансы выпадения зелёных, красных и босс-вещей понижены до конца этапа!",
                DescriptionEn = "Uncommon, Legendary, and Boss item odds penalized until stage end!",
                Color = ColorBad,
                SoundName = "Play_heretic_laugh"
            };

            _definitions[ChaosEventType.HostileDroneSquad] = new ChaosEventDefinition
            {
                Type = ChaosEventType.HostileDroneSquad,
                Polarity = ChaosPolarity.Bad,
                TitleRu = "Враждебные дроны",
                TitleEn = "Hostile Drones",
                DescriptionRu = "Призван отряд враждебных боевых дронов, атакующих вас!",
                DescriptionEn = "Summoned a squadron of rogue combat drones to hunt you down!",
                Color = ColorBad,
                SoundName = "Play_heretic_laugh"
            };

            _definitions[ChaosEventType.ForgeAnomaly] = new ChaosEventDefinition
            {
                Type = ChaosEventType.ForgeAnomaly,
                Polarity = ChaosPolarity.Good,
                TitleRu = "Кузнечная аномалия",
                TitleEn = "Forge Anomaly",
                DescriptionRu = "Перековывает предмет в инвентаре, повышая его качество на +1 ступень!",
                DescriptionEn = "Reforges an inventory item, upgrading its quality tier by +1!",
                Color = ColorGood,
                SoundName = "Play_ui_item_drop"
            };

            _definitions[ChaosEventType.VoidCorrosion] = new ChaosEventDefinition
            {
                Type = ChaosEventType.VoidCorrosion,
                Polarity = ChaosPolarity.Bad,
                TitleRu = "Коррозия пустоты",
                TitleEn = "Void Corrosion",
                DescriptionRu = "Энергия Пустоты разъедает качество одного из ваших артефактов!",
                DescriptionEn = "Void corruption degrades the quality tier of an item in your inventory!",
                Color = ColorBad,
                SoundName = "Play_nullifier_attack1_spawn"
            };
        }

        private static readonly Dictionary<ChaosEventType, ChaosEventDefinition> _lastResults = new Dictionary<ChaosEventType, ChaosEventDefinition>();

        /// <summary>
        /// Returns the result of the most recent execution of this event (with the dynamic description),
        /// or the shared static definition if the event has not been executed yet. Never mutates definitions.
        /// </summary>
        public static ChaosEventDefinition? GetDisplayDefinition(ChaosEventType type)
        {
            if (_lastResults.TryGetValue(type, out var res)) return res;
            return GetDefinition(type);
        }

        public static ChaosEventDefinition? GetDefinition(ChaosEventType type)
        {
            if (_definitions.TryGetValue(type, out var def))
            {
                return def;
            }
            return null;
        }

        public static bool IsEventAllowed(ChaosEventType type)
        {
            return ModConfig.IsChaosEventAllowed(type);
        }

        public static bool HasAnyGoodEventsEnabled()
        {
            if (ModConfig.AllowEventDoubleItem.Value ||
                ModConfig.AllowEventTemporaryDuplicates.Value ||
                ModConfig.AllowEventSpectralDuplicates.Value ||
                ModConfig.AllowEventQualityCashback.Value ||
                ModConfig.AllowEventDoubleGold.Value ||
                ModConfig.AllowEventNextStageFreeRolls.Value ||
                ModConfig.AllowEventKillEnemies30.Value ||
                ModConfig.AllowEventAllyMirror.Value ||
                ModConfig.AllowEventHeal50Percent.Value ||
                ModConfig.AllowEventFriendlyClone.Value ||
                ModConfig.AllowEventElementalAspectDrop.Value ||
                ModConfig.AllowEventTierOddsBoost.Value ||
                ModConfig.AllowEventAlliedDroneSquad.Value ||
                (QualityCompat.IsQualityLoaded && ModConfig.AllowEventForgeAnomaly.Value))
            {
                return true;
            }
            return false;
        }

        public static bool HasAnyBadEventsEnabled()
        {
            if (ModConfig.AllowEventLoseGold30.Value ||
                ModConfig.AllowEventCostIncrease10.Value ||
                ModConfig.AllowEventRandomLunarOrVoid.Value ||
                ModConfig.AllowEventTierDownSacrifice.Value ||
                ModConfig.AllowEventNextSpinFiftyFifty.Value ||
                ModConfig.AllowEventHallowedCurse.Value ||
                ModConfig.AllowEventDamageHp30.Value ||
                ModConfig.AllowEventConvertHpToBarrier.Value ||
                ModConfig.AllowEventEnemyItemMimic.Value ||
                ModConfig.AllowEventEvilClone.Value ||
                ModConfig.AllowEventRandomStageArtifact.Value ||
                ModConfig.AllowEventCombatShrineX2.Value ||
                ModConfig.AllowEventLowGravityMoonX2.Value ||
                ModConfig.AllowEventTierOddsPenalty.Value ||
                ModConfig.AllowEventHostileDroneSquad.Value ||
                (QualityCompat.IsQualityLoaded && ModConfig.AllowEventVoidCorrosion.Value))
            {
                return true;
            }
            return false;
        }

        public static ChaosChances CalculateChances(CharacterMaster? master, bool fiftyFiftyActive = false)
        {
            if (!ModConfig.IsChaosSystemActive || master == null || master.inventory == null)
            {
                return new ChaosChances(0f, 100f, 0f);
            }

            bool hasGood = HasAnyGoodEventsEnabled();
            bool hasBad = HasAnyBadEventsEnabled();

            if (fiftyFiftyActive)
            {
                if (!hasGood && !hasBad)
                {
                    return new ChaosChances(0f, 100f, 0f);
                }
                if (!hasGood)
                {
                    return new ChaosChances(0f, 0f, 100f);
                }
                if (!hasBad)
                {
                    return new ChaosChances(100f, 0f, 0f);
                }
                return new ChaosChances(50f, 0f, 50f);
            }

            if (!hasGood && !hasBad)
            {
                return new ChaosChances(0f, 100f, 0f);
            }

            int lunarVoidCount = 0;
            int cloverCount = 0;
            int purityCount = 0;

            try
            {
                cloverCount = master.inventory.GetItemCountEffective(RoR2Content.Items.Clover.itemIndex);
                purityCount = master.inventory.GetItemCountEffective(RoR2Content.Items.LunarBadLuck.itemIndex);

                foreach (ItemIndex itemIndex in master.inventory.itemAcquisitionOrder)
                {
                    ItemDef itemDef = ItemCatalog.GetItemDef(itemIndex);
                    if (itemDef == null) continue;
                    int count = master.inventory.GetItemCountEffective(itemIndex);
                    if (count <= 0) continue;

                    if (itemDef.tier == ItemTier.Lunar)
                    {
                        lunarVoidCount += count;
                    }
                    else if (itemDef.tier == ItemTier.VoidTier1 ||
                             itemDef.tier == ItemTier.VoidTier2 ||
                             itemDef.tier == ItemTier.VoidTier3 ||
                             itemDef.tier == ItemTier.VoidBoss)
                    {
                        lunarVoidCount += count;
                    }
                }
            }
            catch { }

            float catalystBonus = Mathf.Min(
                ModConfig.MaxLunarVoidBonusCap.Value,
                lunarVoidCount * ModConfig.LunarVoidChancePerItem.Value
            );

            float good = hasGood ? (ModConfig.BaseGoodEventChance.Value + catalystBonus + (cloverCount * ModConfig.CloverBonusPerItem.Value)) : 0f;
            float bad = hasBad ? (ModConfig.BaseBadEventChance.Value + catalystBonus + (purityCount * ModConfig.PurityPenaltyPerItem.Value)) : 0f;

            good = Mathf.Max(0f, good);
            bad = Mathf.Max(0f, bad);

            float neutral;
            if (good + bad >= 100f)
            {
                float total = good + bad;
                good = (good / total) * 100f;
                bad = (bad / total) * 100f;
                neutral = 0f;
            }
            else
            {
                neutral = 100f - (good + bad);
            }

            return new ChaosChances(good, neutral, bad);
        }

        public static ChaosEventType RollEvent(ChaosChances chances, global::Xoroshiro128Plus? rng = null)
        {
            if (!ModConfig.IsChaosSystemActive) return ChaosEventType.None;

            float roll = rng != null ? rng.RangeFloat(0f, 100f) : UnityEngine.Random.Range(0f, 100f);

            if (roll < chances.GoodPercent)
            {
                var goods = new List<ChaosEventType>();
                if (ModConfig.AllowEventDoubleItem.Value) goods.Add(ChaosEventType.DoubleItem);
                if (ModConfig.AllowEventTemporaryDuplicates.Value) goods.Add(ChaosEventType.TemporaryDuplicates);
                if (ModConfig.AllowEventSpectralDuplicates.Value) goods.Add(ChaosEventType.SpectralDuplicates);
                if (ModConfig.AllowEventQualityCashback.Value) goods.Add(ChaosEventType.QualityCashback);
                if (ModConfig.AllowEventDoubleGold.Value) goods.Add(ChaosEventType.DoubleGold);
                if (ModConfig.AllowEventNextStageFreeRolls.Value) goods.Add(ChaosEventType.NextStageFreeRolls);
                if (ModConfig.AllowEventKillEnemies30.Value) goods.Add(ChaosEventType.KillEnemies30);
                if (ModConfig.AllowEventAllyMirror.Value) goods.Add(ChaosEventType.AllyMirror);
                if (ModConfig.AllowEventHeal50Percent.Value) goods.Add(ChaosEventType.Heal50Percent);
                if (ModConfig.AllowEventFriendlyClone.Value) goods.Add(ChaosEventType.FriendlyClone);
                if (ModConfig.AllowEventElementalAspectDrop.Value) goods.Add(ChaosEventType.ElementalAspectDrop);
                if (ModConfig.AllowEventTierOddsBoost.Value) goods.Add(ChaosEventType.TierOddsBoost);
                if (ModConfig.AllowEventAlliedDroneSquad.Value) goods.Add(ChaosEventType.AlliedDroneSquad);
                if (QualityCompat.IsQualityLoaded && ModConfig.AllowEventForgeAnomaly.Value)
                {
                    goods.Add(ChaosEventType.ForgeAnomaly);
                }

                if (goods.Count == 0) return ChaosEventType.None;
                int idx = rng != null ? rng.RangeInt(0, goods.Count) : UnityEngine.Random.Range(0, goods.Count);
                return goods[idx];
            }
            else if (roll < chances.GoodPercent + chances.BadPercent)
            {
                var bads = new List<ChaosEventType>();
                if (ModConfig.AllowEventLoseGold30.Value) bads.Add(ChaosEventType.LoseGold30);
                if (ModConfig.AllowEventCostIncrease10.Value) bads.Add(ChaosEventType.CostIncrease10);
                if (ModConfig.AllowEventRandomLunarOrVoid.Value) bads.Add(ChaosEventType.RandomLunarOrVoid);
                if (ModConfig.AllowEventTierDownSacrifice.Value) bads.Add(ChaosEventType.TierDownSacrifice);
                if (ModConfig.AllowEventNextSpinFiftyFifty.Value) bads.Add(ChaosEventType.NextSpinFiftyFifty);
                if (ModConfig.AllowEventHallowedCurse.Value) bads.Add(ChaosEventType.HallowedCurse);
                if (ModConfig.AllowEventDamageHp30.Value) bads.Add(ChaosEventType.DamageHp30);
                if (ModConfig.AllowEventConvertHpToBarrier.Value) bads.Add(ChaosEventType.ConvertHpToBarrier);
                if (ModConfig.AllowEventEnemyItemMimic.Value) bads.Add(ChaosEventType.EnemyItemMimic);
                if (ModConfig.AllowEventEvilClone.Value) bads.Add(ChaosEventType.EvilClone);
                if (ModConfig.AllowEventRandomStageArtifact.Value) bads.Add(ChaosEventType.RandomStageArtifact);
                if (ModConfig.AllowEventCombatShrineX2.Value) bads.Add(ChaosEventType.CombatShrineX2);
                if (ModConfig.AllowEventLowGravityMoonX2.Value) bads.Add(ChaosEventType.LowGravityMoonX2);
                if (ModConfig.AllowEventTierOddsPenalty.Value) bads.Add(ChaosEventType.TierOddsPenalty);
                if (ModConfig.AllowEventHostileDroneSquad.Value) bads.Add(ChaosEventType.HostileDroneSquad);
                if (QualityCompat.IsQualityLoaded && ModConfig.AllowEventVoidCorrosion.Value)
                {
                    bads.Add(ChaosEventType.VoidCorrosion);
                }

                if (bads.Count == 0) return ChaosEventType.None;
                int idx = rng != null ? rng.RangeInt(0, bads.Count) : UnityEngine.Random.Range(0, bads.Count);
                return bads[idx];
            }

            return ChaosEventType.None;
        }

        public static void ExecuteEvent(CharacterMaster master, ChaosEventType eventType, RouletteItemEntry wonItem, int currentRollCost)
        {
            if (!UnityEngine.Networking.NetworkServer.active) return;
            if (!ModConfig.IsChaosSystemActive || master == null || eventType == ChaosEventType.None) return;

            var baseDef = GetDefinition(eventType);
            if (baseDef == null) return;
            var def = baseDef.Clone();
            _lastResults[eventType] = def;

            try
            {
                switch (eventType)
                {
                    case ChaosEventType.DoubleItem:
                        {
                            ItemIndex targetItem = GetValidTargetItem(master, wonItem);
                            if (targetItem != ItemIndex.None)
                            {
                                master.inventory.GiveItemPermanent(targetItem, 1);
                                PushLocalPickupNotification(master, PickupCatalog.FindPickupIndex(targetItem));
                                string itemName = GetItemName(targetItem);
                                def.SetDescriptionFromToken("ITEMROULETTE_CHAOS_DOUBLE_ITEM_RESULT", itemName);
                            }
                        }
                        break;

                    case ChaosEventType.TemporaryDuplicates:
                        {
                            ItemIndex targetItem = GetValidTargetItem(master, wonItem);
                            if (targetItem != ItemIndex.None)
                            {
                                RouletteManager.Instance.GrantTemporaryItems(master, targetItem, 10, 60f);
                                string itemName = GetItemName(targetItem);
                                def.SetDescriptionFromToken("ITEMROULETTE_CHAOS_TEMP_DUPLICATES_RESULT", itemName);
                            }
                        }
                        break;

                    case ChaosEventType.SpectralDuplicates:
                        {
                            ItemIndex targetItem = GetValidTargetItem(master, wonItem);
                            if (targetItem != ItemIndex.None)
                            {
                                RouletteManager.Instance.GrantSpectralItems(master, targetItem, 5);
                                string itemName = GetItemName(targetItem);
                                def.SetDescriptionFromToken("ITEMROULETTE_CHAOS_SPECTRAL_DUPLICATES_RESULT", itemName);
                            }
                        }
                        break;

                    case ChaosEventType.QualityCashback:
                        int multiplier = 2;
                        switch (wonItem.TierType)
                        {
                            case RouletteTierType.Common: multiplier = 2; break;
                            case RouletteTierType.Uncommon: multiplier = 4; break;
                            case RouletteTierType.Legendary: multiplier = 6; break;
                            case RouletteTierType.Boss: multiplier = 8; break;
                            case RouletteTierType.ZetAspects: multiplier = 8; break;
                            case RouletteTierType.VanillaAspect: multiplier = 8; break;
                            case RouletteTierType.Equipment: multiplier = 3; break;
                            case RouletteTierType.QualityEquipment: multiplier = 4; break;
                            default: multiplier = 2; break;
                        }
                        uint cashback = (uint)(currentRollCost * multiplier);
                        master.GiveMoney(cashback, true);
                        def.SetDescriptionFromToken("ITEMROULETTE_CHAOS_QUALITY_CASHBACK_RESULT", cashback, multiplier, wonItem.TierLabel);
                        break;

                    case ChaosEventType.DoubleGold:
                        uint doubleAmount = master.money;
                        master.GiveMoney(doubleAmount, true);
                        def.SetDescriptionFromToken("ITEMROULETTE_CHAOS_DOUBLE_GOLD_RESULT", doubleAmount);
                        break;

                    case ChaosEventType.NextStageFreeRolls:
                        RouletteManager.GetServerState(master).PendingFreeRollsNextStage += 3;
                        if (NetworkHelper.IsLocalMaster(master)) RouletteManager.Instance.PendingFreeRollsNextStage += 3;
                        break;

                    case ChaosEventType.KillEnemies30:
                        KillMapMonstersPercent(master, 0.30f);
                        break;

                    case ChaosEventType.AllyMirror:
                        string mirroredName = MirrorAllyItem(master, wonItem.TierType);
                        if (!string.IsNullOrEmpty(mirroredName))
                        {
                            def.SetDescriptionFromToken("ITEMROULETTE_CHAOS_ALLY_MIRROR_RESULT", mirroredName);
                        }
                        else
                        {
                            def.SetDescriptionFromToken("ITEMROULETTE_CHAOS_ALLY_MIRROR_DESC");
                        }
                        break;

                    case ChaosEventType.Heal50Percent:
                        var bodyHeal = master.GetBody();
                        if (bodyHeal != null && bodyHeal.healthComponent != null)
                        {
                            bodyHeal.healthComponent.HealFraction(0.50f, default);
                        }
                        break;

                    case ChaosEventType.FriendlyClone:
                        SpawnDoppelganger(master, TeamIndex.Player);
                        break;

                    case ChaosEventType.ElementalAspectDrop:
                        ExecuteElementalAspect(master, def);
                        break;

                    case ChaosEventType.TierOddsBoost:
                        ExecuteTierOddsBoost(master, def);
                        break;

                    case ChaosEventType.AlliedDroneSquad:
                        ExecuteAlliedDrones(master, def);
                        break;

                    case ChaosEventType.ForgeAnomaly:
                        bool upgraded = QualityCompat.UpgradeItemQualityInInventory(master, out string upName, out QualityTierCompat newTier);
                        if (upgraded)
                        {
                            string qName = QualityCompat.GetQualityName(newTier);
                            Color qCol = QualityCompat.GetQualityColor(newTier);
                            string qHex = ColorUtility.ToHtmlStringRGB(qCol);
                            def.SetDescriptionFromToken("ITEMROULETTE_CHAOS_FORGE_ANOMALY_RESULT", upName, $"<color=#{qHex}><b>{qName}</b></color>");
                        }
                        else
                        {
                            def.SetDescriptionFromToken("ITEMROULETTE_CHAOS_FORGE_ANOMALY_EMPTY");
                        }
                        break;

                    // Bad Events
                    case ChaosEventType.LoseGold30:
                        uint lost = (uint)(master.money * 0.30f);
                        master.money -= lost;
                        def.SetDescriptionFromToken("ITEMROULETTE_CHAOS_LOSE_GOLD_RESULT", lost);
                        break;

                    case ChaosEventType.CostIncrease10:
                        RouletteManager.GetServerState(master).ChaosCostPenaltyMultiplier += 0.10f;
                        if (NetworkHelper.IsLocalMaster(master)) RouletteManager.Instance.ChaosCostPenaltyMultiplier += 0.10f;
                        break;

                    case ChaosEventType.RandomLunarOrVoid:
                        TriggerMandatoryLunarVoidSpinServer(master);
                        def.SetDescriptionFromToken("ITEMROULETTE_CHAOS_RANDOM_LUNAR_VOID_DESC");
                        break;

                    case ChaosEventType.TierDownSacrifice:
                        string sacName = SacrificeTierDownItem(master, wonItem.TierType);
                        if (!string.IsNullOrEmpty(sacName))
                        {
                            def.SetDescriptionFromToken("ITEMROULETTE_CHAOS_TIER_DOWN_SACRIFICE_RESULT", sacName);
                        }
                        else
                        {
                            def.SetDescriptionFromToken("ITEMROULETTE_CHAOS_TIER_DOWN_SACRIFICE_EMPTY");
                        }
                        break;

                    case ChaosEventType.NextSpinFiftyFifty:
                        RouletteManager.GetServerState(master).FiftyFiftyActive = true;
                        if (NetworkHelper.IsLocalMaster(master)) RouletteManager.Instance.FiftyFiftyActive = true;
                        break;

                    case ChaosEventType.HallowedCurse:
                        string curseName = GrantHallowedOrCurseItem(master);
                        if (!string.IsNullOrEmpty(curseName))
                        {
                            def.SetDescriptionFromToken("ITEMROULETTE_CHAOS_HALLOWED_CURSE_RESULT", curseName);
                        }
                        else
                        {
                            def.SetDescriptionFromToken("ITEMROULETTE_CHAOS_HALLOWED_CURSE_DESC");
                        }
                        break;

                    case ChaosEventType.DamageHp30:
                        var bodyDmg = master.GetBody();
                        if (bodyDmg != null && bodyDmg.healthComponent != null)
                        {
                            var hc = bodyDmg.healthComponent;
                            if (hc.alive && hc.combinedHealth > 1f)
                            {
                                float maxDmg = Mathf.Max(0f, hc.combinedHealth - 1f);
                                float dmg = Mathf.Min(hc.combinedHealth * 0.30f, maxDmg);
                                if (dmg > 0f)
                                {
                                    hc.TakeDamage(new DamageInfo
                                    {
                                        damage = dmg,
                                        position = bodyDmg.corePosition,
                                        damageType = DamageType.NonLethal
                                    });
                                }
                            }
                        }
                        break;

                    case ChaosEventType.ConvertHpToBarrier:
                        var bodyBarrier = master.GetBody();
                        if (bodyBarrier != null && bodyBarrier.healthComponent != null)
                        {
                            float curHp = bodyBarrier.healthComponent.health;
                            if (curHp > 1f)
                            {
                                float converted = curHp - 1f;
                                bodyBarrier.healthComponent.health = 1f;
                                bodyBarrier.healthComponent.barrier = Mathf.Min(
                                    bodyBarrier.healthComponent.fullBarrier,
                                    bodyBarrier.healthComponent.barrier + converted
                                );
                            }
                        }
                        break;

                    case ChaosEventType.EnemyItemMimic:
                        MimicItemsToEnemy(master);
                        break;

                    case ChaosEventType.EvilClone:
                        SpawnDoppelganger(master, TeamIndex.Monster);
                        break;

                    case ChaosEventType.RandomStageArtifact:
                        ExecuteRandomStageArtifact(master, def);
                        break;

                    case ChaosEventType.CombatShrineX2:
                        ExecuteCombatShrineX2(master, def);
                        break;

                    case ChaosEventType.LowGravityMoonX2:
                        ExecuteLowGravityMoonX2(master, def);
                        break;

                    case ChaosEventType.TierOddsPenalty:
                        ExecuteTierOddsPenalty(master, def);
                        break;

                    case ChaosEventType.HostileDroneSquad:
                        ExecuteHostileDrones(master, def);
                        break;

                    case ChaosEventType.VoidCorrosion:
                        bool downgraded = QualityCompat.DowngradeItemQualityInInventory(master, out string downName, out QualityTierCompat oldTier, out QualityTierCompat toTier);
                        if (downgraded)
                        {
                            string targetTierStr = toTier == QualityTierCompat.None
                                ? Localization.Get("ITEMROULETTE_WORD_ITEM", "Базовое", "Base")
                                : QualityCompat.GetQualityName(toTier);
                            string oldTierStr = QualityCompat.GetQualityName(oldTier);
                            def.SetDescriptionFromToken("ITEMROULETTE_CHAOS_VOID_CORROSION_RESULT", downName, oldTierStr, targetTierStr);
                        }
                        else
                        {
                            def.SetDescriptionFromToken("ITEMROULETTE_CHAOS_VOID_CORROSION_EMPTY");
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemRoulette] Exception executing Chaos event {eventType}: {ex}");
            }

            // Broadcast message to chat
            try
            {
                string colorHex = def.Polarity == ChaosPolarity.Good ? "#4ADE80" : "#F87171";
                string typeLabel = def.Polarity == ChaosPolarity.Good ? Localization.Get("УДАЧА", "FORTUNE") : Localization.Get("БЕДСТВИЕ", "CALAMITY");
                string rouletteTag = Localization.Get("Рулетка", "Roulette");
                string initiatorName = PlayerUtil.GetMasterDisplayName(master);
                ServerEvents.Broadcast($"<color={colorHex}>[{rouletteTag} | {typeLabel}] <b>{def.Title}</b>: {def.Description}</color>", initiatorName);
            }
            catch { }
        }

        private static void KillMapMonstersPercent(CharacterMaster attacker, float percent)
        {
            var monsterTeam = TeamComponent.GetTeamMembers(TeamIndex.Monster);
            var eligible = new List<HealthComponent>();
            foreach (var member in monsterTeam)
            {
                if (member == null || member.body == null || member.body.healthComponent == null) continue;
                if (!member.body.healthComponent.alive) continue;
                if (member.body.isBoss) continue;
                eligible.Add(member.body.healthComponent);
            }

            int toKill = Mathf.CeilToInt(eligible.Count * percent);
            var attackerObj = attacker.GetBodyObject();
            for (int i = 0; i < toKill && eligible.Count > 0; i++)
            {
                int idx = UnityEngine.Random.Range(0, eligible.Count);
                var target = eligible[idx];
                target.Suicide(attackerObj, attackerObj, DamageType.Generic);
                eligible.RemoveAt(idx);
            }
        }

        private static ItemIndex GetValidTargetItem(CharacterMaster master, RouletteItemEntry? wonItem)
        {
            if (wonItem?.PickupDef != null && wonItem.PickupDef.itemIndex != ItemIndex.None)
            {
                var def = ItemCatalog.GetItemDef(wonItem.PickupDef.itemIndex);
                if (def != null && !def.hidden && def.tier != ItemTier.NoTier)
                {
                    return wonItem.PickupDef.itemIndex;
                }
            }

            // Fallback 1: check master's inventory for real, visible items
            if (master != null && master.inventory != null)
            {
                var validItems = new List<ItemIndex>();
                foreach (var idx in master.inventory.itemAcquisitionOrder)
                {
                    var def = ItemCatalog.GetItemDef(idx);
                    if (def != null && !def.hidden && def.tier != ItemTier.NoTier && master.inventory.GetItemCountEffective(idx) > 0)
                    {
                        validItems.Add(idx);
                    }
                }
                if (validItems.Count > 0)
                {
                    return validItems[UnityEngine.Random.Range(0, validItems.Count)];
                }
            }

            // Fallback 2: roll a real item from the run's item pool
            var roll = ItemPoolHelper.RollRandomEntry(master: master);
            if (roll?.PickupDef != null && roll.PickupDef.itemIndex != ItemIndex.None)
            {
                var def = ItemCatalog.GetItemDef(roll.PickupDef.itemIndex);
                if (def != null && !def.hidden && def.tier != ItemTier.NoTier)
                {
                    return roll.PickupDef.itemIndex;
                }
            }

            return RoR2Content.Items.Syringe.itemIndex;
        }

        public static string GetItemName(ItemIndex itemIndex)
        {
            if (itemIndex == ItemIndex.None) return Localization.Get("Неизвестный предмет", "Unknown Item");
            try
            {
                var def = ItemCatalog.GetItemDef(itemIndex);
                if (def != null)
                {
                    if (!string.IsNullOrEmpty(def.nameToken))
                    {
                        string name = Language.GetString(def.nameToken);
                        if (!string.IsNullOrEmpty(name) && name != def.nameToken)
                        {
                            return System.Text.RegularExpressions.Regex.Replace(name, @"<sprite\b[^>]*>", "").Trim();
                        }
                    }
                    if (!string.IsNullOrEmpty(def.name) && !def.name.StartsWith("ITEM_", StringComparison.OrdinalIgnoreCase))
                    {
                        return def.name;
                    }
                }
                return Localization.Get("Предмет", "Item");
            }
            catch
            {
                return Localization.Get("Предмет", "Item");
            }
        }

        private static string MirrorAllyItem(CharacterMaster master, RouletteTierType tier)
        {
            var otherMasters = new List<CharacterMaster>();
            foreach (var pcmc in PlayerCharacterMasterController.instances)
            {
                if (pcmc != null && pcmc.master != null && pcmc.master != master && pcmc.master.inventory != null)
                {
                    otherMasters.Add(pcmc.master);
                }
            }

            ItemIndex foundItem = ItemIndex.None;
            string allyName = "";
            if (otherMasters.Count > 0)
            {
                var ally = otherMasters[UnityEngine.Random.Range(0, otherMasters.Count)];
                var matchingItems = new List<ItemIndex>();
                foreach (var itemIdx in ally.inventory.itemAcquisitionOrder)
                {
                    var itemDef = ItemCatalog.GetItemDef(itemIdx);
                    if (itemDef == null || itemDef.hidden || itemDef.tier == ItemTier.NoTier) continue;
                    if (MatchesTier(itemDef.tier, tier))
                    {
                        matchingItems.Add(itemIdx);
                    }
                }
                if (matchingItems.Count > 0)
                {
                    foundItem = matchingItems[UnityEngine.Random.Range(0, matchingItems.Count)];
                    var allyBody = ally.GetBody();
                    allyName = allyBody != null ? allyBody.GetUserName() : "";
                }
            }

            if (foundItem != ItemIndex.None)
            {
                master.inventory.GiveItemPermanent(foundItem, 1);
                PushLocalPickupNotification(master, PickupCatalog.FindPickupIndex(foundItem));
                string itemName = GetItemName(foundItem);
                if (QualityCompat.IsQualityLoaded)
                {
                    var qTier = QualityCompat.GetQualityTier(foundItem);
                    if (qTier != QualityTierCompat.None)
                    {
                        itemName = $"{itemName} [{QualityCompat.GetQualityName(qTier)}]";
                    }
                }
                return !string.IsNullOrEmpty(allyName) 
                    ? Localization.FormatToken("ITEMROULETTE_CHAOS_ALLY_MIRROR_FORMAT", itemName, allyName) 
                    : itemName;
            }
            else
            {
                // Fallback to random item of that tier
                var list = ItemPoolHelper.GetListForTier(tier);
                if (list != null && list.Count > 0)
                {
                    var pIndex = list[UnityEngine.Random.Range(0, list.Count)];
                    var pDef = PickupCatalog.GetPickupDef(pIndex);
                    if (pDef != null)
                    {
                        master.inventory.GiveItemPermanent(pDef.itemIndex, 1);
                        PushLocalPickupNotification(master, pIndex);
                        return GetItemName(pDef.itemIndex);
                    }
                }
            }
            return "";
        }

        private static bool MatchesTier(ItemTier itemTier, RouletteTierType tierType)
        {
            switch (tierType)
            {
                case RouletteTierType.Common: return itemTier == ItemTier.Tier1;
                case RouletteTierType.Uncommon: return itemTier == ItemTier.Tier2;
                case RouletteTierType.Legendary: return itemTier == ItemTier.Tier3;
                case RouletteTierType.Boss: return itemTier == ItemTier.Boss;
                case RouletteTierType.ZetAspects: return itemTier == ItemTier.Boss || itemTier == ItemTier.Tier3;
                case RouletteTierType.VanillaAspect: return itemTier == ItemTier.Boss || itemTier == ItemTier.Tier3;
                case RouletteTierType.Lunar: return itemTier == ItemTier.Lunar;
                case RouletteTierType.Void:
                    return itemTier == ItemTier.VoidTier1 || itemTier == ItemTier.VoidTier2 ||
                           itemTier == ItemTier.VoidTier3 || itemTier == ItemTier.VoidBoss;
                default: return itemTier == ItemTier.Tier1;
            }
        }

        public static void TriggerMandatoryLunarVoidSpinServer(CharacterMaster master)
        {
            if (master == null || !UnityEngine.Networking.NetworkServer.active) return;
            var msg = new ClientEventMessage
            {
                kind = (byte)ClientEventKind.MandatoryLunarVoidSpin
            };
            ServerEvents.SendToTarget(master, msg);
        }

        private static string GrantRandomLunarOrVoid(CharacterMaster master)
        {
            bool isVoid = UnityEngine.Random.value > 0.5f;
            var list = isVoid ? ItemPoolHelper.GetListForTier(RouletteTierType.Void) : ItemPoolHelper.GetListForTier(RouletteTierType.Lunar);
            if (list == null || list.Count == 0) list = ItemPoolHelper.GetListForTier(RouletteTierType.Lunar);
            if (list != null && list.Count > 0)
            {
                var pIndex = list[UnityEngine.Random.Range(0, list.Count)];
                var pDef = PickupCatalog.GetPickupDef(pIndex);
                if (pDef != null)
                {
                    master.inventory.GiveItemPermanent(pDef.itemIndex, 1);
                    PushLocalPickupNotification(master, pIndex);
                    return GetItemName(pDef.itemIndex);
                }
            }
            return "";
        }

        private static string SacrificeTierDownItem(CharacterMaster master, RouletteTierType wonTier)
        {
            ItemTier targetTier = ItemTier.Tier1;
            switch (wonTier)
            {
                case RouletteTierType.Legendary: targetTier = ItemTier.Tier2; break;
                case RouletteTierType.Boss: targetTier = ItemTier.Tier2; break;
                case RouletteTierType.ZetAspects: targetTier = ItemTier.Tier2; break;
                case RouletteTierType.VanillaAspect: targetTier = ItemTier.Tier2; break;
                case RouletteTierType.Uncommon: targetTier = ItemTier.Tier1; break;
                default: targetTier = ItemTier.Tier1; break;
            }

            var candidates = new List<ItemIndex>();
            foreach (var idx in master.inventory.itemAcquisitionOrder)
            {
                var def = ItemCatalog.GetItemDef(idx);
                if (def != null && !def.hidden && def.canRemove && def.tier == targetTier && master.inventory.GetItemCountEffective(idx) > 0)
                {
                    candidates.Add(idx);
                }
            }

            if (candidates.Count > 0)
            {
                var sacrifice = candidates[UnityEngine.Random.Range(0, candidates.Count)];
                master.inventory.RemoveItemPermanent(sacrifice, 1);
                return GetItemName(sacrifice);
            }
            return "";
        }

        private static string GrantHallowedOrCurseItem(CharacterMaster master)
        {
            var validIndices = new List<ItemIndex>();

            // 1. Check active Run's availableHexTierDropList (Hexes from DLC4)
            if (Run.instance != null && Run.instance.availableHexTierDropList != null && Run.instance.availableHexTierDropList.Count > 0)
            {
                foreach (var pIndex in Run.instance.availableHexTierDropList)
                {
                    var pDef = PickupCatalog.GetPickupDef(pIndex);
                    if (pDef != null && pDef.itemIndex != ItemIndex.None && !validIndices.Contains(pDef.itemIndex))
                    {
                        validIndices.Add(pDef.itemIndex);
                    }
                }
            }

            // 2. Collect all items registered with ItemTier.Hex from ItemCatalog
            if (validIndices.Count == 0 && ItemCatalog.allItemDefs.Length > 0)
            {
                foreach (var itemDef in ItemCatalog.allItemDefs)
                {
                    if (itemDef != null && itemDef.tier == ItemTier.Hex && itemDef.itemIndex != ItemIndex.None)
                    {
                        if (!validIndices.Contains(itemDef.itemIndex))
                        {
                            validIndices.Add(itemDef.itemIndex);
                        }
                    }
                }
            }

            // 3. Collect from DLC4Content.Items explicit references
            if (validIndices.Count == 0)
            {
                try
                {
                    ItemDef?[] dlc4Hexes = new[]
                    {
                        DLC4Content.Items.HexCurse,
                        DLC4Content.Items.RoamingItemDisable,
                        DLC4Content.Items.SlipperyGround,
                        DLC4Content.Items.IncreasedDifficulty,
                        DLC4Content.Items.RandomDebuffOnInteract,
                        DLC4Content.Items.Blindness,
                        DLC4Content.Items.WeakAssHamstring,
                        DLC4Content.Items.OutOfTPRadius,
                        DLC4Content.Items.HexMountain,
                        DLC4Content.Items.LowerHealing,
                        DLC4Content.Items.HexHaunting,
                        DLC4Content.Items.Mimicry,
                        DLC4Content.Items.BarrierEnemiesOnKill,
                        DLC4Content.Items.ShareItemsWithEnemies
                    };
                    foreach (var itemDef in dlc4Hexes)
                    {
                        if (itemDef != null && itemDef.itemIndex != ItemIndex.None && !validIndices.Contains(itemDef.itemIndex))
                        {
                            validIndices.Add(itemDef.itemIndex);
                        }
                    }
                }
                catch { }
            }

            // 4. Name-based lookup for all 14 Hexes
            if (validIndices.Count == 0)
            {
                string[] hexNames = new[]
                {
                    "HexCurse",
                    "RoamingItemDisable",
                    "SlipperyGround",
                    "IncreasedDifficulty",
                    "RandomDebuffOnInteract",
                    "RandomDebuffOnSuccessfulRoll",
                    "Blindness",
                    "WeakAssHamstring",
                    "OutOfTPRadius",
                    "HexMountain",
                    "LowerHealing",
                    "HexHaunting",
                    "Mimicry",
                    "BarrierEnemiesOnKill",
                    "BarrierEnemies",
                    "ShareItemsWithEnemies",
                    "ShareItems"
                };
                foreach (var name in hexNames)
                {
                    ItemIndex idx = ItemCatalog.FindItemIndex(name);
                    if (idx != ItemIndex.None && !validIndices.Contains(idx))
                    {
                        validIndices.Add(idx);
                    }
                }
            }

            // 5. Ultimate fallback to classic lunar curses if DLC4 is completely missing
            if (validIndices.Count == 0)
            {
                var curseCandidates = new[]
                {
                    "LunarBadLuck",
                    "LunarDagger",
                    "LunarSun",
                    "LunarTrinket",
                    "TonicAffliction"
                };
                foreach (var name in curseCandidates)
                {
                    ItemIndex idx = ItemCatalog.FindItemIndex(name);
                    if (idx != ItemIndex.None)
                    {
                        validIndices.Add(idx);
                    }
                }
            }

            if (validIndices.Count > 0)
            {
                var chosen = validIndices[UnityEngine.Random.Range(0, validIndices.Count)];
                master.inventory.GiveItemPermanent(chosen, 1);
                PushLocalPickupNotification(master, PickupCatalog.FindPickupIndex(chosen));
                return GetItemName(chosen);
            }
            return "";
        }

        private static void MimicItemsToEnemy(CharacterMaster playerMaster)
        {
            var monsterTeam = TeamComponent.GetTeamMembers(TeamIndex.Monster);
            CharacterBody? bestTarget = null;

            foreach (var member in monsterTeam)
            {
                if (member == null || member.body == null || !member.body.healthComponent.alive) continue;
                if (member.body.isElite)
                {
                    bestTarget = member.body;
                    break;
                }
                if (bestTarget == null) bestTarget = member.body;
            }

            if (bestTarget != null && bestTarget.master != null && bestTarget.master.inventory != null)
            {
                foreach (var idx in playerMaster.inventory.itemAcquisitionOrder)
                {
                    var def = ItemCatalog.GetItemDef(idx);
                    if (def == null || def.hidden || def.tier == ItemTier.NoTier) continue;

                    if (UnityEngine.Random.value < 0.50f)
                    {
                        int count = Mathf.Max(1, playerMaster.inventory.GetItemCountEffective(idx) / 2);
                        bestTarget.master.inventory.GiveItemPermanent(idx, count);
                    }
                }
            }
        }

        private static void SpawnDoppelganger(CharacterMaster summoner, TeamIndex team)
        {
            if (!NetworkServer.active) return;
            var body = summoner.GetBody();
            if (body == null) return;

            try
            {
                // 1. Correctly find AI Master Prefab for the player's survivor body
                var masterIndex = MasterCatalog.FindAiMasterIndexForBody(body.bodyIndex);
                GameObject? masterPrefab = MasterCatalog.GetMasterPrefab(masterIndex);
                if (masterPrefab == null)
                {
                    string cleanName = summoner.gameObject.name.Replace("(Clone)", "").Trim();
                    masterPrefab = MasterCatalog.FindMasterPrefab(cleanName)
                                ?? MasterCatalog.FindMasterPrefab(cleanName + "MonsterMaster")
                                ?? MasterCatalog.FindMasterPrefab("CommandoMonsterMaster");
                }

                if (masterPrefab != null)
                {
                    Vector3 spawnPos = body.corePosition + (body.transform.forward * 3f) + (Vector3.up * 0.5f);
                    var summon = new MasterSummon
                    {
                        masterPrefab = masterPrefab,
                        position = spawnPos,
                        rotation = body.transform.rotation,
                        summonerBodyObject = body.gameObject,
                        teamIndexOverride = team,
                        ignoreTeamMemberLimit = true,
                        useAmbientLevel = true
                    };
                    var cloneMaster = summon.Perform();
                    if (cloneMaster != null)
                    {
                        // Copy player's full inventory of items
                        if (cloneMaster.inventory != null && summoner.inventory != null)
                        {
                            cloneMaster.inventory.CopyItemsFrom(summoner.inventory);
                        }

                        if (team == TeamIndex.Player)
                        {
                            // Register MinionOwnership so it appears on the player's companion HUD display!
                            if (cloneMaster.minionOwnership != null)
                            {
                                cloneMaster.minionOwnership.SetOwner(summoner);
                            }

                            // Set AIOwnership and leader target so it follows the player
                            var aiOwnership = cloneMaster.GetComponent<RoR2.CharacterAI.AIOwnership>();
                            if (aiOwnership != null)
                            {
                                aiOwnership.ownerMaster = summoner;
                            }

                            var baseAIs = cloneMaster.GetComponents<RoR2.CharacterAI.BaseAI>();
                            foreach (var ai in baseAIs)
                            {
                                if (ai != null && ai.leader != null)
                                {
                                    ai.leader.gameObject = body.gameObject;
                                }
                            }

                            // Minion leash ensures it stays tethered and teleports along with the player
                            if (cloneMaster.inventory != null && RoR2Content.Items.MinionLeash != null)
                            {
                                cloneMaster.inventory.GiveItemPermanent(RoR2Content.Items.MinionLeash.itemIndex, 1);
                            }

                            // Set companion subtitle token
                            var cloneBody = cloneMaster.GetBody();
                            if (cloneBody != null)
                            {
                                string companionOwner = summoner.GetBody()?.GetUserName() ?? Localization.Get("игрока", "player");
                                cloneBody.subtitleNameToken = $"ITEMROULETTE_SUBTITLE_COMPANION:{companionOwner}";
                            }
                        }
                        else
                        {
                            // Evil clone: give InvadingDoppelganger visual aura (shadows & red eyes)
                            if (cloneMaster.inventory != null && RoR2Content.Items.InvadingDoppelganger != null)
                            {
                                cloneMaster.inventory.GiveItemPermanent(RoR2Content.Items.InvadingDoppelganger.itemIndex, 1);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemRoulette] Exception spawning clone: {ex}");
            }
        }

        public static bool IsArtifactAllowed(ArtifactDef def, bool isCoop)
        {
            if (def == null) return false;
            string name = def.cachedName ?? "";
            string token = (def.nameToken ?? "").ToUpperInvariant();

            // Check custom blacklist first
            string blacklist = ModConfig.ArtifactCustomBlacklist.Value;
            if (!string.IsNullOrWhiteSpace(blacklist))
            {
                string[] parts = blacklist.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in parts)
                {
                    string trim = p.Trim();
                    if (string.IsNullOrEmpty(trim)) continue;
                    if (name.IndexOf(trim, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        token.IndexOf(trim, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return false;
                    }
                }
            }

            // 1. Spite / Bomb
            if (name.Equals("Bomb", StringComparison.OrdinalIgnoreCase) || token.Contains("BOMB") || token.Contains("SPITE"))
                return ModConfig.AllowArtifactSpite.Value;

            // 2. Warfare / War
            if (name.IndexOf("Warfare", StringComparison.OrdinalIgnoreCase) >= 0 || 
                name.IndexOf("ArtifactOfWar", StringComparison.OrdinalIgnoreCase) >= 0 || 
                name.Equals("War", StringComparison.OrdinalIgnoreCase) || 
                token.Contains("WARFARE") || token.Contains("ARTIFACT_WAR_") || token.EndsWith("_WAR_NAME") || token.EndsWith("_WARFARE_NAME"))
                return ModConfig.AllowArtifactWarfare.Value;

            // 3. Honor / EliteOnly
            if (name.Equals("EliteOnly", StringComparison.OrdinalIgnoreCase) || token.Contains("ELITEONLY") || token.Contains("HONOR"))
                return ModConfig.AllowArtifactHonor.Value;

            // 4. Chaos / FriendlyFire (only in coop)
            if (name.Equals("FriendlyFire", StringComparison.OrdinalIgnoreCase) || token.Contains("FRIENDLYFIRE") || token.Contains("CHAOS"))
                return isCoop && ModConfig.AllowArtifactChaos.Value;

            // 5. Dissonance / MixEnemy
            if (name.Equals("MixEnemy", StringComparison.OrdinalIgnoreCase) || token.Contains("MIXENEMY") || token.Contains("DISSONANCE"))
                return ModConfig.AllowArtifactDissonance.Value;

            // 6. Kin / SingleMonsterType (Единство / Родство)
            if (name.Equals("SingleMonsterType", StringComparison.OrdinalIgnoreCase) || token.Contains("SINGLEMONSTERTYPE") || token.Contains("KIN"))
                return ModConfig.AllowArtifactKin.Value;

            // 7. Swarms
            if (name.Equals("Swarms", StringComparison.OrdinalIgnoreCase) || token.Contains("SWARMS"))
                return ModConfig.AllowArtifactSwarms.Value;

            // 8. Frailty / WeakAssKnees
            if (name.Equals("WeakAssKnees", StringComparison.OrdinalIgnoreCase) || token.Contains("WEAKASSKNEES") || token.Contains("FRAILTY"))
                return ModConfig.AllowArtifactFrailty.Value;

            // 9. Soul / WispOnDeath
            if (name.Equals("WispOnDeath", StringComparison.OrdinalIgnoreCase) || token.Contains("WISPONDEATH") || token.Contains("SOUL"))
                return ModConfig.AllowArtifactSoul.Value;

            // 10. Glass
            if (name.Equals("Glass", StringComparison.OrdinalIgnoreCase) || token.Contains("GLASS"))
                return ModConfig.AllowArtifactGlass.Value;

            // 11. Evolution / MonsterTeamGainsItems
            if (name.Equals("MonsterTeamGainsItems", StringComparison.OrdinalIgnoreCase) || token.Contains("MONSTERTEAMGAINSITEMS") || token.Contains("EVOLUTION"))
                return ModConfig.AllowArtifactEvolution.Value;

            // 12. Vengeance / ShadowClone
            if (name.Equals("ShadowClone", StringComparison.OrdinalIgnoreCase) || token.Contains("SHADOWCLONE") || token.Contains("VENGEANCE"))
                return ModConfig.AllowArtifactVengeance.Value;

            // 13. Metamorphosis / RandomSurvivorOnRespawn
            if (name.Equals("RandomSurvivorOnRespawn", StringComparison.OrdinalIgnoreCase) || token.Contains("RANDOMSURVIVORONRESPAWN") || token.Contains("METAMORPHOSIS"))
                return ModConfig.AllowArtifactMetamorphosis.Value;

            // 14. Enigma
            if (name.Equals("Enigma", StringComparison.OrdinalIgnoreCase) || token.Contains("ENIGMA"))
                return ModConfig.AllowArtifactEnigma.Value;

            // 15. Sacrifice
            if (name.Equals("Sacrifice", StringComparison.OrdinalIgnoreCase) || token.Contains("SACRIFICE"))
                return ModConfig.AllowArtifactSacrifice.Value;

            // 16. Command
            if (name.Equals("Command", StringComparison.OrdinalIgnoreCase) || token.Contains("COMMAND"))
                return ModConfig.AllowArtifactCommand.Value;

            // 17. Death / TeamDeath
            if (name.Equals("TeamDeath", StringComparison.OrdinalIgnoreCase) || token.Contains("TEAMDEATH") || token.Contains("DEATH"))
                return ModConfig.AllowArtifactDeath.Value;

            // 18. Devotion (Единство дронов / Преданность)
            if (name.Equals("Devotion", StringComparison.OrdinalIgnoreCase) || token.Contains("DEVOTION"))
                return ModConfig.AllowArtifactDevotion.Value;

            // 19. Delusion
            if (name.Equals("Delusion", StringComparison.OrdinalIgnoreCase) || token.Contains("DELUSION"))
                return ModConfig.AllowArtifactDelusion.Value;

            // 20. Rebirth
            if (name.Equals("Rebirth", StringComparison.OrdinalIgnoreCase) || token.Contains("REBIRTH"))
                return ModConfig.AllowArtifactRebirth.Value;

            // 21. Third-party / modded artifacts
            return ModConfig.AllowModdedArtifacts.Value;
        }

        private static string ExecuteRandomStageArtifact(CharacterMaster master, ChaosEventDefinition? eventDef = null)
        {
            try
            {
                bool isCoop = RoR2Application.isInMultiPlayer || 
                              (NetworkUser.readOnlyInstancesList != null && NetworkUser.readOnlyInstancesList.Count > 1);

                var pool = new List<ArtifactDef>();

                void TryAdd(ArtifactDef? def)
                {
                    if (def != null && !pool.Contains(def) && IsArtifactAllowed(def, isCoop))
                    {
                        pool.Add(def);
                    }
                }

                if (ArtifactCatalog.artifactCount > 0)
                {
                    for (int i = 0; i < ArtifactCatalog.artifactCount; i++)
                    {
                        var def = ArtifactCatalog.GetArtifactDef((ArtifactIndex)i);
                        if (def != null)
                        {
                            TryAdd(def);
                        }
                    }
                }
                else
                {
                    // Fallback to static catalog if dynamic count is not ready
                    TryAdd(ResolveArtifact(RoR2Content.Artifacts.Bomb, "Bomb"));
                    TryAdd(FindWarArtifact());
                    TryAdd(ResolveArtifact(RoR2Content.Artifacts.EliteOnly, "EliteOnly"));
                    if (isCoop) TryAdd(ResolveArtifact(RoR2Content.Artifacts.FriendlyFire, "FriendlyFire"));
                    TryAdd(ResolveArtifact(RoR2Content.Artifacts.MixEnemy, "MixEnemy"));
                    TryAdd(ResolveArtifact(RoR2Content.Artifacts.SingleMonsterType, "SingleMonsterType"));
                    TryAdd(ResolveArtifact(RoR2Content.Artifacts.Swarms, "Swarms"));
                    TryAdd(ResolveArtifact(RoR2Content.Artifacts.WeakAssKnees, "WeakAssKnees"));
                    TryAdd(ResolveArtifact(RoR2Content.Artifacts.WispOnDeath, "WispOnDeath"));
                }

                // Filter out artifacts that are ALREADY active on this run
                var available = new List<ArtifactDef>();
                if (RunArtifactManager.instance != null)
                {
                    foreach (var def in pool)
                    {
                        if (!RunArtifactManager.instance.IsArtifactEnabled(def))
                        {
                            available.Add(def);
                        }
                    }
                }
                else
                {
                    available.AddRange(pool);
                }

                ArtifactDef? chosen = null;
                if (available.Count > 0)
                {
                    chosen = available[UnityEngine.Random.Range(0, available.Count)];
                }

                if (chosen == null)
                {
                    eventDef?.SetDescriptionFromToken("ITEMROULETTE_CHAOS_STAGE_ARTIFACT_ALL_ACTIVE");
                    return Localization.Get("ITEMROULETTE_CHAOS_STAGE_ARTIFACT_ALL_ACTIVE");
                }

                // Enable artifact
                if (UnityEngine.Networking.NetworkServer.active)
                {
                    if (RunArtifactManager.instance != null)
                    {
                        RunArtifactManager.instance.SetArtifactEnabledServer(chosen, true);
                    }
                    RouletteManager.Instance.StageTemporaryArtifacts.Add(chosen.artifactIndex);
                }
                else
                {
                    // In multiplayer client: inform server host via network chat message
                    RouletteManager.Instance.StageTemporaryArtifacts.Add(chosen.artifactIndex);
                    ServerEvents.Broadcast($"[ItemRoulette:EnableStageArtifact:{(int)chosen.artifactIndex}]");
                }

                string artName = GetArtifactName(chosen);
                eventDef?.SetDescriptionFromToken("ITEMROULETTE_CHAOS_STAGE_ARTIFACT_DEBUFF", artName);
                return Localization.FormatToken("ITEMROULETTE_CHAOS_STAGE_ARTIFACT_DEBUFF", artName);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemRoulette] Exception in ExecuteRandomStageArtifact: {ex}");
                eventDef?.SetDescriptionFromToken("ITEMROULETTE_CHAOS_STAGE_ARTIFACT_FALLBACK");
                return Localization.Get("ITEMROULETTE_CHAOS_STAGE_ARTIFACT_FALLBACK");
            }
        }

        private static ArtifactDef? FindWarArtifact()
        {
            if (ArtifactCatalog.artifactCount <= 0) return null;

            for (int i = 0; i < ArtifactCatalog.artifactCount; i++)
            {
                var def = ArtifactCatalog.GetArtifactDef((ArtifactIndex)i);
                if (def == null) continue;

                string name = def.cachedName ?? "";
                if (name.Equals("Swarms", StringComparison.OrdinalIgnoreCase)) continue;

                if (name.IndexOf("Warfare", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("ArtifactOfWar", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.Equals("War", StringComparison.OrdinalIgnoreCase) ||
                    name.IndexOf("ArtifactWar", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return def;
                }

                if (!string.IsNullOrEmpty(def.nameToken))
                {
                    string tokenUpper = def.nameToken.ToUpperInvariant();
                    if (tokenUpper.Contains("WARFARE") ||
                        tokenUpper.Contains("ARTIFACT_WAR_") ||
                        tokenUpper.EndsWith("_WAR_NAME") ||
                        tokenUpper.EndsWith("_WARFARE_NAME"))
                    {
                        return def;
                    }
                }
            }
            return null;
        }

        private static ArtifactDef? ResolveArtifact(ArtifactDef? primary, string fallbackName)
        {
            if (primary != null) return primary;
            if (!string.IsNullOrEmpty(fallbackName))
            {
                try
                {
                    return ArtifactCatalog.FindArtifactDef(fallbackName);
                }
                catch { }
            }
            return null;
        }

        public static string GetArtifactName(ArtifactDef? artifactDef)
        {
            if (artifactDef == null) return Localization.Get("Артефакт", "Artifact");
            try
            {
                if (!string.IsNullOrEmpty(artifactDef.nameToken))
                {
                    string loc = Language.GetString(artifactDef.nameToken);
                    if (!string.IsNullOrEmpty(loc) && loc != artifactDef.nameToken)
                    {
                        return loc;
                    }
                }
                if (!string.IsNullOrEmpty(artifactDef.cachedName))
                {
                    return artifactDef.cachedName;
                }
                return Localization.Get("Артефакт", "Artifact");
            }
            catch
            {
                return Localization.Get("Артефакт", "Artifact");
            }
        }

        public static string ExecuteCombatShrineX2(CharacterMaster master, ChaosEventDefinition? eventDef = null)
        {
            if (!NetworkServer.active)
            {
                // In multiplayer client: inform server host via network chat message
                ServerEvents.Broadcast($"[ItemRoulette:TriggerCombatShrineX2:{master.netId}]");
                eventDef?.SetDescriptionFromToken("ITEMROULETTE_CHAOS_COMBAT_SHRINE_CLIENT");
                return Localization.Get("ITEMROULETTE_CHAOS_COMBAT_SHRINE_CLIENT");
            }

            try
            {
                var body = master != null ? master.GetBody() : null;
                Interactor? interactor = body != null ? body.GetComponent<Interactor>() : null;
                if (interactor == null)
                {
                    foreach (var pcmc in PlayerCharacterMasterController.instances)
                    {
                        if (pcmc != null && pcmc.master != null)
                        {
                            var b = pcmc.master.GetBody();
                            if (b != null)
                            {
                                var inter = b.GetComponent<Interactor>();
                                if (inter != null)
                                {
                                    interactor = inter;
                                    body = b;
                                    break;
                                }
                            }
                        }
                    }
                }

                // Difficulty scaling
                float diffCoeff = Run.instance != null ? Mathf.Max(1f, Run.instance.difficultyCoefficient) : 1f;
                if (Stage.instance != null && Stage.instance.entryDifficultyCoefficient > diffCoeff)
                {
                    diffCoeff = Stage.instance.entryDifficultyCoefficient;
                }
                float monsterCredit = 100f * diffCoeff;

                var physicalShrines = UnityEngine.Object.FindObjectsOfType<ShrineCombatBehavior>();

                if (RoR2Application.instance != null)
                {
                    RoR2Application.instance.StartCoroutine(SpawnCombatShrineWavesCoroutine(interactor, monsterCredit, physicalShrines));
                }
                else
                {
                    var shrine1 = (physicalShrines != null && physicalShrines.Length > 0) ? physicalShrines[0] : null;
                    var shrine2 = (physicalShrines != null && physicalShrines.Length > 1) ? physicalShrines[1] : null;
                    TriggerSingleShrineWave(interactor, monsterCredit, shrine1);
                    TriggerSingleShrineWave(interactor, monsterCredit, shrine2);
                }

                eventDef?.SetDescriptionFromToken("ITEMROULETTE_CHAOS_COMBAT_SHRINE_CHALLENGE");
                return Localization.Get("ITEMROULETTE_CHAOS_COMBAT_SHRINE_CHALLENGE");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemRoulette] Exception in ExecuteCombatShrineX2: {ex}");
                eventDef?.SetDescriptionFromToken("ITEMROULETTE_CHAOS_COMBAT_SHRINE_FALLBACK");
                return Localization.Get("ITEMROULETTE_CHAOS_COMBAT_SHRINE_FALLBACK");
            }
        }

        private static System.Collections.IEnumerator SpawnCombatShrineWavesCoroutine(
            Interactor? interactor,
            float monsterCredit,
            ShrineCombatBehavior[]? physicalShrines)
        {
            // Wave 1
            ShrineCombatBehavior? shrine1 = (physicalShrines != null && physicalShrines.Length > 0) ? physicalShrines[0] : null;
            TriggerSingleShrineWave(interactor, monsterCredit, shrine1);

            // Dramatic 0.75s pause before wave 2
            yield return new WaitForSeconds(0.75f);

            // Wave 2
            ShrineCombatBehavior? shrine2 = (physicalShrines != null && physicalShrines.Length > 1) ? physicalShrines[1] : null;
            TriggerSingleShrineWave(interactor, monsterCredit, shrine2);
        }

        private static void TriggerSingleShrineWave(Interactor? interactor, float monsterCredit, ShrineCombatBehavior? physicalShrine)
        {
            try
            {
                if (physicalShrine != null && interactor != null)
                {
                    physicalShrine.AddShrineStack(interactor);
                    return;
                }

                // If no physical shrine available, use an ambient/stage CombatDirector
                CombatDirector? director = null;
                if (CombatDirector.instancesList != null && CombatDirector.instancesList.Count > 0)
                {
                    foreach (var cd in CombatDirector.instancesList)
                    {
                        if (cd != null && cd.isActiveAndEnabled)
                        {
                            director = cd;
                            break;
                        }
                    }
                    if (director == null)
                    {
                        director = CombatDirector.instancesList[0];
                    }
                }

                if (director != null)
                {
                    var card = director.SelectMonsterCardForCombatShrine(monsterCredit);
                    if (interactor != null)
                    {
                        director.CombatShrineActivation(interactor, monsterCredit, card);
                    }
                    else
                    {
                        director.StartManualMonsterWave(monsterCredit, card, 0f);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ItemRoulette] Exception in TriggerSingleShrineWave: {ex}");
            }
        }

        public static List<EquipmentIndex> GetAvailableEliteAspects()
        {
            var list = new List<EquipmentIndex>();
            var seen = new HashSet<EquipmentIndex>();

            // 1. Dynamic scan from EliteCatalog
            try
            {
                if (EliteCatalog.eliteList != null)
                {
                    foreach (var eliteIdx in EliteCatalog.eliteList)
                    {
                        var eliteDef = EliteCatalog.GetEliteDef(eliteIdx);
                        if (eliteDef != null && eliteDef.eliteEquipmentDef != null)
                        {
                            var eqDef = eliteDef.eliteEquipmentDef;
                            // Only include functional elite aspects that have a valid passive buff.
                            // Filters out incomplete/dummy internal DLC2 equipments like EliteAurelioniteEquipment.
                            if (eqDef.passiveBuffDef != null && eqDef.equipmentIndex != EquipmentIndex.None && seen.Add(eqDef.equipmentIndex))
                            {
                                list.Add(eqDef.equipmentIndex);
                            }
                        }
                    }
                }
            }
            catch { }

            // 2. Base RoR2 Elite Equipment affixes
            EquipmentIndex[] baseAffixes = new[]
            {
                RoR2Content.Equipment.AffixRed.equipmentIndex,       // Ifrit's Distinction (Fire)
                RoR2Content.Equipment.AffixBlue.equipmentIndex,      // Silence Between Two Strikes (Lightning)
                RoR2Content.Equipment.AffixWhite.equipmentIndex,     // Her Biting Embrace (Ice)
                RoR2Content.Equipment.AffixPoison.equipmentIndex,    // N'kuhana's Retort (Malachite)
                RoR2Content.Equipment.AffixHaunted.equipmentIndex,   // Spectral Circlet (Celestine)
                RoR2Content.Equipment.AffixLunar.equipmentIndex,     // Shared Design (Perfected)
            };
            foreach (var affix in baseAffixes)
            {
                if (affix != EquipmentIndex.None && seen.Add(affix))
                {
                    list.Add(affix);
                }
            }

            // 3. DLC1 & DLC2 affixes if available
            try
            {
                if (DLC1Content.Equipment.EliteEarthEquipment != null && seen.Add(DLC1Content.Equipment.EliteEarthEquipment.equipmentIndex))
                {
                    list.Add(DLC1Content.Equipment.EliteEarthEquipment.equipmentIndex); // His Reassurance (Mending)
                }
                if (DLC1Content.Equipment.EliteVoidEquipment != null && seen.Add(DLC1Content.Equipment.EliteVoidEquipment.equipmentIndex))
                {
                    list.Add(DLC1Content.Equipment.EliteVoidEquipment.equipmentIndex); // Void Aspect
                }
            }
            catch { }

            return list;
        }

        public static string ExecuteElementalAspect(CharacterMaster master, ChaosEventDefinition? eventDef = null)
        {
            if (master == null)
            {
                eventDef?.SetDescriptionFromToken("ITEMROULETTE_CHAOS_ASPECT_FALLBACK");
                return Localization.Get("ITEMROULETTE_CHAOS_ASPECT_FALLBACK");
            }

            // 1. If ZetAspects mod is installed and integration is enabled, award aspect as passive item!
            if (ZetAspectsCompat.IsZetAspectsLoaded && ModConfig.EnableZetAspectsIntegration.Value)
            {
                var aspectItems = ZetAspectsCompat.GetAspectItemIndexes();
                if (aspectItems != null && aspectItems.Count > 0)
                {
                    ItemIndex chosenItem = ItemIndex.None;
                    if (ModConfig.ZetAspectsDuplicateExistingOnChaos.Value && master != null && master.inventory != null)
                    {
                        var ownedAspects = new List<ItemIndex>();
                        foreach (var asp in aspectItems)
                        {
                            if (master.inventory.GetItemCountEffective(asp) > 0)
                            {
                                ownedAspects.Add(asp);
                            }
                        }
                        if (ownedAspects.Count > 0)
                        {
                            chosenItem = ownedAspects[UnityEngine.Random.Range(0, ownedAspects.Count)];
                        }
                    }

                    if (chosenItem == ItemIndex.None)
                    {
                        chosenItem = aspectItems[UnityEngine.Random.Range(0, aspectItems.Count)];
                    }
                    var itemDef = ItemCatalog.GetItemDef(chosenItem);
                    string aspectName = itemDef != null ? Language.GetString(itemDef.nameToken) : Localization.Get("Элементальный Аспект", "Elemental Aspect");

                    if (NetworkServer.active)
                    {
                        ExecuteGrantAspectItemServer(master!, chosenItem);
                    }
                    else if (master != null)
                    {
                        ServerEvents.Broadcast($"[ItemRoulette:GrantAspectItem:{(int)chosenItem}:{master.netId.Value}]");
                    }

                    eventDef?.SetDescriptionFromToken("ITEMROULETTE_CHAOS_ASPECT_RESULT", aspectName);
                    return Localization.FormatToken("ITEMROULETTE_CHAOS_ASPECT_RESULT", aspectName);
                }
            }

            // 2. Vanilla fallback: award as elite equipment
            var aspects = GetAvailableEliteAspects();
            if (aspects == null || aspects.Count == 0)
            {
                eventDef?.SetDescriptionFromToken("ITEMROULETTE_CHAOS_ASPECT_NOT_FOUND");
                return Localization.Get("ITEMROULETTE_CHAOS_ASPECT_NOT_FOUND");
            }

            EquipmentIndex chosen = aspects[UnityEngine.Random.Range(0, aspects.Count)];
            var eqDef = EquipmentCatalog.GetEquipmentDef(chosen);
            string vanillaAspectName = eqDef != null ? Language.GetString(eqDef.nameToken) : Localization.Get("Элементальная Сфера", "Elemental Aspect");

            if (NetworkServer.active)
            {
                ExecuteGrantElementalAspectServer(master, chosen);
            }
            else
            {
                // In multiplayer client mode, request server execution
                ServerEvents.Broadcast($"[ItemRoulette:GrantElementalAspect:{(int)chosen}:{master.netId.Value}]");
            }

            eventDef?.SetDescriptionFromToken("ITEMROULETTE_CHAOS_ASPECT_EQUIP_RESULT", vanillaAspectName);
            return Localization.FormatToken("ITEMROULETTE_CHAOS_ASPECT_EQUIP_RESULT", vanillaAspectName);
        }

        public static void ExecuteGrantAspectItemServer(CharacterMaster master, ItemIndex chosenItem)
        {
            if (master == null || master.inventory == null || chosenItem == ItemIndex.None) return;

            master.inventory.GiveItemPermanent(chosenItem, 1);
            PushLocalPickupNotification(master, PickupCatalog.FindPickupIndex(chosenItem));
        }

        public static void ExecuteGrantElementalAspectServer(CharacterMaster master, EquipmentIndex chosenAspect)
        {
            if (master == null || master.inventory == null || chosenAspect == EquipmentIndex.None) return;

            // If ZetAspects is loaded, convert equipment aspect to item!
            if (ZetAspectsCompat.IsZetAspectsLoaded && ModConfig.EnableZetAspectsIntegration.Value)
            {
                ItemIndex aspectItem = ZetAspectsCompat.GetAspectItemIndex(chosenAspect);
                if (aspectItem != ItemIndex.None)
                {
                    ExecuteGrantAspectItemServer(master, aspectItem);
                    return;
                }
            }

            var body = master.GetBody();
            EquipmentIndex oldEquipment = PlayerUtil.GetStoredEquipmentIndex(master.inventory);

            // If player already has an equipment: drop previous equipment onto the floor!
            if (oldEquipment != EquipmentIndex.None && oldEquipment != chosenAspect)
            {
                PickupIndex oldPickupIndex = PickupCatalog.FindPickupIndex(oldEquipment);
                if (oldPickupIndex != PickupIndex.none)
                {
                    Vector3 dropPos = (body != null) ? (body.corePosition + Vector3.up * 1.5f) : Vector3.zero;
                    Vector3 dropVel = (body != null) ? (Vector3.up * 5f + body.transform.forward * 3f) : (Vector3.up * 5f);

                    PickupDropletController.CreatePickupDroplet(
                        new GenericPickupController.CreatePickupInfo
                        {
                            pickup = new UniquePickup { pickupIndex = oldPickupIndex }
                        },
                        dropPos,
                        dropVel
                    );
                }
            }

            // Assign new aspect
            EquipmentLocation loc = master.inventory.activeEquipmentLocation;
            master.inventory.SetEquipmentIndex(chosenAspect, in loc);

            // Display pickup notification
            PushLocalPickupNotification(master, PickupCatalog.FindPickupIndex(chosenAspect));
        }

        public static string ExecuteLowGravityMoonX2(CharacterMaster master, ChaosEventDefinition? eventDef = null)
        {
            float targetGravity = ModConfig.LowGravityValue.Value;
            RouletteManager.Instance.ApplyStageLowGravity(targetGravity);

            // In multiplayer, broadcast network tag so all clients sync Physics.gravity
            ServerEvents.Broadcast($"[ItemRoulette:SetStageGravity:{targetGravity.ToString(System.Globalization.CultureInfo.InvariantCulture)}]");

            eventDef?.SetDescriptionFromToken("ITEMROULETTE_CHAOS_LOW_GRAVITY_RESULT", targetGravity);
            return Localization.FormatToken("ITEMROULETTE_CHAOS_LOW_GRAVITY_RESULT", targetGravity);
        }

        public static string ExecuteTierOddsBoost(CharacterMaster master, ChaosEventDefinition? eventDef = null)
        {
            if (master == null)
            {
                eventDef?.SetDescriptionFromToken("ITEMROULETTE_CHAOS_TIER_ODDS_BOOST_FALLBACK");
                return Localization.Get("ITEMROULETTE_CHAOS_TIER_ODDS_BOOST_FALLBACK");
            }

            float oldMult = RouletteManager.Instance.GetPlayerTierMultiplier(master);
            float newMult = Mathf.Min(4.0f, oldMult * ModConfig.TierOddsBoostMultiplier.Value);
            uint netId = master.netId.Value != 0 ? master.netId.Value : (uint)master.GetInstanceID();

            RouletteManager.Instance.SetPlayerTierMultiplier(netId, newMult);

            // In multiplayer, broadcast network tag so all clients sync personal multiplier
            ServerEvents.Broadcast($"[ItemRoulette:SetPlayerTierLuck:{netId}:{newMult.ToString(System.Globalization.CultureInfo.InvariantCulture)}]");

            eventDef?.SetDescriptionFromToken("ITEMROULETTE_CHAOS_TIER_ODDS_BOOST_RESULT", newMult);
            return Localization.FormatToken("ITEMROULETTE_CHAOS_TIER_ODDS_BOOST_RESULT", newMult);
        }

        public static string ExecuteTierOddsPenalty(CharacterMaster master, ChaosEventDefinition? eventDef = null)
        {
            if (master == null)
            {
                eventDef?.SetDescriptionFromToken("ITEMROULETTE_CHAOS_TIER_ODDS_PENALTY_FALLBACK");
                return Localization.Get("ITEMROULETTE_CHAOS_TIER_ODDS_PENALTY_FALLBACK");
            }

            float oldMult = RouletteManager.Instance.GetPlayerTierMultiplier(master);
            float newMult = Mathf.Max(0.20f, oldMult * ModConfig.TierOddsPenaltyMultiplier.Value);
            uint netId = master.netId.Value != 0 ? master.netId.Value : (uint)master.GetInstanceID();

            RouletteManager.Instance.SetPlayerTierMultiplier(netId, newMult);

            // In multiplayer, broadcast network tag so all clients sync personal multiplier
            ServerEvents.Broadcast($"[ItemRoulette:SetPlayerTierLuck:{netId}:{newMult.ToString(System.Globalization.CultureInfo.InvariantCulture)}]");

            eventDef?.SetDescriptionFromToken("ITEMROULETTE_CHAOS_TIER_ODDS_PENALTY_RESULT", newMult);
            return Localization.FormatToken("ITEMROULETTE_CHAOS_TIER_ODDS_PENALTY_RESULT", newMult);
        }

        public class DroneSquadOption
        {
            public string NameRu { get; set; } = "";
            public string NameEn { get; set; } = "";
            public string Name
            {
                get => Localization.Get(NameRu, NameEn);
                set { NameRu = value; NameEn = value; }
            }
            public string FallbackMasterName { get; set; } = "";
            public Func<DroneDef?>? GetDroneDef { get; set; }
            public int Count { get; set; }
        }

        public static List<DroneSquadOption> GetHostileDroneOptions()
        {
            var list = new List<DroneSquadOption>();

            int gunnerCount = ModConfig.HostileDroneGunnerCount != null ? ModConfig.HostileDroneGunnerCount.Value : 4;
            if (gunnerCount > 0)
            {
                list.Add(new DroneSquadOption
                {
                    NameRu = "Дрон-стрелок",
                    NameEn = "Gunner Drone",
                    FallbackMasterName = "Drone1Master",
                    GetDroneDef = () => RoR2Content.DroneDefs.Drone1,
                    Count = gunnerCount
                });
            }

            int flameCount = ModConfig.HostileDroneFlameCount != null ? ModConfig.HostileDroneFlameCount.Value : 3;
            if (flameCount > 0)
            {
                list.Add(new DroneSquadOption
                {
                    NameRu = "Огнемётный дрон",
                    NameEn = "Flame Drone",
                    FallbackMasterName = "FlameDroneMaster",
                    GetDroneDef = () => RoR2Content.DroneDefs.FlameDrone,
                    Count = flameCount
                });
            }

            int missileCount = ModConfig.HostileDroneMissileCount != null ? ModConfig.HostileDroneMissileCount.Value : 2;
            if (missileCount > 0)
            {
                list.Add(new DroneSquadOption
                {
                    NameRu = "Ракетный дрон",
                    NameEn = "Missile Drone",
                    FallbackMasterName = "MissileDroneMaster",
                    GetDroneDef = () => RoR2Content.DroneDefs.MissileDrone,
                    Count = missileCount
                });
            }

            int megaCount = ModConfig.HostileDroneMegaCount != null ? ModConfig.HostileDroneMegaCount.Value : 1;
            if (megaCount > 0)
            {
                list.Add(new DroneSquadOption
                {
                    NameRu = "Мега-дрон (Прототип TC-280)",
                    NameEn = "Mega Drone (TC-280 Prototype)",
                    FallbackMasterName = "MegaDroneMaster",
                    GetDroneDef = () => RoR2Content.DroneDefs.MegaDrone,
                    Count = megaCount
                });
            }

            // Fallback in case all counts are set to 0 in config
            if (list.Count == 0)
            {
                list.Add(new DroneSquadOption
                {
                    NameRu = "Дрон-стрелок",
                    NameEn = "Gunner Drone",
                    FallbackMasterName = "Drone1Master",
                    GetDroneDef = () => RoR2Content.DroneDefs.Drone1,
                    Count = 1
                });
            }

            return list;
        }

        public static List<DroneSquadOption> GetAlliedDroneOptions()
        {
            var list = new List<DroneSquadOption>
            {
                new DroneSquadOption
                {
                    NameRu = "Дрон-стрелок",
                    NameEn = "Gunner Drone",
                    FallbackMasterName = "Drone1Master",
                    GetDroneDef = () => RoR2Content.DroneDefs.Drone1,
                    Count = 4
                },
                new DroneSquadOption
                {
                    NameRu = "Медицинский дрон",
                    NameEn = "Healing Drone",
                    FallbackMasterName = "Drone2Master",
                    GetDroneDef = () => RoR2Content.DroneDefs.Drone2,
                    Count = 4
                },
                new DroneSquadOption
                {
                    NameRu = "Огнемётный дрон",
                    NameEn = "Flame Drone",
                    FallbackMasterName = "FlameDroneMaster",
                    GetDroneDef = () => RoR2Content.DroneDefs.FlameDrone,
                    Count = 3
                },
                new DroneSquadOption
                {
                    NameRu = "Ракетный дрон",
                    NameEn = "Missile Drone",
                    FallbackMasterName = "MissileDroneMaster",
                    GetDroneDef = () => RoR2Content.DroneDefs.MissileDrone,
                    Count = 2
                },
                new DroneSquadOption
                {
                    NameRu = "Аварийный дрон",
                    NameEn = "Emergency Drone",
                    FallbackMasterName = "EmergencyDroneMaster",
                    GetDroneDef = () => RoR2Content.DroneDefs.EmergencyDrone,
                    Count = 2
                },
                new DroneSquadOption
                {
                    NameRu = "Мега-дрон (Прототип TC-280)",
                    NameEn = "Mega Drone (TC-280 Prototype)",
                    FallbackMasterName = "MegaDroneMaster",
                    GetDroneDef = () => RoR2Content.DroneDefs.MegaDrone,
                    Count = 1
                }
            };

            try
            {
                if (MasterCatalog.FindMasterPrefab("DroneCommanderMaster") != null)
                {
                    list.Add(new DroneSquadOption
                    {
                        NameRu = "Полковник Дроунман",
                        NameEn = "Colonel Droneman",
                        FallbackMasterName = "DroneCommanderMaster",
                        GetDroneDef = () => DLC1Content.DroneDefs.DroneCommander,
                        Count = 1
                    });
                }
            }
            catch { }

            return list;
        }

        private static GameObject? ResolveDroneMasterPrefab(DroneSquadOption option)
        {
            DroneDef? def = null;
            try
            {
                def = option.GetDroneDef?.Invoke();
            }
            catch { }

            if (def != null && def.masterPrefab != null)
            {
                return def.masterPrefab;
            }

            if (!string.IsNullOrEmpty(option.FallbackMasterName))
            {
                return MasterCatalog.FindMasterPrefab(option.FallbackMasterName);
            }

            return null;
        }

        public static string ExecuteAlliedDrones(CharacterMaster master, ChaosEventDefinition? eventDef = null)
        {
            if (master == null)
            {
                eventDef?.SetDescriptionFromToken("ITEMROULETTE_CHAOS_ALLIED_DRONES_FALLBACK");
                return Localization.Get("ITEMROULETTE_CHAOS_ALLIED_DRONES_FALLBACK");
            }

            var options = GetAlliedDroneOptions();
            int choiceIndex = UnityEngine.Random.Range(0, options.Count);
            var choice = options[choiceIndex];

            if (NetworkServer.active)
            {
                ExecuteAlliedDronesServer(master, choiceIndex);
            }
            else
            {
                uint netId = master.netId.Value != 0 ? master.netId.Value : (uint)master.GetInstanceID();
                ServerEvents.Broadcast($"[ItemRoulette:SpawnAlliedDrones:{netId}:{choiceIndex}]");
            }

            if (eventDef != null)
            {
                eventDef.SetDescription(
                    Localization.FormatForLang("ru", "ITEMROULETTE_CHAOS_ALLIED_DRONES_RESULT", choice.NameRu, choice.Count),
                    Localization.FormatForLang("en", "ITEMROULETTE_CHAOS_ALLIED_DRONES_RESULT", choice.NameEn, choice.Count));
            }

            return Localization.FormatToken("ITEMROULETTE_CHAOS_ALLIED_DRONES_RESULT", choice.Name, choice.Count);
        }

        public static string ExecuteHostileDrones(CharacterMaster master, ChaosEventDefinition? eventDef = null)
        {
            if (master == null)
            {
                eventDef?.SetDescriptionFromToken("ITEMROULETTE_CHAOS_HOSTILE_DRONES_FALLBACK");
                return Localization.Get("ITEMROULETTE_CHAOS_HOSTILE_DRONES_FALLBACK");
            }

            var options = GetHostileDroneOptions();
            int choiceIndex = UnityEngine.Random.Range(0, options.Count);
            var choice = options[choiceIndex];

            if (NetworkServer.active)
            {
                ExecuteHostileDronesServer(master, choiceIndex);
            }
            else
            {
                uint netId = master.netId.Value != 0 ? master.netId.Value : (uint)master.GetInstanceID();
                ServerEvents.Broadcast($"[ItemRoulette:SpawnHostileDrones:{netId}:{choiceIndex}]");
            }

            if (eventDef != null)
            {
                eventDef.SetDescription(
                    Localization.FormatForLang("ru", "ITEMROULETTE_CHAOS_HOSTILE_DRONES_RESULT", choice.NameRu, choice.Count),
                    Localization.FormatForLang("en", "ITEMROULETTE_CHAOS_HOSTILE_DRONES_RESULT", choice.NameEn, choice.Count));
            }

            float stunSec = ModConfig.HostileDroneStunDuration != null ? ModConfig.HostileDroneStunDuration.Value : 10f;
            if (stunSec > 0f)
            {
                return Localization.Format("Призваны враждебные дроны: <color=#FF4444><b>{0}</b> (x{1})</color>! Системы дронов перезагружаются ({2:0} сек)...", "Summoned hostile rogue drones: <color=#FF4444><b>{0}</b> (x{1})</color>! Drone systems rebooting ({2:0}s)...", choice.Name, choice.Count, stunSec);
            }

            return Localization.FormatToken("ITEMROULETTE_CHAOS_HOSTILE_DRONES_RESULT", choice.Name, choice.Count);
        }

        public static void ExecuteAlliedDronesServer(CharacterMaster summoner, int choiceIndex)
        {
            if (!NetworkServer.active || summoner == null) return;
            var body = summoner.GetBody();
            if (body == null) return;

            var options = GetAlliedDroneOptions();
            if (choiceIndex < 0 || choiceIndex >= options.Count) choiceIndex = UnityEngine.Random.Range(0, options.Count);
            var choice = options[choiceIndex];

            var masterPrefab = ResolveDroneMasterPrefab(choice);
            if (masterPrefab == null)
            {
                Debug.LogWarning($"[ItemRoulette] Failed to resolve master prefab for allied drone: {choice.Name}");
                return;
            }

            int count = choice.Count;
            for (int i = 0; i < count; i++)
            {
                float angle = (i * (360f / Mathf.Max(1, count)) + UnityEngine.Random.Range(-15f, 15f)) * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 4f + Vector3.up * 2.5f;
                Vector3 spawnPos = body.corePosition + offset;

                var summon = new MasterSummon
                {
                    masterPrefab = masterPrefab,
                    position = spawnPos,
                    rotation = body.transform.rotation,
                    summonerBodyObject = body.gameObject,
                    teamIndexOverride = TeamIndex.Player,
                    ignoreTeamMemberLimit = true,
                    useAmbientLevel = true
                };

                var minionMaster = summon.Perform();
                if (minionMaster != null)
                {
                    if (minionMaster.minionOwnership != null)
                    {
                        minionMaster.minionOwnership.SetOwner(summoner);
                    }

                    var aiOwnership = minionMaster.GetComponent<RoR2.CharacterAI.AIOwnership>();
                    if (aiOwnership != null)
                    {
                        aiOwnership.ownerMaster = summoner;
                    }

                    var baseAIs = minionMaster.GetComponents<RoR2.CharacterAI.BaseAI>();
                    foreach (var ai in baseAIs)
                    {
                        if (ai != null && ai.leader != null)
                        {
                            ai.leader.gameObject = body.gameObject;
                        }
                    }

                    if (minionMaster.inventory != null && RoR2Content.Items.MinionLeash != null)
                    {
                        minionMaster.inventory.GiveItemPermanent(RoR2Content.Items.MinionLeash.itemIndex, 1);
                    }

                    var droneBody = minionMaster.GetBody();
                    if (droneBody != null)
                    {
                        string droneOwner = summoner.GetBody()?.GetUserName() ?? Localization.Get("игрока", "player");
                        droneBody.subtitleNameToken = $"ITEMROULETTE_SUBTITLE_DRONE:{droneOwner}";
                    }
                }
            }
        }

        public static void ExecuteHostileDronesServer(CharacterMaster summoner, int choiceIndex)
        {
            if (!NetworkServer.active || summoner == null) return;
            var body = summoner.GetBody();
            if (body == null) return;

            var options = GetHostileDroneOptions();
            if (choiceIndex < 0 || choiceIndex >= options.Count) choiceIndex = UnityEngine.Random.Range(0, options.Count);
            var choice = options[choiceIndex];

            var masterPrefab = ResolveDroneMasterPrefab(choice);
            if (masterPrefab == null)
            {
                Debug.LogWarning($"[ItemRoulette] Failed to resolve master prefab for hostile drone: {choice.Name}");
                return;
            }

            float spawnDist = ModConfig.HostileDroneSpawnDistance != null ? Mathf.Max(5f, ModConfig.HostileDroneSpawnDistance.Value) : 15f;
            float radius = choice.Count == 1 ? spawnDist * 1.2f : spawnDist;

            int count = choice.Count;
            for (int i = 0; i < count; i++)
            {
                float angle = (i * (360f / Mathf.Max(1, count)) + UnityEngine.Random.Range(-20f, 20f)) * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius + Vector3.up * 4f;
                Vector3 spawnPos = body.corePosition + offset;

                var summon = new MasterSummon
                {
                    masterPrefab = masterPrefab,
                    position = spawnPos,
                    rotation = Quaternion.LookRotation(body.corePosition - spawnPos),
                    summonerBodyObject = null,
                    teamIndexOverride = TeamIndex.Monster,
                    ignoreTeamMemberLimit = true,
                    useAmbientLevel = true
                };

                var monsterDroneMaster = summon.Perform();
                if (monsterDroneMaster != null)
                {
                    float stunDuration = ModConfig.HostileDroneStunDuration != null ? ModConfig.HostileDroneStunDuration.Value : 10f;
                    bool hasLifetime = ModConfig.EnableHostileDroneLifetime != null && ModConfig.EnableHostileDroneLifetime.Value;
                    float lifetime = ModConfig.HostileDroneLifetimeSeconds != null ? ModConfig.HostileDroneLifetimeSeconds.Value : 30f;

                    var controller = monsterDroneMaster.gameObject.AddComponent<HostileDroneController>();
                    controller.Initialize(monsterDroneMaster, summoner, stunDuration, hasLifetime, lifetime);

                    var droneBody = monsterDroneMaster.GetBody();
                    if (droneBody != null)
                    {
                        float dmgMult = ModConfig.HostileDroneDamageMultiplier != null ? Mathf.Clamp(ModConfig.HostileDroneDamageMultiplier.Value, 0.01f, 5.0f) : 0.10f;
                        float hpMult = ModConfig.HostileDroneHealthMultiplier != null ? Mathf.Clamp(ModConfig.HostileDroneHealthMultiplier.Value, 0.01f, 5.0f) : 0.10f;

                        droneBody.baseDamage *= dmgMult;
                        droneBody.levelDamage *= dmgMult;
                        droneBody.baseMaxHealth *= hpMult;
                        droneBody.levelMaxHealth *= hpMult;
                        droneBody.baseRegen = 0f;
                        droneBody.levelRegen = 0f;
                        droneBody.RecalculateStats();
                        if (droneBody.healthComponent != null)
                        {
                            droneBody.healthComponent.health = droneBody.healthComponent.fullHealth;
                        }

                        if (stunDuration > 0f)
                        {
                            var setStateOnHurt = droneBody.GetComponent<SetStateOnHurt>();
                            if (setStateOnHurt != null)
                            {
                                setStateOnHurt.canBeStunned = true;
                                setStateOnHurt.SetStun(stunDuration);
                            }
                            if (droneBody.skillLocator != null)
                            {
                                if (droneBody.skillLocator.primary != null) droneBody.skillLocator.primary.enabled = false;
                                if (droneBody.skillLocator.secondary != null) droneBody.skillLocator.secondary.enabled = false;
                                if (droneBody.skillLocator.utility != null) droneBody.skillLocator.utility.enabled = false;
                                if (droneBody.skillLocator.special != null) droneBody.skillLocator.special.enabled = false;
                            }
                            droneBody.subtitleNameToken = "ITEMROULETTE_SUBTITLE_HOSTILE_DRONE_REBOOTING";
                        }
                        else
                        {
                            droneBody.subtitleNameToken = "ITEMROULETTE_SUBTITLE_HOSTILE_DRONE";
                        }
                    }
                }
            }
        }

        public static void ProcessLocalAction(string action, string[] parts)
        {
            if (!NetworkServer.active) return;

            if (action == "EnableStageArtifact")
            {
                if (parts.Length > 2 && int.TryParse(parts[2], out int artIdx))
                {
                    var artDef = ArtifactCatalog.GetArtifactDef((ArtifactIndex)artIdx);
                    if (artDef != null && RunArtifactManager.instance != null)
                    {
                        RunArtifactManager.instance.SetArtifactEnabledServer(artDef, true);
                        RouletteManager.Instance.StageTemporaryArtifacts.Add((ArtifactIndex)artIdx);
                    }
                }
            }
            else if (action == "TriggerCombatShrineX2")
            {
                CharacterMaster? targetMaster = null;
                if (parts.Length > 2 && uint.TryParse(parts[2], out uint netIdVal))
                {
                    targetMaster = PlayerUtil.FindMaster(netIdVal, "");
                }
                if (targetMaster == null && PlayerCharacterMasterController.instances.Count > 0)
                {
                    targetMaster = PlayerCharacterMasterController.instances[0]?.master;
                }
                if (targetMaster != null)
                {
                    ExecuteCombatShrineX2(targetMaster);
                }
            }
            else if (action == "GrantElementalAspect")
            {
                if (parts.Length > 2 && int.TryParse(parts[2], out int aspectEqIdx))
                {
                    CharacterMaster? targetMaster = null;
                    if (parts.Length > 3 && uint.TryParse(parts[3], out uint netIdVal))
                    {
                        targetMaster = PlayerUtil.FindMaster(netIdVal, "");
                    }
                    if (targetMaster == null && PlayerCharacterMasterController.instances.Count > 0)
                    {
                        targetMaster = PlayerCharacterMasterController.instances[0]?.master;
                    }
                    if (targetMaster != null)
                    {
                        ExecuteGrantElementalAspectServer(targetMaster, (EquipmentIndex)aspectEqIdx);
                    }
                }
            }
            else if (action == "GrantAspectItem")
            {
                if (parts.Length > 2 && int.TryParse(parts[2], out int aspectItemIdx))
                {
                    CharacterMaster? targetMaster = null;
                    if (parts.Length > 3 && uint.TryParse(parts[3], out uint netIdVal))
                    {
                        targetMaster = PlayerUtil.FindMaster(netIdVal, "");
                    }
                    if (targetMaster == null && PlayerCharacterMasterController.instances.Count > 0)
                    {
                        targetMaster = PlayerCharacterMasterController.instances[0]?.master;
                    }
                    if (targetMaster != null)
                    {
                        ExecuteGrantAspectItemServer(targetMaster, (ItemIndex)aspectItemIdx);
                    }
                }
            }
            else if (action == "SpawnAlliedDrones")
            {
                if (parts.Length > 3 && uint.TryParse(parts[2], out uint pNetId) && int.TryParse(parts[3], out int choiceIdx))
                {
                    CharacterMaster? targetMaster = PlayerUtil.FindMaster(pNetId, "");
                    if (targetMaster == null && PlayerCharacterMasterController.instances.Count > 0)
                    {
                        targetMaster = PlayerCharacterMasterController.instances[0]?.master;
                    }
                    if (targetMaster != null)
                    {
                        ExecuteAlliedDronesServer(targetMaster, choiceIdx);
                    }
                }
            }
            else if (action == "SpawnHostileDrones")
            {
                if (parts.Length > 3 && uint.TryParse(parts[2], out uint pNetId) && int.TryParse(parts[3], out int choiceIdx))
                {
                    CharacterMaster? targetMaster = PlayerUtil.FindMaster(pNetId, "");
                    if (targetMaster == null && PlayerCharacterMasterController.instances.Count > 0)
                    {
                        targetMaster = PlayerCharacterMasterController.instances[0]?.master;
                    }
                    if (targetMaster != null)
                    {
                        ExecuteHostileDronesServer(targetMaster, choiceIdx);
                    }
                }
            }
        }
    }
}
