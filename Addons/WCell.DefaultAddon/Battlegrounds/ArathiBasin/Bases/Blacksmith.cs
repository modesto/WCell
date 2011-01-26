using WCell.Constants;
using WCell.Constants.GameObjects;
using WCell.RealmServer.GameObjects;

namespace WCell.Addons.Default.Battlegrounds.ArathiBasin.Bases
{
    class Blacksmith : ArathiBase
    {
        public Blacksmith(ArathiBasin instance) 
            : base(instance, null)
        {
        }

        public override string BaseName
        {
            get { return "Blacksmith"; }
        }

        protected override void SpawnNeutral()
        {
            GOEntry blacksmithBannerEntry = GOMgr.GetEntry(GOEntryId.BlacksmithBanner_2);
            FlagStand = blacksmithBannerEntry.FirstSpawnEntry.Spawn(Instance);

            GOEntry neutralBannerAuraEntry = GOMgr.GetEntry(GOEntryId.NeutralBannerAura);
            ActualAura = neutralBannerAuraEntry.SpawnEntries[(int)ArathiBases.Blacksmith].Spawn(Instance);
        }

        protected override void SpawnAlliance()
        {
            GOEntry allianceControlledFlagEntry = GOMgr.GetEntry(GOEntryId.AllianceBanner_10);
            FlagStand = allianceControlledFlagEntry.SpawnEntries[(int)ArathiBases.Blacksmith].Spawn(Instance);

            GOEntry allianceBannerAuraEntry = GOMgr.GetEntry(GOEntryId.AllianceBannerAura);
            ActualAura = allianceBannerAuraEntry.SpawnEntries[(int)ArathiBases.Blacksmith].Spawn(Instance);
        }

        protected override void SpawnHorde()
        {
            GOEntry hordeControlledFlagEntry = GOMgr.GetEntry(GOEntryId.HordeBanner_10);
            FlagStand = hordeControlledFlagEntry.SpawnEntries[(int)ArathiBases.Blacksmith].Spawn(Instance);

            GOEntry hordeBannerAuraEntry = GOMgr.GetEntry(GOEntryId.HordeBannerAura);
            ActualAura = hordeBannerAuraEntry.SpawnEntries[(int)ArathiBases.Blacksmith].Spawn(Instance);
        }

        protected override void SpawnContested()
        {
            if (Capturer.Battlegrounds.Team.Side == BattlegroundSide.Horde)
            {
                GOEntry hordeAttackFlagEntry = GOMgr.GetEntry(GOEntryId.ContestedBanner_25);
                FlagStand = hordeAttackFlagEntry.SpawnEntries[(int)ArathiBases.Blacksmith].Spawn(Instance);
            }
            else
            {
                GOEntry allianceAttackFlagEntry = GOMgr.GetEntry(GOEntryId.ContestedBanner_26);
                FlagStand = allianceAttackFlagEntry.SpawnEntries[(int)ArathiBases.Blacksmith].Spawn(Instance);
            }

            // don't know if we have to spawn neutral aura...
            GOEntry neutralBannerAuraEntry = GOMgr.GetEntry(GOEntryId.NeutralBannerAura);
            neutralBannerAuraEntry.SpawnEntries[(int)ArathiBases.Blacksmith].Spawn(Instance);
        }
    }
}