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
using WCell.RealmServer.Global;
using WCell.RealmServer.Handlers;
using WCell.RealmServer.Lang;
using WCell.RealmServer.NPCs;
using WCell.RealmServer.NPCs.Spawns;
using WCell.Util.Graphics;
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

        [Variable("EOTSPrepTimeMillis")]
        public static int EOTSPreparationTimeMillis = 60 * 1000;
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
        private AreaTrigger _bloodelf1;
        private AreaTrigger _bloodelf2;
        private AreaTrigger _bloodelf3;
        
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
            _bloodelf1 = AreaTriggerMgr.GetTrigger(AreaTriggerId.EyeOfTheStormBloodElfTower);
            _bloodelf2 = AreaTriggerMgr.GetTrigger(AreaTriggerId.EyeOfTheStormBloodElfTowerPad);
            _bloodelf3 = AreaTriggerMgr.GetTrigger(AreaTriggerId.EyeOfTheStormBloodElfTowerPad_2);
        }

        protected override void OnStart()
        {
            base.OnStart();
            _hForcefield.State = GameObjectState.Disabled;
            _aForcefield.State = GameObjectState.Disabled;
            Characters.SendSystemMessage(DefaultAddonLocalizer.Instance.GetTranslations(AddonMsgKey.EOTSOnStart));
        }

        protected override void OnFinish(bool disposing)
        {
            base.OnFinish(disposing);
            Characters.SendSystemMessage(DefaultAddonLocalizer.Instance.GetTranslations(AddonMsgKey.EOTSOnFinish), Winner.Side.ToString());
        }

        protected override void OnPrepareHalftime()
        {
            base.OnPrepareHalftime();
            var time = RealmLocalizer.FormatTimeSecondsMinutes(PreparationTimeMillis / 2000);
            Characters.SendSystemMessage(DefaultAddonLocalizer.Instance.GetTranslations(AddonMsgKey.EOTSOnPrepareHalfTime), time);
        }

        protected override void OnPrepareBegin()
        {
            base.OnPrepareBegin();
            var time = RealmLocalizer.FormatTimeSecondsMinutes(PreparationTimeMillis / 1000);
            Characters.SendSystemMessage(DefaultAddonLocalizer.Instance.GetTranslations(AddonMsgKey.EOTSOnPrepareBegin), time);
        }

        protected override void OnLeave(Character chr)
        {
            base.OnLeave(chr);
            Characters.SendSystemMessage(DefaultAddonLocalizer.Instance.GetTranslations(AddonMsgKey.EOTSOnLeave), chr.Name);
        }
       
        protected override void OnEnter(Character chr)
        {
            base.OnEnter(chr);
            Characters.SendSystemMessage(DefaultAddonLocalizer.Instance.GetTranslations(AddonMsgKey.EOTSOnEnter), chr.Name);
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

        protected override void SpawnNPCs()
        {
            base.SpawnNPCs();
            NPCEntry hordeSGhome = NPCMgr.GetEntry(Constants.NPCs.NPCId.HordeSpiritGuide);
            NPCEntry allianceSGhome = NPCMgr.GetEntry(Constants.NPCs.NPCId.AllianceSpiritGuide); ;
            
            hordeSGhome.SpawnAt(this, new Vector3(1807.737f, 1539.417f, 1267.625f), false);
            allianceSGhome.SpawnAt(this, new Vector3(2523.711f, 1596.52f, 1269.361f), false);
        }
        
        public override void FinishFight()
        {
            base.FinishFight();
        }

        #endregion
        private void TestTrigger(Character chr)
        {
            if (_bloodelf1.CheckTrigger(chr) == true)
            {
                Characters.SendSystemMessage("Bloodelf 1 Triggered");
            }
            if (_bloodelf2.CheckTrigger(chr) == true)
            {
                Characters.SendSystemMessage("Bloodelf 2 Triggered");
            }
            if (_bloodelf3.CheckTrigger(chr) == true)
            {
                Characters.SendSystemMessage("Bloodelf 3 Triggered");
            }
            if (_felReaverAT.CheckTrigger(chr) == true)
            {
                Characters.SendSystemMessage("Fel Reaver Ruins Triggered");
            }
        }

        private void Graveyards(Character chr, BattlegroundTeam team)
        {
        }

        private void InformNearVictory(BattlegroundTeam team, int score)
        {
            Characters.SendSystemMessage(DefaultAddonLocalizer.Instance.GetTranslations(AddonMsgKey.EOTSNearVictory), team.Side.ToString(), score);
            isInformatedNearVictory = true;
        }

        #region NPC Fixes
        [Initialization]
        [DependentInitialization(typeof(NPCMgr))]        
        public static void FixNPCs()
        {
            var hordeSpiritGuideEntry = NPCMgr.GetEntry(Constants.NPCs.NPCId.HordeSpiritGuide);
            var allianceSpiritGuideEntry = NPCMgr.GetEntry(Constants.NPCs.NPCId.AllianceSpiritGuide);
            
            hordeSpiritGuideEntry.SpawnEntries.ForEach(spawn => spawn.AutoSpawns = false);
            allianceSpiritGuideEntry.SpawnEntries.ForEach(spawn => spawn.AutoSpawns = false);
        }
        #endregion

        #region GO Fixes
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

            GOEntry foodBuff = GOMgr.GetEntry(GOEntryId.SpeedBuff);
            GOEntry speedBuff = GOMgr.GetEntry(GOEntryId.SpeedBuff);
            GOEntry berserkBuff = GOMgr.GetEntry(GOEntryId.BerserkBuff);

            foodBuff.FirstSpawnEntry.MapId = MapId.EyeOfTheStorm;
            foodBuff.FirstSpawnEntry.AutoSpawns = false;
            foodBuff.FirstSpawnEntry.Scale = 1f;

            speedBuff.FirstSpawnEntry.MapId = MapId.EyeOfTheStorm;
            speedBuff.FirstSpawnEntry.AutoSpawns = false;
            speedBuff.FirstSpawnEntry.Scale = 1f;

            berserkBuff.FirstSpawnEntry.MapId = MapId.EyeOfTheStorm;
            berserkBuff.FirstSpawnEntry.AutoSpawns = false;
            berserkBuff.FirstSpawnEntry.Scale = 1f;
        }
        #endregion
        public void RegisterEvents()
        {
            AreaTrigger bloodelftowerAT = AreaTriggerMgr.GetTrigger(AreaTriggerId.EyeOfTheStormBloodElfTower);
            AreaTrigger draeneiruinsAT = AreaTriggerMgr.GetTrigger(AreaTriggerId.EyeOfTheStormDraeneiRuins);
            AreaTrigger felreaverruinsAT = AreaTriggerMgr.GetTrigger(AreaTriggerId.EyeOfTheStormFelReaverRuins);
            AreaTrigger magetowerAT = AreaTriggerMgr.GetTrigger(AreaTriggerId.EyeOfTheStormMageTowerBottom);
            AreaTrigger bloodelftower2 = AreaTriggerMgr.GetTrigger(AreaTriggerId.EyeOfTheStormBloodElfTowerPad);
            AreaTrigger bloodelftower3 = AreaTriggerMgr.GetTrigger(AreaTriggerId.EyeOfTheStormBloodElfTowerPad_2);
        }
    }
    public delegate void BaseHandler(Character chr);
}
