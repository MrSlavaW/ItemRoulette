using System;
using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace ItemRoulette.Core
{
    public enum RouletteColor
    {
        Green,
        Red,
        Black
    }

    public enum CasinoBetType
    {
        None,
        // Colors (1:1 payout, x2)
        Red,
        Black,
        // Parity (1:1 payout, x2)
        Even,
        Odd,
        // Halves (1:1 payout, x2)
        Low1_18,
        High19_36,
        // Dozens (2:1 payout, x3)
        Dozen1_12,
        Dozen13_24,
        Dozen25_36,
        // Columns (2:1 payout, x3)
        Column1,
        Column2,
        Column3,
        // Single Number (35:1 payout, x36)
        StraightNumber
    }

    public struct RoulettePocket
    {
        public int Number;
        public RouletteColor Color;
        public string Label;
        public ChaosPolarity ChaosPolarity;
        public ChaosEventType ChaosEvent;

        public RoulettePocket(int number, RouletteColor color, ChaosPolarity polarity = ChaosPolarity.None, ChaosEventType chaosEvent = ChaosEventType.None)
        {
            Number = number;
            Color = color;
            Label = number.ToString();
            ChaosPolarity = polarity;
            ChaosEvent = chaosEvent;
        }
    }

    [Serializable]
    public struct CasinoBet
    {
        public CasinoBetType Type;
        public int TargetNumber;
        public int Amount;

        public CasinoBet(CasinoBetType type, int targetNumber, int amount)
        {
            Type = type;
            TargetNumber = targetNumber;
            Amount = amount;
        }
    }

    public class CasinoFeltBets
    {
        public int[] NumberBets = new int[37]; // 0 to 36
        public int[] ColumnBets = new int[3];  // 0: Col1, 1: Col2, 2: Col3
        public int[] DozenBets = new int[3];   // 0: 1st12, 1: 2nd12, 2: 3rd12
        public int BetLow1_18;
        public int BetHigh19_36;
        public int BetEven;
        public int BetOdd;
        public int BetRed;
        public int BetBlack;

        public void Clear()
        {
            Array.Clear(NumberBets, 0, NumberBets.Length);
            Array.Clear(ColumnBets, 0, ColumnBets.Length);
            Array.Clear(DozenBets, 0, DozenBets.Length);
            BetLow1_18 = 0;
            BetHigh19_36 = 0;
            BetEven = 0;
            BetOdd = 0;
            BetRed = 0;
            BetBlack = 0;
        }

        public int GetTotalMoneyBet()
        {
            // Multi-selection: only the single bet stake is paid once in gold;
            // additional sectors cost Lunar Coins instead of multiplying gold price.
            int maxBet = 0;
            for (int i = 0; i <= 36; i++) if (NumberBets[i] > maxBet) maxBet = NumberBets[i];
            for (int i = 0; i < 3; i++) if (ColumnBets[i] > maxBet) maxBet = ColumnBets[i];
            for (int i = 0; i < 3; i++) if (DozenBets[i] > maxBet) maxBet = DozenBets[i];
            if (BetLow1_18 > maxBet) maxBet = BetLow1_18;
            if (BetHigh19_36 > maxBet) maxBet = BetHigh19_36;
            if (BetEven > maxBet) maxBet = BetEven;
            if (BetOdd > maxBet) maxBet = BetOdd;
            if (BetRed > maxBet) maxBet = BetRed;
            if (BetBlack > maxBet) maxBet = BetBlack;
            return maxBet;
        }

        public bool HasAnyOutsideBet()
        {
            for (int i = 0; i < 3; i++) if (ColumnBets[i] > 0) return true;
            for (int i = 0; i < 3; i++) if (DozenBets[i] > 0) return true;
            return BetLow1_18 > 0 || BetHigh19_36 > 0 || BetEven > 0 || BetOdd > 0 || BetRed > 0 || BetBlack > 0;
        }

        public bool HasAnyNumberBet()
        {
            for (int i = 0; i <= 36; i++) if (NumberBets[i] > 0) return true;
            return false;
        }

        public int GetNumberBetsCount()
        {
            int count = 0;
            for (int i = 0; i <= 36; i++) if (NumberBets[i] > 0) count++;
            return count;
        }

        public int GetActiveBetSectorsCount()
        {
            int count = 0;
            for (int i = 0; i <= 36; i++) if (NumberBets[i] > 0) count++;
            for (int i = 0; i < 3; i++) if (ColumnBets[i] > 0) count++;
            for (int i = 0; i < 3; i++) if (DozenBets[i] > 0) count++;
            if (BetLow1_18 > 0) count++;
            if (BetHigh19_36 > 0) count++;
            if (BetEven > 0) count++;
            if (BetOdd > 0) count++;
            if (BetRed > 0) count++;
            if (BetBlack > 0) count++;
            return count;
        }

        public int GetRequiredLunarCoins()
        {
            // Multi-selection is strictly for numbers (0-36).
            // Outside bets (color, row, dozen) cannot be multi-selected.
            return CasinoRouletteHelper.CalculateMultiBetLunarCost(GetNumberBetsCount());
        }

        public List<CasinoBet> GetAllActiveBets()
        {
            var list = new List<CasinoBet>();
            for (int i = 0; i <= 36; i++)
            {
                if (NumberBets[i] > 0) list.Add(new CasinoBet(CasinoBetType.StraightNumber, i, NumberBets[i]));
            }
            if (ColumnBets[0] > 0) list.Add(new CasinoBet(CasinoBetType.Column1, 0, ColumnBets[0]));
            if (ColumnBets[1] > 0) list.Add(new CasinoBet(CasinoBetType.Column2, 0, ColumnBets[1]));
            if (ColumnBets[2] > 0) list.Add(new CasinoBet(CasinoBetType.Column3, 0, ColumnBets[2]));

            if (DozenBets[0] > 0) list.Add(new CasinoBet(CasinoBetType.Dozen1_12, 0, DozenBets[0]));
            if (DozenBets[1] > 0) list.Add(new CasinoBet(CasinoBetType.Dozen13_24, 0, DozenBets[1]));
            if (DozenBets[2] > 0) list.Add(new CasinoBet(CasinoBetType.Dozen25_36, 0, DozenBets[2]));

            if (BetLow1_18 > 0) list.Add(new CasinoBet(CasinoBetType.Low1_18, 0, BetLow1_18));
            if (BetHigh19_36 > 0) list.Add(new CasinoBet(CasinoBetType.High19_36, 0, BetHigh19_36));
            if (BetEven > 0) list.Add(new CasinoBet(CasinoBetType.Even, 0, BetEven));
            if (BetOdd > 0) list.Add(new CasinoBet(CasinoBetType.Odd, 0, BetOdd));
            if (BetRed > 0) list.Add(new CasinoBet(CasinoBetType.Red, 0, BetRed));
            if (BetBlack > 0) list.Add(new CasinoBet(CasinoBetType.Black, 0, BetBlack));

            return list;
        }
    }

    public static class CasinoRouletteHelper
    {
        // Authentic European single-zero wheel pockets in exact counter-clockwise order
        public static readonly int[] EuropeanWheelOrder = new int[]
        {
            0, 32, 15, 19, 4, 21, 2, 25, 17, 34, 6, 27, 13, 36, 11, 30, 8, 23, 10, 5, 24, 16, 33, 1, 20, 14, 31, 9, 22, 18, 29, 7, 28, 12, 35, 3, 26
        };

        public static readonly HashSet<int> RedNumbers = new HashSet<int>
        {
            1, 3, 5, 7, 9, 12, 14, 16, 18, 19, 21, 23, 25, 27, 30, 32, 34, 36
        };

        public static readonly HashSet<int> BlackNumbers = new HashSet<int>
        {
            2, 4, 6, 8, 10, 11, 13, 15, 17, 20, 22, 24, 26, 28, 29, 31, 33, 35
        };

        public static RouletteColor GetColorForNumber(int number)
        {
            if (number == 0) return RouletteColor.Green;
            return RedNumbers.Contains(number) ? RouletteColor.Red : RouletteColor.Black;
        }

        public static Color GetDisplayColor(RouletteColor color)
        {
            switch (color)
            {
                case RouletteColor.Green: return new Color(0.18f, 0.80f, 0.44f, 1f); // Vibrant emerald
                case RouletteColor.Red: return new Color(0.90f, 0.22f, 0.22f, 1f);   // Crimson red
                case RouletteColor.Black: return new Color(0.18f, 0.20f, 0.25f, 1f); // Dark obsidian
                default: return Color.gray;
            }
        }

        public static string GetColorName(RouletteColor color)
        {
            switch (color)
            {
                case RouletteColor.Green: return Localization.Get("ЗЕЛЁНОЕ", "GREEN");
                case RouletteColor.Red: return Localization.Get("КРАСНОЕ", "RED");
                case RouletteColor.Black: return Localization.Get("ЧЁРНОЕ", "BLACK");
                default: return "";
            }
        }

        public static string GetColorNameRussian(RouletteColor color) => GetColorName(color);

        public static string GetBetTypeName(CasinoBetType type, int targetNumber = 0)
        {
            switch (type)
            {
                case CasinoBetType.Red: return Localization.Get("Красное (x2)", "Red (x2)");
                case CasinoBetType.Black: return Localization.Get("Чёрное (x2)", "Black (x2)");
                case CasinoBetType.Even: return Localization.Get("Чётное (x2)", "Even (x2)");
                case CasinoBetType.Odd: return Localization.Get("Нечётное (x2)", "Odd (x2)");
                case CasinoBetType.Low1_18: return Localization.Get("1-18 (x2)", "1-18 Low (x2)");
                case CasinoBetType.High19_36: return Localization.Get("19-36 (x2)", "19-36 High (x2)");
                case CasinoBetType.Dozen1_12: return Localization.Get("1-я Дюжина [1-12] (x3)", "1st Dozen [1-12] (x3)");
                case CasinoBetType.Dozen13_24: return Localization.Get("2-я Дюжина [13-24] (x3)", "2nd Dozen [13-24] (x3)");
                case CasinoBetType.Dozen25_36: return Localization.Get("3-я Дюжина [25-36] (x3)", "3rd Dozen [25-36] (x3)");
                case CasinoBetType.Column1: return Localization.Get("1-я Колонка (x3)", "1st Column (x3)");
                case CasinoBetType.Column2: return Localization.Get("2-я Колонка (x3)", "2nd Column (x3)");
                case CasinoBetType.Column3: return Localization.Get("3-я Колонка (x3)", "3rd Column (x3)");
                case CasinoBetType.StraightNumber:
                    string colorLabel = GetColorName(GetColorForNumber(targetNumber));
                    return Localization.Format("Число {0} [{1}] (x36)", "Number {0} [{1}] (x36)", targetNumber, colorLabel);
                default: return Localization.Get("Нет ставки", "No Bet");
            }
        }

        public static string GetBetTypeNameRussian(CasinoBetType type, int targetNumber = 0) => GetBetTypeName(type, targetNumber);

        public static bool DoesBetWin(CasinoBetType betType, int targetNumber, int rolledNumber, out int payoutMultiplier)
        {
            payoutMultiplier = 0;
            if (betType == CasinoBetType.None) return false;

            // Green Zero: only straight number on 0 wins. All outside bets lose on 0!
            if (rolledNumber == 0)
            {
                if (betType == CasinoBetType.StraightNumber && targetNumber == 0)
                {
                    payoutMultiplier = 36;
                    return true;
                }
                return false;
            }

            var color = GetColorForNumber(rolledNumber);
            bool isEven = (rolledNumber % 2 == 0);

            switch (betType)
            {
                case CasinoBetType.Red:
                    payoutMultiplier = 2;
                    return color == RouletteColor.Red;

                case CasinoBetType.Black:
                    payoutMultiplier = 2;
                    return color == RouletteColor.Black;

                case CasinoBetType.Even:
                    payoutMultiplier = 2;
                    return isEven;

                case CasinoBetType.Odd:
                    payoutMultiplier = 2;
                    return !isEven;

                case CasinoBetType.Low1_18:
                    payoutMultiplier = 2;
                    return rolledNumber >= 1 && rolledNumber <= 18;

                case CasinoBetType.High19_36:
                    payoutMultiplier = 2;
                    return rolledNumber >= 19 && rolledNumber <= 36;

                case CasinoBetType.Dozen1_12:
                    payoutMultiplier = 3;
                    return rolledNumber >= 1 && rolledNumber <= 12;

                case CasinoBetType.Dozen13_24:
                    payoutMultiplier = 3;
                    return rolledNumber >= 13 && rolledNumber <= 24;

                case CasinoBetType.Dozen25_36:
                    payoutMultiplier = 3;
                    return rolledNumber >= 25 && rolledNumber <= 36;

                case CasinoBetType.Column1: // 1, 4, 7, 10, 13, 16, 19, 22, 25, 28, 31, 34
                    payoutMultiplier = 3;
                    return rolledNumber % 3 == 1;

                case CasinoBetType.Column2: // 2, 5, 8, 11, 14, 17, 20, 23, 26, 29, 32, 35
                    payoutMultiplier = 3;
                    return rolledNumber % 3 == 2;

                case CasinoBetType.Column3: // 3, 6, 9, 12, 15, 18, 21, 24, 27, 30, 33, 36
                    payoutMultiplier = 3;
                    return rolledNumber % 3 == 0;

                case CasinoBetType.StraightNumber:
                    payoutMultiplier = 36;
                    return rolledNumber == targetNumber;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Calculates total Lunar Coin cost for multi-selecting numbers:
        /// 1 number = 0 ☾ (free)
        /// 2 numbers = 4 ☾ (+4)
        /// 3 numbers = 12 ☾ (+8)
        /// 4 numbers = 28 ☾ (+16)
        /// 5 numbers = 60 ☾ (+32)
        /// Formula: 4 * (2^(count - 1) - 1)
        /// </summary>
        public static int CalculateMultiBetLunarCost(int numberCount)
        {
            if (numberCount <= 1) return 0;
            int extra = numberCount - 1;
            if (extra >= 29) return int.MaxValue;
            long cost = 4L * ((1L << extra) - 1L);
            return (int)Math.Min(cost, (long)int.MaxValue);
        }

        /// <summary>
        /// Returns the additional Lunar Coin cost to add one more number cell
        /// when currentNumberCount numbers are already selected:
        /// when 1 selected -> next costs 4
        /// when 2 selected -> next costs 8
        /// when 3 selected -> next costs 16
        /// when 4 selected -> next costs 32
        /// </summary>
        public static int GetNextCellLunarCost(int currentNumberCount)
        {
            if (currentNumberCount <= 0) return 0;
            int extraIndex = currentNumberCount - 1; // 0 for 1st extra, 1 for 2nd extra...
            if (extraIndex >= 29) return int.MaxValue;
            long cost = 4L * (1L << extraIndex);
            return (int)Math.Min(cost, (long)int.MaxValue);
        }

        public static uint CalculateMultiBetPayout(IEnumerable<CasinoBet>? bets, int winningNumber)
        {
            if (bets == null) return 0;
            uint totalPayout = 0;
            foreach (var bet in bets)
            {
                if (bet.Amount <= 0) continue;
                if (DoesBetWin(bet.Type, bet.TargetNumber, winningNumber, out int multiplier))
                {
                    totalPayout += (uint)(bet.Amount * multiplier);
                }
            }
            return totalPayout;
        }

        public static readonly List<RoulettePocket> SpinHistory = new List<RoulettePocket>();
        public static event Action? OnSpinHistoryChanged;

        public static void RecordSpinResult(RoulettePocket pocket)
        {
            lock (SpinHistory)
            {
                SpinHistory.Insert(0, pocket);
                if (SpinHistory.Count > 10)
                {
                    SpinHistory.RemoveAt(SpinHistory.Count - 1);
                }
            }
            try
            {
                OnSpinHistoryChanged?.Invoke();
            }
            catch { }
        }

        public static void ClearSpinHistory()
        {
            lock (SpinHistory)
            {
                SpinHistory.Clear();
            }
            try
            {
                OnSpinHistoryChanged?.Invoke();
            }
            catch { }
        }

        public static (uint totalPrice, uint levelStagePart, uint itemsPart) CalculateLifeBetPrice(CharacterMaster? master)
        {
            if (master == null) return (100, 50, 50);

            var body = master.GetBody();
            int level = body != null ? Mathf.Max(1, Mathf.FloorToInt(body.level)) : 1;
            int stage = Run.instance != null ? Mathf.Max(1, Run.instance.stageClearCount + 1) : 1;

            int baseCfg = Configuration.ModConfig.LifeBetLevelStageBaseCost != null ? Configuration.ModConfig.LifeBetLevelStageBaseCost.Value : 25;
            uint scaledBaseCost = Run.instance != null ? (uint)Mathf.Max(baseCfg, Run.instance.GetDifficultyScaledCost(baseCfg)) : (uint)baseCfg;

            // 1. Level * Stage component
            uint levelStagePart = (uint)(level * stage * scaledBaseCost);

            // 2. Cost of all player items
            uint itemsPart = 0;
            if (master.inventory != null)
            {
                var inv = master.inventory;
                foreach (var itemIndex in inv.itemAcquisitionOrder)
                {
                    int count = inv.GetItemCountEffective(itemIndex);
                    if (count <= 0) continue;

                    var def = ItemCatalog.GetItemDef(itemIndex);
                    if (def == null || def.hidden) continue;

                    uint itemCost = scaledBaseCost;
                    switch (def.tier)
                    {
                        case ItemTier.Tier1: itemCost = scaledBaseCost; break;
                        case ItemTier.Tier2: itemCost = scaledBaseCost * 2; break;
                        case ItemTier.Tier3: itemCost = scaledBaseCost * 16; break;
                        case ItemTier.Boss: itemCost = scaledBaseCost * 6; break;
                        case ItemTier.Lunar: itemCost = scaledBaseCost * 4; break;
                        case ItemTier.VoidTier1: itemCost = (uint)(scaledBaseCost * 1.5f); break;
                        case ItemTier.VoidTier2: itemCost = scaledBaseCost * 3; break;
                        case ItemTier.VoidTier3: itemCost = scaledBaseCost * 20; break;
                        default: itemCost = scaledBaseCost; break;
                    }
                    itemsPart += (uint)(itemCost * count);
                }

                if (PlayerUtil.GetStoredEquipmentIndex(inv) != EquipmentIndex.None)
                {
                    itemsPart += scaledBaseCost * 2;
                }
            }

            uint total = levelStagePart + itemsPart;
            if (total == 0) total = scaledBaseCost;
            return (total, levelStagePart, itemsPart);
        }
    }
}
