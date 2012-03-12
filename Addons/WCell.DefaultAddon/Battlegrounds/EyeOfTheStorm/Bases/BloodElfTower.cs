using WCell.Addons.Default.Lang;
using WCell.Constants.Battlegrounds;
using WCell.Constants.GameObjects;
using WCell.Constants.World;
using WCell.RealmServer.GameObjects;

namespace WCell.Addons.Default.Battlegrounds.EyeOfTheStorm.Bases
{
    class BloodElfTower : EOTSBase
    {
        public BloodElfTower(EyeOfTheStorm instance) 
            : base(instance)
        {
            baseNeutral = WorldStateId.EOTSBloodElfTowerUncontrolled;
            baseAllianceContested = WorldStateId.EOTSBloodElfTowerAllianceContested;
            baseAllianceControlled = WorldStateId.EOTSBloodElfTowerAllianceControl;
            baseHordeContested = WorldStateId.EOTSBloodElfTowerHordeContested;
            baseHordeControlled = WorldStateId.EOTSBloodElfTowerHordeControl;

            //Names = DefaultAddonLocalizer.Instance.GetTranslations(AddonMsgKey.ABBlacksmith);
        }

        protected override void AddSpawns()
        {
            neutralBannerSpawn = GOMgr.GetEntry(GOEntryId.VisualBannerNeutral).FirstSpawnEntry;
            allianceBannerSpawn = GOMgr.GetEntry(GOEntryId.VisualBannerAlliance).SpawnEntries[(int)EOTSBases.BloodElfTower];
            hordeBannerSpawn = GOMgr.GetEntry(GOEntryId.VisualBannerHorde).SpawnEntries[(int)EOTSBases.BloodElfTower];
        }

        public override string BaseName
        {
            get { return "Blood Elf Tower"; }
        }
    }
}
