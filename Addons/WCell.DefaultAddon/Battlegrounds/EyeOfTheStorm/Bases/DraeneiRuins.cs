using WCell.Addons.Default.Lang;
using WCell.Constants.Battlegrounds;
using WCell.Constants.GameObjects;
using WCell.Constants.World;
using WCell.RealmServer.GameObjects;

namespace WCell.Addons.Default.Battlegrounds.EyeOfTheStorm.Bases
{
    class DraeneiRuins : EOTSBase
    {
        public DraeneiRuins(EyeOfTheStorm instance)
            : base(instance)
        {
            baseNeutral = WorldStateId.EOTSDraeneiRuinsUncontrolled;
            baseAllianceContested = WorldStateId.EOTSDraeneiRuinsAllianceContested;
            baseAllianceControlled = WorldStateId.EOTSDraeneiRuinsAllianceControl;
            baseHordeContested = WorldStateId.EOTSDraeneiRuinsHordeContested;
            baseHordeControlled = WorldStateId.EOTSDraeneiRuinsHordeControl;
        }

        protected override void AddSpawns()
        {
            neutralBannerSpawn = GOMgr.GetEntry(GOEntryId.VisualBannerNeutral).FirstSpawnEntry;
            allianceBannerSpawn = GOMgr.GetEntry(GOEntryId.VisualBannerAlliance).SpawnEntries[(int)EOTSBases.DraeneiRuins];
            hordeBannerSpawn = GOMgr.GetEntry(GOEntryId.VisualBannerHorde).SpawnEntries[(int)EOTSBases.DraeneiRuins];
        }

        public override string BaseName
        {
            get { return "Draenei Ruins"; }
        }
    }
}