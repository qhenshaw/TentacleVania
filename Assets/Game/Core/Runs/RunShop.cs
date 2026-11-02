using System;
using System.Collections.Generic;
using TentacleVania.Core.Progression;
using TentacleVania.Core.Saves;
using UnityEngine;

namespace TentacleVania.Core.Runs
{
    public static class RunShop
    {
        public const int BaseOffers = 3;
        public const int HealPrice = 150;
        public const float HealFraction = 0.5f;

        private static readonly int[] MutationPriceByNotches = { 120, 250, 400 };
        private static readonly List<int> Sold = new(8);
        private static int _visit = -1;

        public static int MutationPrice(int notches) => MutationPriceByNotches[Mathf.Clamp(notches, 1, MutationPriceByNotches.Length) - 1];

        public static int Offers(MetaState meta) => BaseOffers + MotherCall.Count(meta, UnlockKind.HoarderTier);

        public static int Visit => unchecked(RunSession.Seed * 31 + RunSession.Floor * 1009 + RunSession.Step);

        public static System.Random StockRng() => new(unchecked(Visit * 7 + 17));

        public static bool IsSold(int offer)
        {
            Refresh();
            return Sold.Contains(offer);
        }

        public static bool TryBuy(int offer, int price)
        {
            Refresh();
            if (Sold.Contains(offer) || !Biomass.Spend(price))
                return false;
            Sold.Add(offer);
            Autosave.Request(AutosaveReason.Purchase);
            return true;
        }

        public static bool TryBuyHeal() => Biomass.Spend(HealPrice);

        private static void Refresh()
        {
            if (_visit == Visit)
                return;
            _visit = Visit;
            Sold.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _visit = -1;
            Sold.Clear();
        }
    }
}
