using WCell.Addons.Default.Lang;
using WCell.Constants;
using WCell.Constants.AreaTriggers;
using WCell.Constants.Battlegrounds;
using WCell.Constants.World;
using WCell.Core.Timers;
using WCell.RealmServer.Entities;
using WCell.RealmServer.GameObjects.Spawns;
using WCell.RealmServer.NPCs.Spawns;
using WCell.RealmServer.Handlers;
using WCell.RealmServer.Lang;
using WCell.RealmServer.NPCs;
using WCell.Util.Variables;
using WCell.Addons.Default.Battlegrounds.EyeOfTheStorm;
using WCell.RealmServer.AreaTriggers;

namespace WCell.Addons.Default.Battlegrounds.EyeOfTheStorm
{
    public abstract class EOTSBase
    {
        #region Events
        public event BaseHandler BaseChallenged;
        public event BaseHandler CaptureInterrupted;
        public event BaseHandler BaseCaptured;
        #endregion

        #region Fields
        private BattlegroundSide _side = BattlegroundSide.End;
        private BaseState _state = BaseState.Neutral;

        public GameObject Flags;

        protected GOSpawnEntry neutralBannerSpawn;
        protected GOSpawnEntry allianceBannerSpawn;
        protected GOSpawnEntry hordeBannerSpawn;

       /*public bool GivesScore;

        public TimerEntry StartScoreTimer;
        public TimerEntry CaptureTimer;*/

        protected WorldStateId baseNeutral;
        protected WorldStateId baseAllianceControlled;
        protected WorldStateId baseAllianceContested;
        protected WorldStateId baseHordeControlled;
        protected WorldStateId baseHordeContested;

        public Character Capturer;
        public EyeOfTheStorm Instance;
        #endregion

        protected EOTSBase(EyeOfTheStorm instance)
        {
            Instance = instance;
            AddSpawns();
            SpawnNeutral();
        }

        public abstract string BaseName
        {
            get;
        }

        public virtual string[] Names
        {
            get;
            protected set;
        }
        /// <summary>
        /// The side currently in control of this base.
        /// If End, base is neutral.
        /// </summary>
        public BattlegroundSide BaseOwner
        {
            get { return _side; }
            set { _side = value; }
        }
        /// <summary>
        /// The state currently of this base
        /// </summary>
        public BaseState State
        {
            get { return _state; }
            set { _state = value; }
        }
         
        protected virtual void AddSpawns()
        {
        }

        /// <summary>
        /// Spawn neutral flag (use only at the beginning)
        /// </summary>
        protected void SpawnNeutral()
        {
            Flags = neutralBannerSpawn.Spawn(Instance);
        }

        /// <summary>
        /// Spawn Horde flag (use only when CaptureTimer = 0)
        /// </summary>
        protected void SpawnHorde()
        {
            Flags = hordeBannerSpawn.Spawn(Instance);
        }

        /// <summary>
        /// Spawn Alliance flag (use only when CaptureTimer = 0)
        /// </summary>
        protected void SpawnAlliance()
        {
            Flags = allianceBannerSpawn.Spawn(Instance);
        }

        public void Destroy()
        {
            //Capturer = null;
            Instance = null;
            Flags.Delete();
            Flags.Dispose();
        }
    }
}