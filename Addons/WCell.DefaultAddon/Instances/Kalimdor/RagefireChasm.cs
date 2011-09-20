using WCell.Constants.NPCs;
using WCell.Constants.Spells;
using WCell.Core.Initialization;
using WCell.RealmServer.AI;
using WCell.RealmServer.AI.Actions.Combat;
using WCell.RealmServer.AI.Actions.States;
using WCell.RealmServer.AI.Brains;
using WCell.RealmServer.Entities;
using WCell.RealmServer.Instances;
using WCell.RealmServer.Misc;
using WCell.RealmServer.NPCs;
using WCell.RealmServer.Spells;


namespace WCell.Addons.Default.Instances
{
	public class RagefireChasm : BaseInstance
	{
		#region Setup Content
        //Trash Mobs
        private static NPCEntry ragefireshamanEntry;
        private static NPCEntry ragefiretroggEntry;
        private static NPCEntry searingbladewarlockEntry;
        private static NPCEntry searingbladeenforcerEntry;
        private static NPCEntry searingbladecultistEntry;
        private static NPCEntry moltenelementalEntry;
        private static NPCEntry earthborerEntry;
        //Bosses
		private static NPCEntry oggleflintEntry;
		private static NPCEntry taragamanEntry;
		private static NPCEntry jergoshEntry;
		private static NPCEntry bazzalanEntry;
//      Oggleflint
        static readonly ProcHandlerTemplate cleave = new TriggerSpellProcHandlerTemplate(SpellHandler.Get(SpellId.Cleave_28), ProcTriggerFlags.ReceivedAnyDamage, ProcHitFlags.None, 10);
//      Taragaman
//      static readonly ProcHandlerTemplate uppercut = new TriggerSpellProcHandlerTemplate(SpellHandler.Get(SpellId.Uppercut_2), ProcTriggerFlags.AnyHit, 10);  //Not working properly
        static readonly ProcHandlerTemplate firenova = new TriggerSpellProcHandlerTemplate(SpellHandler.Get(SpellId.FireNova_2), ProcTriggerFlags.ReceivedAnyDamage, ProcHitFlags.None, 10);
//      Jergosh
        static readonly ProcHandlerTemplate weakness = new TriggerSpellProcHandlerTemplate(SpellHandler.Get(SpellId.CurseOfWeakness_6), ProcTriggerFlags.ReceivedAnyDamage, ProcHitFlags.None, 10);
        static readonly ProcHandlerTemplate immolate = new TriggerSpellProcHandlerTemplate(SpellHandler.Get(SpellId.Immolate_13), ProcTriggerFlags.ReceivedAnyDamage, ProcHitFlags.None, 15);
//      Bazzalan
        static readonly ProcHandlerTemplate poison = new TriggerSpellProcHandlerTemplate(SpellHandler.Get(SpellId.Poison_10), ProcTriggerFlags.ReceivedAnyDamage, ProcHitFlags.None, 5);
        static readonly ProcHandlerTemplate sstrike = new TriggerSpellProcHandlerTemplate(SpellHandler.Get(SpellId.SinisterStrike), ProcTriggerFlags.ReceivedAnyDamage, ProcHitFlags.None, 10);
        
