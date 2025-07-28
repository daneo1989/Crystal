using System.Collections.Generic;

namespace Server.MirEnvir
{
    public class TitleStatBonus
    {
        // Classic Mir2 stats
        public int MinDC, MaxDC;
        public int MinMC, MaxMC;
        public int MinSC, MaxSC;
        public int MinAC, MaxAC;
        public int MinMAC, MaxMAC;
        public int HP, MP;
        public int Accuracy, Agility;
        public int Luck, AttackSpeed;
        public int BagWeight, HandWeight, WearWeight;
        public int ExpRatePercent, DropRatePercent, GoldRatePercent;
        public int CritRate, CritDamage;
        // Add any others below as needed

        public TitleStatBonus(
            int minDC = 0, int maxDC = 0,
            int minMC = 0, int maxMC = 0,
            int minSC = 0, int maxSC = 0,
            int minAC = 0, int maxAC = 0,
            int minMAC = 0, int maxMAC = 0,
            int hp = 0, int mp = 0,
            int accuracy = 0, int agility = 0,
            int luck = 0, int attackSpeed = 0,
            int bagWeight = 0, int handWeight = 0, int wearWeight = 0,
            int expRatePercent = 0, int dropRatePercent = 0, int goldRatePercent = 0,
            int critRate = 0, int critDamage = 0
        // Add more params as needed
        )
        {
            MinDC = minDC; MaxDC = maxDC;
            MinMC = minMC; MaxMC = maxMC;
            MinSC = minSC; MaxSC = maxSC;
            MinAC = minAC; MaxAC = maxAC;
            MinMAC = minMAC; MaxMAC = maxMAC;
            HP = hp; MP = mp;
            Accuracy = accuracy; Agility = agility;
            Luck = luck; AttackSpeed = attackSpeed;
            BagWeight = bagWeight; HandWeight = handWeight; WearWeight = wearWeight;
            ExpRatePercent = expRatePercent; DropRatePercent = dropRatePercent; GoldRatePercent = goldRatePercent;
            CritRate = critRate; CritDamage = critDamage;
        }
    }

    public static class TitleBonuses
    {
        public static readonly Dictionary<string, TitleStatBonus> Bonuses = new()
        {
            // EXAMPLES:
            ["Dragon Slayer"] = new TitleStatBonus(maxDC: 10, maxAC: 5, critRate: 2, critDamage: 8),
            ["Archmage"] = new TitleStatBonus(maxMC: 15, mp: 20, expRatePercent: 5),
            ["Champion"] = new TitleStatBonus(maxDC: 5, maxSC: 5, hp: 25, luck: 1),
            ["Iron Defender"] = new TitleStatBonus(maxAC: 10, maxMAC: 8, hp: 50),
            ["Life Giver"] = new TitleStatBonus(hp: 100, mp: 50, dropRatePercent: 3),
            ["Golden Hero"] = new TitleStatBonus(maxDC: 6, accuracy: 3, goldRatePercent: 10, attackSpeed: 2),
            // Add more titles as you wish!
        };
    }
}