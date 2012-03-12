using System.Linq;
using WCell.Addons.Default.Battlegrounds.EyeOfTheStorm.Bases;
using WCell.Addons.Default.Lang;
using WCell.Constants;
using WCell.Constants.AreaTriggers;
using WCell.Constants.Battlegrounds;
using WCell.Constants.Factions;
using WCell.Constants.GameObjects;
using WCell.Constants.World;
using WCell.Core.Initialization;
using WCell.Core.Timers;
using WCell.RealmServer.AreaTriggers;
using WCell.RealmServer.Battlegrounds;
using WCell.RealmServer.Chat;
using WCell.RealmServer.Entities;
using WCell.RealmServer.GameObjects;
using WCell.RealmServer.Handlers;
using WCell.RealmServer.Lang;
using WCell.Util.Variables;

namespace WCell.Addons.Default.Battlegrounds.EyeOfTheStorm
{
	public class EyeOfTheStorm : Battleground
	{
        #region Static Fields
        [Variable("EOTSMaxScore")]
	    public static int MaxScoreDefault
	    {
            get { return Constants.World.WorldStates.GetState(WorldStateId.EOTSMaxResources).DefaultValue; }
            set { Constants.World.WorldStates.GetState(WorldStateId.EOTSMaxResources).DefaultValue = value; }
	    }

        [Variable("EOTSNearVictoryScore")]
        public static int NearVictoryScoreDefault
        {
            get { return Constants.World.WorldStates.GetState(WorldStateId.EOTSNearVictoryWarning).DefaultValue; }
            set { Constants.World.WorldStates.GetState(WorldStateId.EOTSNearVictoryWarning).DefaultValue = value; }
        }

        static EyeOfTheStorm()
        {
            MaxScoreDefault = 1600;
            NearVictoryScoreDefault = 1400;
        }
        #endregion

        #region Fields

        private GameObject _hForcefield;
        private GameObject _aForcefield;
        private GameObject _netherstormFlag;

        public EOTSBase[] Bases;
        public int MaxScore;
        public int NearVictoryScore;
        public bool isInformatedNearVictory;
        private AreaTrigger _felReaverAT;
        #endregion

        #region Props
        public int HordeScore
        {
            get { return WorldStates.GetInt32(WorldStateId.EOTSResourcesHorde); }
            set
            {
                WorldStates.SetInt32(WorldStateId.EOTSResourcesHorde, value);
                if (value >= MaxScore)
                {
                    Winner = HordeTeam;
                    FinishFight();
                }
                if (value >= NearVictoryScore && !isInformatedNearVictory)
                {
                    InformNearVictory(HordeTeam, value);
                }
            }
        }

        public int AllianceScore
        {
            get { return WorldStates.GetInt32(WorldStateId.EOTSResourcesAlliance); }
            set
            {
                WorldStates.SetInt32(WorldStateId.EOTSResourcesAlliance, value);
                if (value >= MaxScore)
                {
                    Winner = AllianceTeam;
                    FinishFight(); 
                }
                if (value >= NearVictoryScore && !isInformatedNearVictory)
                {
                    InformNearVictory(AllianceTeam, value);
                }
            }
        }

	    public int HordeBaseCount 
        { 
            get { return WorldStates.GetInt32(WorldStateId.EOTSHordeBases); }
            set
            { 
                WorldStates.SetInt32(WorldStateId.EOTSHordeBases, value);
            }
        }

	    public int AllianceBaseCount
        {
            get { return WorldStates.GetInt32(WorldStateId.EOTSAllianceBases); }
            set
            {
                WorldStates.SetInt32(WorldStateId.EOTSAllianceBases, value);
            }
        }

#endregion

    public EyeOfTheStorm()
    {
        _template = BattlegroundMgr.GetTemplate(BattlegroundId.EyeOfTheStorm);
        Bases = new EOTSBase[(int)EOTSBases.End];
    }   
    #region Overrides
        protected override void InitMap()
        {
            base.InitMap();
            Bases[(int)EOTSBases.BloodElfTower] = new BloodElfTower(this);
            Bases[(int)EOTSBases.DraeneiRuins] = new DraeneiRuins(this);
            Bases[(int)EOTSBases.FelReaverRunis] = new FelReaverRuins(this);
            Bases[(int)EOTSBases.MageTower] = new MageTower(this);
            RegisterEvents();
            MaxScore = MaxScoreDefault;
            NearVictoryScore = NearVictoryScoreDefault;
            _felReaverAT = AreaTriggerMgr.GetTrigger(AreaTriggerId.EyeOfTheStormFelReaverRuinsPad);
        }

        protected override void OnStart()
        {
            base.OnStart();
            _hForcefield.State = GameObjectState.Disabled;
            _aForcefield.State = GameObjectState.Disabled;
            Characters.SendSystemMessage("The Battle Begins!");
        }

        protected override void OnFinish(bool disposing)
        {
            base.OnFinish(disposing);
        }

        protected override void OnPrepareHalftime()
        {
            base.OnPrepareHalftime();
            Characters.SendSystemMessage("The Battle Begins Shortly");
        }