		[Initialization]
		[DependentInitialization(typeof(NPCMgr))]
		public static void InitNPCs()
        {
            #region Trash Mobs
            ragefireshamanEntry = NPCMgr.GetEntry(NPCId.RagefireShaman);
            ragefireshamanEntry.Activated += RagefireShaman =>
            {
                ((BaseBrain)RagefireShaman.Brain).DefaultCombatAction.Strategy = new RFShamanAttackAction(RagefireShaman);
            };
            #endregion
            #region Bosses
            //          Oggleflint
			oggleflintEntry = NPCMgr.GetEntry(NPCId.Oggleflint);
			oggleflintEntry.AddSpell(SpellId.Cleave);            
			oggleflintEntry.Activated += oggleflint =>
			{
				var brain = (BaseBrain)oggleflint.Brain;
				var combatAction = (AICombatAction)brain.Actions[BrainState.Combat];
				combatAction.Strategy = new OggleflintAttackAction(oggleflint);
                oggleflint.AddProcHandler(cleave);
			};

//          Taragaman the Hungerer
			taragamanEntry = NPCMgr.GetEntry(NPCId.TaragamanTheHungerer);
//          taragamanEntry.AddSpell(SpellId.Uppercut);  //Not working properly
			taragamanEntry.AddSpell(SpellId.FireNova);
			taragamanEntry.Activated += taragaman =>
			{
				var brain = (BaseBrain)taragaman.Brain;
				var combatAction = (AICombatAction)brain.Actions[BrainState.Combat];
				combatAction.Strategy = new TaragamanAttackAction(taragaman);
//              taragaman.AddProcHandler(uppercut);  //Currently not working
                taragaman.AddProcHandler(firenova); 
            };

//          Jergosh the Invoker
			jergoshEntry = NPCMgr.GetEntry(NPCId.JergoshTheInvoker);
			jergoshEntry.AddSpell(SpellId.CurseOfWeakness);
			jergoshEntry.AddSpell(SpellId.Immolate);
			jergoshEntry.Activated += jergosh =>
			{
				var brain = (BaseBrain)jergosh.Brain;
				var combatAction = (AICombatAction)brain.Actions[BrainState.Combat];
				combatAction.Strategy = new JergoshAttackAction(jergosh);
                jergosh.AddProcHandler(weakness);
                jergosh.AddProcHandler(immolate);
			};

//          Bazzalan
			bazzalanEntry = NPCMgr.GetEntry(NPCId.Bazzalan);
			bazzalanEntry.AddSpell(SpellId.Poison);
			bazzalanEntry.AddSpell(SpellId.SinisterStrike);
			bazzalanEntry.Activated += bazzalan =>
			{
				var brain = (BaseBrain)bazzalan.Brain;
				var combatAction = (AICombatAction)brain.Actions[BrainState.Combat];
				combatAction.Strategy = new BazzalanAttackAction(bazzalan);
                bazzalan.AddProcHandler(poison);
                bazzalan.AddProcHandler(sstrike);
			};
            #endregion
        }
		#endregion
	}
    #region Trash Mob Brain

    #region Ragefire Shaman
    public class RFShamanAttackAction : AIAttackAction
    {
        [Initialization(InitializationPass.Second)]
        public static void InitShamanSpells()
        {
            var healingwave = SpellHandler.Get(SpellId.HealingWave);
            healingwave.AISettings.SetCooldownRange(15000);
            healingwave.MaxTargets = 1;
            healingwave.OverrideAITargetDefinitions(
                DefaultTargetAdders.AddAreaSource, 									    // Adder
                DefaultTargetEvaluators.AnyWoundedEvaluator, 							// Evaluator
                DefaultTargetFilters.IsFriendly, DefaultTargetFilters.IsNotPlayer);		// Filters

            var lightningbolt = SpellHandler.Get(SpellId.LightningBolt);
            lightningbolt.AISettings.SetCooldownRange(8000);
            lightningbolt.MaxTargets = 1;
            lightningbolt.OverrideAITargetDefinitions(
                DefaultTargetAdders.AddAreaSource, 									    // Adder
                DefaultTargetEvaluators.RandomEvaluator, 							    // Evaluator
                DefaultTargetFilters.IsHostile, DefaultTargetFilters.IsPlayer);		    // Filters
        }

        public RFShamanAttackAction(NPC RagefireShaman)
            : base(RagefireShaman)
        {
            RagefireShaman.Spells.AddSpell(SpellId.HealingWave, SpellId.LightningBolt);
        }
    }
    #endregion
    
    #endregion

    #region Oggleflint
    public class OggleflintAttackAction : AIAttackAction
	{
		public OggleflintAttackAction(NPC oggleflint)
			: base(oggleflint)
		{
		}
	}
	#endregion

	#region Taragaman
	public class TaragamanAttackAction : AIAttackAction
	{
		public TaragamanAttackAction(NPC taragaman)
			: base(taragaman)
		{
  		}
	}
	#endregion

	#region Jergosh
	public class JergoshAttackAction : AIAttackAction
	{
		public JergoshAttackAction(NPC jergosh)
			: base(jergosh)
		{
		}
	}
	#endregion

	#region Bazzalan
	public class BazzalanAttackAction : AIAttackAction
	{
		public BazzalanAttackAction(NPC bazzalan)
			: base(bazzalan)
		{
		}
	}
	#endregion
}