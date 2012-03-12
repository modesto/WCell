using WCell.Addons.Default.Lang;
using WCell.Constants.Battlegrounds;
using WCell.Constants.GameObjects;
using WCell.Constants.World;
using WCell.RealmServer.GameObjects;

namespace WCell.Addons.Default.Battlegrounds.EyeOfTheStorm.Bases
{
    class FelReaverRuins : EOTSBase
    {
        public FelReaverRuins(EyeOfTheStorm instance)
            : base(instance)
        {
            baseNeutral = WorldStateId.EOTSFelReaverUncontrolled;
            baseAllianceContested = WorldStateId.EOTSFelReaverAllianceContested;
            baseAllianceControlled = WorldStateId.EOTSFelReaverAllianceControl;
            baseHordeContested = WorldStateId.EOTSFelReaverHordeContested;
            baseHordeControlled = WorldStateId.EOTSFelReaverHordeControl;

            //Names = DefaultAddonLocalizer.Instance.GetTranslations(AddonMsgKey.ABBlacksmith);
        }

        protected override void AddSpawns()
        {
            neutralBannerSpawn = GOMgr.GetEntry(GOEntryId.VisualBannerNeutral).FirstSpawnEntry;
            allianceBannerSpawn = GOMgr.GetEntry(GOEntryId.VisualBannerAlliance).SpawnEntries[(int)EOTSBases.FelReaverRunis];
            hordeBannerSpawn = GOMgr.GetEntry(GOEntryId.VisualBannerHorde).SpawnEntries[(int)EOTSBases.FelReaverRunis];
        }

        public override string BaseName
        {
            get { return "Fel Reaver Ruins"; }
        }
    }
}