        protected override void OnPrepareBegin()
        {
            base.OnPrepareBegin();
            Characters.SendSystemMessage("Prepatation Begins");
        }

        protected override void OnLeave(Character chr)
        {
            base.OnLeave(chr);
            Characters.SendSystemMessage(DefaultAddonLocalizer.Instance.GetTranslations(AddonMsgKey.ABOnLeave), chr.Name);
        }
       
        protected override void OnEnter(Character chr)
        {
            base.OnEnter(chr);
            Characters.SendSystemMessage(DefaultAddonLocalizer.Instance.GetTranslations(AddonMsgKey.ABOnEnter), chr.Name);
        }
        
        protected override void SpawnGOs()
        {
            base.SpawnGOs();
            GOEntry allianceDoorEntry = GOMgr.GetEntry(GOEntryId.Forcefield000);
            GOEntry hordeDoorEntry = GOMgr.GetEntry(GOEntryId.Forcefield001);
            GOEntry netherstormFlagEntry = GOMgr.GetEntry(GOEntryId.NetherstormFlag);

            _hForcefield = allianceDoorEntry.FirstSpawnEntry.Spawn(this);
            _aForcefield = hordeDoorEntry.FirstSpawnEntry.Spawn(this);
            _netherstormFlag = netherstormFlagEntry.FirstSpawnEntry.Spawn(this);
        }
        
        public override void FinishFight()
        {
            base.FinishFight();
        }

        #endregion
        private void TestTrigger(Character chr)
        {
            if (_felReaverAT.CheckTrigger(chr) == true)
            {
                Characters.SendSystemMessage("Fel Reaver Ruins Triggered");
            }
        }

        private void InformNearVictory(BattlegroundTeam team, int score)
        {
            Characters.SendSystemMessage(DefaultAddonLocalizer.Instance.GetTranslations(AddonMsgKey.ABNearVictory), team.Side.ToString(), score);
            MiscHandler.SendPlaySoundToMap(this, (uint)ABSounds.NearVictory);
            isInformatedNearVictory = true;
        }

        #region Spell/GO fixes and event registration
        [Initialization]
        [DependentInitialization(typeof(GOMgr))]
        public static void FixGOs()
        {
            GOEntry allianceDoorEntry = GOMgr.GetEntry(GOEntryId.Forcefield000);
            GOEntry hordeDoorEntry = GOMgr.GetEntry(GOEntryId.Forcefield001);

            allianceDoorEntry.FirstSpawnEntry.State = GameObjectState.Enabled;
            allianceDoorEntry.Flags |= GameObjectFlags.DoesNotDespawn | GameObjectFlags.InUse;
            allianceDoorEntry.FirstSpawnEntry.AutoSpawns = false;

            hordeDoorEntry.FirstSpawnEntry.State = GameObjectState.Enabled;
            hordeDoorEntry.Flags |= GameObjectFlags.DoesNotDespawn | GameObjectFlags.InUse;
            hordeDoorEntry.FirstSpawnEntry.AutoSpawns = false;

            GOEntry bannerNeutralEntry = GOMgr.GetEntry(GOEntryId.VisualBannerNeutral);
            GOEntry bannerHordeEntry = GOMgr.GetEntry(GOEntryId.VisualBannerHorde);
            GOEntry bannerAllianceEntry = GOMgr.GetEntry(GOEntryId.VisualBannerAlliance);
            GOEntry netherstormEntry = GOMgr.GetEntry(GOEntryId.NetherstormFlag);
            GOEntry netherstorm2Entry = GOMgr.GetEntry(GOEntryId.NetherstormFlag_2);
            GOEntry netherstorm3Entry = GOMgr.GetEntry(GOEntryId.NetherstormFlag_3);

            bannerNeutralEntry.FirstSpawnEntry.AutoSpawns = false;
            bannerHordeEntry.SpawnEntries.ForEach(spawn => spawn.AutoSpawns = false);
            bannerAllianceEntry.SpawnEntries.ForEach(spawn => spawn.AutoSpawns = false);
            netherstormEntry.SpawnEntries.ForEach(spawn => spawn.AutoSpawns = false);
            netherstorm2Entry.SpawnEntries.ForEach(spawn => spawn.AutoSpawns = false);
            netherstorm3Entry.SpawnEntries.ForEach(spawn => spawn.AutoSpawns = false);

            
        }
        public void RegisterEvents()
        {
            AreaTrigger bloodelftowerAT = AreaTriggerMgr.GetTrigger(AreaTriggerId.EyeOfTheStormBloodElfTower);
            AreaTrigger draeneiruinsAT = AreaTriggerMgr.GetTrigger(AreaTriggerId.EyeOfTheStormDraeneiRuins);
            AreaTrigger felreaverruinsAT = AreaTriggerMgr.GetTrigger(AreaTriggerId.EyeOfTheStormFelReaverRuins);
            AreaTrigger magetowerAT = AreaTriggerMgr.GetTrigger(AreaTriggerId.EyeOfTheStormMageTowerBottom);
        }
        #endregion
    }
    public delegate void BaseHandler(Character chr);
}
