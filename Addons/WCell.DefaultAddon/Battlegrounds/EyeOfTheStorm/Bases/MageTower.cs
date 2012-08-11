using WCell.Addons.Default.Lang;
using WCell.Constants.Battlegrounds;
using WCell.Constants.GameObjects;
using WCell.Constants.World;
using WCell.RealmServer.GameObjects;

namespace WCell.Addons.Default.Battlegrounds.EyeOfTheStorm.Bases
{
    class MageTower : EOTSBase
    {
        public MageTower(EyeOfTheStorm instance)
            : base(instance)
        {
            baseNeutral = WorldStateId.EOTSMageTowerUncontrolled;
            baseAllianceContested = WorldStateId.EOTSMageTowerAllianceContested;
            baseAllianceControlled = WorldStateId.EOTSMageTowerAllianceControl;
            baseHordeContested = WorldStateId.EOTSMageTowerHordeContested;
            baseHordeControlled = WorldStateId.EOTSMageTowerHordeControl;
        }

        protected override void AddSpawns()
        {
            neutralBannerSpawn = GOMgr.GetEntry(GOEntryId.VisualBannerNeutral).FirstSpawnEntry;
            allianceBannerSpawn = GOMgr.GetEntry(GOEntryId.VisualBannerAlliance).SpawnEntries[(int)EOTSBases.MageTower];
            hordeBannerSpawn = GOMgr.GetEntry(GOEntryId.VisualBannerHorde).SpawnEntries[(int)EOTSBases.MageTower];
        }

        public override string BaseName
        {
            get { return "Mage Tower"; }
        }
    }
}