using System;
using WCell.Addons.Default.Battlegrounds.EyeOfTheStorm.Bases;
using WCell.Addons.Default.Lang;
using WCell.Constants;
using WCell.Constants.AreaTriggers;
using WCell.Constants.Battlegrounds;
using WCell.Constants.Factions;
using WCell.Constants.GameObjects;
using WCell.Constants.Spells;
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

        public static int EOTSBuffRespawnTimeMillis = 2 * 60 * 100;
        #endregion

        #region Fields

        private GameObject _hForcefield;
        private GameObject _aForcefield;
        private GameObject _netherstormFlag;
        private GameObject _bebuffgo;
        private GameObject _frbuffgo;
        private GameObject _mtbuffgo;
        private GameObject _drbuffgo;


        private SpellId _bebuffspell;
        private SpellId _frbuffspell;
        private SpellId _mtbuffspell;
        private SpellId _drbuffspell;


        public EOTSBase[] Bases;
        public int MaxScore;
        public int NearVictoryScore;
        public bool isInformatedNearVictory;
        private AreaTrigger _felReaverAT;
        private AreaTrigger FRbuffTrigger;
        private AreaTrigger MTbuffTrigger;
        private AreaTrigger DRbuffTrigger;
        private AreaTrigger _bloodelf2;
        private AreaTrigger _bloodelf3;
        Random EOTSrnd = new Random();

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
        }

        protected override void OnStart()
        {
            base.OnStart();
            _hForcefield.State = GameObjectState.Disabled;
            _aForcefield.State = GameObjectState.Disabled;
            Characters.SendSystemMessage(DefaultAddonLocalizer.Instance.GetTranslations(AddonMsgKey.EOTSOnStart));

            SpawnBEBuff();
            SpawnFRBuff();
            SpawnMTBuff();
            SpawnDRBuff();
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

            RegisterBuffEvents();
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

        private void Graveyards(Character chr, BattlegroundTeam team)
        {
        }

        private void InformNearVictory(BattlegroundTeam team, int score)
        {
            Characters.SendSystemMessage(DefaultAddonLocalizer.Instance.GetTranslations(AddonMsgKey.EOTSNearVictory), team.Side.ToString(), score);
            isInformatedNearVictory = true;
        }

        private void HandlePowerUp(Unit unit, SpellId spell, GameObject go, Action respawnCallback)
        {
            if (go != null && !go.IsDeleted)
            {
                if (spell != 0)
                {
                    unit.SpellCast.TriggerSelf(spell);
                }
                go.Delete();
                CallDelayed(EOTSBuffRespawnTimeMillis, respawnCallback);
            }
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

            GOEntry BEspeedBuff = GOMgr.GetEntry(GOEntryId.SpeedBuff_8);
            GOEntry BErestorationBuff = GOMgr.GetEntry(GOEntryId.RestorationBuff);
            GOEntry BEberserkBuff = GOMgr.GetEntry(GOEntryId.BerserkBuff_8);

            BEspeedBuff.FirstSpawnEntry.MapId = MapId.EyeOfTheStorm;
            BEspeedBuff.FirstSpawnEntry.AutoSpawns = false;
            BEspeedBuff.FirstSpawnEntry.Scale = 1f;
            BEspeedBuff.FirstSpawnEntry.Position = new Vector3(2050.468000f, 1372.202000f, 1194.563000f);
            BEspeedBuff.FirstSpawnEntry.Orientation = 1.675514f;
            BEspeedBuff.FirstSpawnEntry.Rotations = new float[] { 0, 0, 0.7431440f, 0.6691315f };

            BErestorationBuff.FirstSpawnEntry.MapId = MapId.EyeOfTheStorm;
            BErestorationBuff.FirstSpawnEntry.AutoSpawns = false;
            BErestorationBuff.FirstSpawnEntry.Scale = 1f;
            BErestorationBuff.FirstSpawnEntry.Position = new Vector3(2050.468000f, 1372.202000f, 1194.563000f);
            BErestorationBuff.FirstSpawnEntry.Orientation = 1.675514f;
            BErestorationBuff.FirstSpawnEntry.Rotations = new float[] { 0, 0, 0.7431440f, 0.6691315f };


            BEberserkBuff.FirstSpawnEntry.MapId = MapId.EyeOfTheStorm;
            BEberserkBuff.FirstSpawnEntry.AutoSpawns = false;
            BEberserkBuff.FirstSpawnEntry.Scale = 1f;
            BEberserkBuff.FirstSpawnEntry.Position = new Vector3(2050.468000f, 1372.202000f, 1194.563000f);
            BEberserkBuff.FirstSpawnEntry.Orientation = 1.675514f;
            BEberserkBuff.FirstSpawnEntry.Rotations = new float[] { 0, 0, 0.7431440f, 0.6691315f };


            GOEntry FRspeedBuff = GOMgr.GetEntry(GOEntryId.SpeedBuff_9);
            GOEntry FRrestorationBuff = GOMgr.GetEntry(GOEntryId.RestorationBuff_2);
            GOEntry FRberserkBuff = GOMgr.GetEntry(GOEntryId.BerserkBuff_9);

            FRspeedBuff.FirstSpawnEntry.MapId = MapId.EyeOfTheStorm;
            FRspeedBuff.FirstSpawnEntry.AutoSpawns = false;
            FRspeedBuff.FirstSpawnEntry.Scale = 1f;
            FRspeedBuff.FirstSpawnEntry.Position = new Vector3(2046.463000f, 1749.167000f, 1190.010000f);
            FRspeedBuff.FirstSpawnEntry.Orientation = 5.410522f;
            FRspeedBuff.FirstSpawnEntry.Rotations = new float[] { 0, 0, -0.4226179f, 0.9063079f };

            FRrestorationBuff.FirstSpawnEntry.MapId = MapId.EyeOfTheStorm;
            FRrestorationBuff.FirstSpawnEntry.AutoSpawns = false;
            FRrestorationBuff.FirstSpawnEntry.Scale = 1f;
            FRrestorationBuff.FirstSpawnEntry.Position = new Vector3(2046.463000f, 1749.167000f, 1190.010000f);
            FRrestorationBuff.FirstSpawnEntry.Orientation = 5.410522f;
            FRrestorationBuff.FirstSpawnEntry.Rotations = new float[] { 0, 0, -0.4226179f, 0.9063079f };

            FRberserkBuff.FirstSpawnEntry.MapId = MapId.EyeOfTheStorm;
            FRberserkBuff.FirstSpawnEntry.AutoSpawns = false;
            FRberserkBuff.FirstSpawnEntry.Scale = 1f;
            FRberserkBuff.FirstSpawnEntry.Position = new Vector3(2046.463000f, 1749.167000f, 1190.010000f);
            FRberserkBuff.FirstSpawnEntry.Orientation = 5.410522f;
            FRberserkBuff.FirstSpawnEntry.Rotations = new float[] { 0, 0, -0.4226179f, 0.9063079f };

            GOEntry MTspeedBuff = GOMgr.GetEntry(GOEntryId.SpeedBuff_10);
            GOEntry MTrestorationBuff = GOMgr.GetEntry(GOEntryId.RestorationBuff_3);
            GOEntry MTberserkBuff = GOMgr.GetEntry(GOEntryId.BerserkBuff_10);

            MTspeedBuff.FirstSpawnEntry.MapId = MapId.EyeOfTheStorm;
            MTspeedBuff.FirstSpawnEntry.AutoSpawns = false;
            MTspeedBuff.FirstSpawnEntry.Scale = 1f;
            MTspeedBuff.FirstSpawnEntry.Position = new Vector3(2283.710000f, 1748.870000f, 1189.707000f);
            MTspeedBuff.FirstSpawnEntry.Orientation = -1.500983f;
            MTspeedBuff.FirstSpawnEntry.Rotations = new float[] { 0, 0, -0.6819983f, 0.7313538f };

            MTrestorationBuff.FirstSpawnEntry.MapId = MapId.EyeOfTheStorm;
            MTrestorationBuff.FirstSpawnEntry.AutoSpawns = false;
            MTrestorationBuff.FirstSpawnEntry.Scale = 1f;
            MTrestorationBuff.FirstSpawnEntry.Position = new Vector3(2283.710000f, 1748.870000f, 1189.707000f);
            MTrestorationBuff.FirstSpawnEntry.Orientation = -1.500983f;
            MTrestorationBuff.FirstSpawnEntry.Rotations = new float[] { 0, 0, -0.6819983f, 0.7313538f };

            MTberserkBuff.FirstSpawnEntry.MapId = MapId.EyeOfTheStorm;
            MTberserkBuff.FirstSpawnEntry.AutoSpawns = false;
            MTberserkBuff.FirstSpawnEntry.Scale = 1f;
            MTberserkBuff.FirstSpawnEntry.Position = new Vector3(2283.710000f, 1748.870000f, 1189.707000f);
            MTberserkBuff.FirstSpawnEntry.Orientation = -1.500983f;
            MTberserkBuff.FirstSpawnEntry.Rotations = new float[] { 0, 0, -0.6819983f, 0.7313538f };

            GOEntry DRspeedBuff = GOMgr.GetEntry(GOEntryId.SpeedBuff_11);
            GOEntry DRrestorationBuff = GOMgr.GetEntry(GOEntryId.RestorationBuff_4);
            GOEntry DRberserkBuff = GOMgr.GetEntry(GOEntryId.BerserkBuff_11);

            DRspeedBuff.FirstSpawnEntry.MapId = MapId.EyeOfTheStorm;
            DRspeedBuff.FirstSpawnEntry.AutoSpawns = false;
            DRspeedBuff.FirstSpawnEntry.Scale = 1f;
            DRspeedBuff.FirstSpawnEntry.Position = new Vector3(2302.477000f, 1391.245000f, 1197.736000f);
            DRspeedBuff.FirstSpawnEntry.Orientation = 1.762782f;
            DRspeedBuff.FirstSpawnEntry.Rotations = new float[] { 0, 0, 0.7716246f, 0.6360782f };

            DRrestorationBuff.FirstSpawnEntry.MapId = MapId.EyeOfTheStorm;
            DRrestorationBuff.FirstSpawnEntry.AutoSpawns = false;
            DRrestorationBuff.FirstSpawnEntry.Scale = 1f;
            DRrestorationBuff.FirstSpawnEntry.Position = new Vector3(2302.477000f, 1391.245000f, 1197.736000f);
            DRrestorationBuff.FirstSpawnEntry.Orientation = 1.762782f;
            DRrestorationBuff.FirstSpawnEntry.Rotations = new float[] { 0, 0, 0.7716246f, 0.6360782f };

            DRberserkBuff.FirstSpawnEntry.MapId = MapId.EyeOfTheStorm;
            DRberserkBuff.FirstSpawnEntry.AutoSpawns = false;
            DRberserkBuff.FirstSpawnEntry.Scale = 1f;
            DRberserkBuff.FirstSpawnEntry.Position = new Vector3(2302.477000f, 1391.245000f, 1197.736000f);
            DRberserkBuff.FirstSpawnEntry.Orientation = 1.762782f;
            DRberserkBuff.FirstSpawnEntry.Rotations = new float[] { 0, 0, 0.7716246f, 0.6360782f };
        }
        #endregion

        [Initialization]
        [DependentInitialization(typeof(GOMgr))]
        public void RegisterEvents()
        {

            AreaTrigger bloodelftower2 = AreaTriggerMgr.GetTrigger(AreaTriggerId.EyeOfTheStormBloodElfTowerPad);
            AreaTrigger bloodelftower3 = AreaTriggerMgr.GetTrigger(AreaTriggerId.EyeOfTheStormBloodElfTowerPad_2);

            bloodelftower2.Triggered += (at, unit) => Characters.SendSystemMessage("BE2 Triggered");
        }

        #region Buff Handlers
        public void RegisterBuffEvents()
        {
                AreaTrigger BEbuff = AreaTriggerMgr.GetTrigger(AreaTriggerId.EyeOfTheStormBloodElfTower);
                AreaTrigger FRbuff = AreaTriggerMgr.GetTrigger(AreaTriggerId.EyeOfTheStormFelReaverRuins);
                AreaTrigger MTbuff = AreaTriggerMgr.GetTrigger(AreaTriggerId.EyeOfTheStormMageTowerBottom);
                AreaTrigger DRbuff = AreaTriggerMgr.GetTrigger(AreaTriggerId.EyeOfTheStormDraeneiRuins);

                BEbuff.Triggered += (at, unit) => HandlePowerUp(unit, _bebuffspell, _bebuffgo, SpawnBEBuff);
                FRbuff.Triggered += (at, unit) => HandlePowerUp(unit, _frbuffspell, _frbuffgo, SpawnFRBuff);
                MTbuff.Triggered += (at, unit) => HandlePowerUp(unit, _mtbuffspell, _mtbuffgo, SpawnMTBuff);
                DRbuff.Triggered += (at, unit) => HandlePowerUp(unit, _drbuffspell, _drbuffgo, SpawnDRBuff);
        }

        public void SpawnBEBuff()
        {
            GOEntry[] BEBuffEntry = { GOMgr.GetEntry(GOEntryId.SpeedBuff_8), GOMgr.GetEntry(GOEntryId.RestorationBuff), GOMgr.GetEntry(GOEntryId.BerserkBuff_8) };
            SpellId[] BEBuffSpell = { SpellId.Speed_5, SpellId.Restoration_2, SpellId.Berserking_2 };
            int _bebuffchoice = EOTSrnd.Next(0, BEBuffEntry.Length);
            _bebuffspell = BEBuffSpell[_bebuffchoice];
            _bebuffgo = BEBuffEntry[_bebuffchoice].FirstSpawnEntry.Spawn(this);
        }

        public void SpawnFRBuff()
        {
            GOEntry[] FRBuffEntry = { GOMgr.GetEntry(GOEntryId.SpeedBuff_9), GOMgr.GetEntry(GOEntryId.RestorationBuff_2), GOMgr.GetEntry(GOEntryId.BerserkBuff_9) };
            SpellId[] FRBuffSpell = { SpellId.Speed_5, SpellId.Restoration_2, SpellId.Berserking_2 };
            int _frbuffchoice = EOTSrnd.Next(0, FRBuffEntry.Length);
            _frbuffspell = FRBuffSpell[_frbuffchoice];
            _frbuffgo = FRBuffEntry[_frbuffchoice].FirstSpawnEntry.Spawn(this);
        }

        public void SpawnMTBuff()
        {
            GOEntry[] MTBuffEntry = { GOMgr.GetEntry(GOEntryId.SpeedBuff_10), GOMgr.GetEntry(GOEntryId.RestorationBuff_3), GOMgr.GetEntry(GOEntryId.BerserkBuff_10) };
            SpellId[] MTBuffSpell = { SpellId.Speed_5, SpellId.Restoration_2, SpellId.Berserking_2 };
            int _mtbuffchoice = EOTSrnd.Next(0, MTBuffEntry.Length);
            _mtbuffspell = MTBuffSpell[_mtbuffchoice];
            _mtbuffgo = MTBuffEntry[_mtbuffchoice].FirstSpawnEntry.Spawn(this);
        }

        public void SpawnDRBuff()
        {
            GOEntry[] DRBuffEntry = { GOMgr.GetEntry(GOEntryId.SpeedBuff_11), GOMgr.GetEntry(GOEntryId.RestorationBuff_4), GOMgr.GetEntry(GOEntryId.BerserkBuff_11) };
            SpellId[] DRBuffSpell = { SpellId.Speed_5, SpellId.Restoration_2, SpellId.Berserking_2 };
            int _drbuffchoice = EOTSrnd.Next(0, DRBuffEntry.Length);
            _drbuffspell = DRBuffSpell[_drbuffchoice];
            _drbuffgo = DRBuffEntry[_drbuffchoice].FirstSpawnEntry.Spawn(this);
        }
        #endregion
    }
    public delegate void BaseHandler(Character chr);
}
