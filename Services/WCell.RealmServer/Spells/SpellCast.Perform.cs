using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using WCell.Constants;
using WCell.Constants.Achievements;
using WCell.Constants.Misc;
using WCell.Constants.Spells;
using WCell.RealmServer.Entities;
using WCell.RealmServer.Handlers;
using WCell.RealmServer.Misc;
using WCell.RealmServer.Spells.Auras;
using WCell.RealmServer.Spells.Targeting;

namespace WCell.RealmServer.Spells
{
	public partial class SpellCast
	{
		/// <summary>
		/// Returns whether the given target is immune to the given spell
		/// </summary>
		public static bool IsImmune(Unit target, Spell spell, bool hostile)
		{
			if (
						hostile &&
						spell.Mechanic.IsNegative() &&
						!spell.Attributes.HasFlag(SpellAttributes.UnaffectedByInvulnerability) &&
						(spell.Mechanic == SpellMechanic.Invulnerable_2 || spell.Mechanic == SpellMechanic.Invulnerable) &&
						(
							// immune against spell
							target.IsInvulnerable ||
							target.IsImmune(SpellMechanic.Invulnerable_2) ||
							target.IsImmune(SpellMechanic.Invulnerable) ||
							target.IsImmune(spell.Mechanic) ||
							target.IsImmune(spell.DispelType)
						)
				)
			{
				return true;
			}
			return false;
		}

		#region InitHandlers
		/// <summary>
		/// Creates the SpellEffectHandlers and collects all initial targets
		/// </summary>
		protected SpellFailedReason InitHandlers()
		{
			var failReason = SpellFailedReason.Ok;

			if (Targets == null)
			{
				//m_targets = WorldObject.WorldObjectSetPool.Obtain();
				Targets = new HashSet<WorldObject>();
			}

			//var extraEffects = CasterUnit.Spells.GetExtraEffectsForSpell(m_spell.SpellId);
			//var hasExtraEffects = extraEffects != null;
			var handlers = new SpellEffectHandler[Spell.EffectHandlerCount];// + (hasExtraEffects ? extraEffects.Count : 0)];
			var h = 0;
			SpellTargetCollection targets = null;
			foreach (var effect in Spell.Effects)
			{
				if (effect.SpellEffectHandlerCreator == null)
				{
					continue;
				}

				CreateHandler(effect, h, handlers, ref targets, ref failReason);
				if (failReason != SpellFailedReason.Ok)
				{
					return failReason;
				}
				h++;
			}

			//if (hasExtraEffects)
			//{
			//    foreach (var effect in extraEffects)
			//    {
			//        if (effect.SpellEffectHandlerCreator == null)
			//        {
			//            continue;
			//        }

			//        InitHandler(effect, h, handlers, out targets, ref failReason);
			//        if (failReason != SpellFailedReason.Ok)
			//        {
			//            return failReason;
			//        }
			//        h++;
			//    }
			//}

			if (failReason == SpellFailedReason.Ok)
			{
				Handlers = handlers;

				// initialize handlers
				foreach (var handler in Handlers)
				{
					handler.Initialize(ref failReason);
					if (failReason != SpellFailedReason.Ok)
					{
						Handlers = null;
						return failReason;
					}
				}

				// initialize targets
				foreach (var handler in Handlers)
				{
					var handlerTargets = handler.m_targets;
					if (handlerTargets == null || handlerTargets.IsInitialized) continue;

					if (InitialTargets != null)
					{
						// initialize forced targets
						if ((failReason = handlerTargets.AddAll(InitialTargets)) != SpellFailedReason.Ok)
						{
							return failReason;
						}
					}
					else
					{
						// Initialize standard Targets
						if ((failReason = handlerTargets.FindAllTargets()) != SpellFailedReason.Ok)
						{
							return failReason;
						}
					}

					foreach (var target in handlerTargets)
					{
						Targets.Add(target);
					}
				}
			}
			return failReason;
		}

		SpellTargetCollection CreateSpellTargetCollection()
		{
			if (IsAICast)
			{
				return AISpellTargetCollection.ObtainAICollection();
			}
			return SpellTargetCollection.Obtain();
		}

		private void CreateHandler(SpellEffect effect, int h, SpellEffectHandler[] handlers, ref SpellTargetCollection targets, ref SpellFailedReason failReason)
		{
			var handler = effect.SpellEffectHandlerCreator(this, effect);
			handlers[h] = handler;

			// make sure, we have the right Caster-Type
			handler.CheckCasterType(ref failReason);
			if (failReason != SpellFailedReason.Ok)
			{
				return;
			}

			// find targets and amount SpellTargetCollection if effects have same ImplicitTargetTypes
			if (InitialTargets != null)
			{
				// do we have given targets?
				//targets = SpellTargetCollection.SpellTargetCollectionPool.Obtain();
				if (targets == null)
				{
					targets = CreateSpellTargetCollection();
				}
			}
			else if (handler.HasOwnTargets)
			{
				// see if targets are shared between effects
				targets = null;

				for (var j = 0; j < h; j++)
				{
					var handler2 = handlers[j];
					if (handler.Effect.SharesTargetsWith(handler2.Effect, IsAICast))
					{
						// same targets -> share target collection
						targets = handler2.m_targets;
						break;
					}
				}

				if (targets == null)
				{
					//targets = SpellTargetCollection.SpellTargetCollectionPool.Obtain();
					targets = CreateSpellTargetCollection();
				}
			}

			if (targets != null)
			{
				handler.m_targets = targets;
				targets.m_handlers.Add(handler);
			}
		}

		#endregion

		#region Perform
		private void Perform(int elapsed)
		{
			CheckCasterValidity();
			Perform();
		}

		/// <summary>
		/// Does some sanity checks and adjustments right before perform
		/// </summary>
		protected SpellFailedReason PrePerform()
		{
			if (IsPlayerCast)
			{
				// Make sure that there is an Item for Spells that require an Item target
				if (Spell.TargetFlags.HasAnyFlag(SpellTargetFlags.Item))
				{
					// Spell targets an item
					if (TargetItem == null || !TargetItem.IsInWorld || TargetItem.Owner != CasterObject)
					{
						if (IsPassive)
						{
							LogManager.GetCurrentClassLogger().Warn("Trying to trigger Spell without Item selected: " + this);
						}
						return SpellFailedReason.ItemNotFound;
					}

					if (TargetItem.IsEquipped && !TargetItem.Unequip())
					{
						if (IsPassive)
						{
							LogManager.GetCurrentClassLogger().Warn("Trying to trigger Spell without Item ready: " + this);
						}

						// make sure, Item is not equipped					
						return SpellFailedReason.ItemNotReady;
					}
				}

				// check immunities
				if (!IsAoE && Selected is Unit && !Spell.IsPreventionDebuff)
				{
					var hostile = Spell.IsHarmfulFor(CasterReference, Selected);
					if (IsImmune((Unit)Selected, Spell, hostile))
					{
						Cancel(SpellFailedReason.Immune);
						return SpellFailedReason.Immune;
					}
				}

				// toggle autoshot
				if (Spell.IsAutoRepeating)
				{
					if (CasterUnit.Target == null)
					{
						CasterUnit.Target = Selected as Unit;
						if (CasterUnit.Target == null)
						{
							return SpellFailedReason.BadTargets;
						}
					}

					CasterUnit.IsFighting = true;
					if (CasterUnit.AutorepeatSpell == Spell)
					{
						// deactivate
						CasterUnit.AutorepeatSpell = null;
					}
					else
					{
						// activate
						CasterUnit.AutorepeatSpell = Spell;
						SendCastStart();
						CasterUnit.IsFighting = true;
					}
					return SpellFailedReason.DontReport;
				}

				else if (Spell.Attributes.HasFlag(SpellAttributes.StopsAutoAttack))
				{
					// cancel autoshot
					CasterUnit.AutorepeatSpell = null;
					CasterUnit.IsFighting = false;
				}
			}

			if (CasterUnit == null && Spell.IsChanneled)
			{
				// Channel requires CasterUnit
				return SpellFailedReason.CasterAurastate;
			}

			// check aura stacking and prepare auras
			if (Spell.IsAura)
			{
				if (Targets.Count == 0 && !IsAoE && !Spell.IsAreaAura)
				{
					return SpellFailedReason.NoValidTargets;
				}
				var failReason = PrepAuras();
				if (failReason != SpellFailedReason.Ok)
				{
					Cancel(failReason);
					return failReason;
				}
			}

			if (!GodMode && CasterUnit is Character)
			{
				// check whether skill succeeded
				if (
					Spell.Ability != null &&
					Spell.Ability.GreenValue > 0 &&
					!Spell.Ability.CheckSuccess(((Character)CasterUnit).Skills.GetValue(Spell.Ability.Skill.Id)))
				{
					return SpellFailedReason.TryAgain;
				}

				// consume reagents (last check -> must not fail anymore after this one)
				if (!ConsumeReagents())
				{
					return SpellFailedReason.Reagents;
				}
			}

			if (CasterUnit != null)
			{
				// we might have to kneel
				if (Selected is ILockable && ((ILockable)Selected).Lock != null)
				{
					if (((ILockable)Selected).Lock.RequiresKneeling)
					{
						CasterUnit.StandState = StandState.Kneeling;
					}
				}

				// cancel stealth
				if (!Spell.AttributesEx.HasFlag(SpellAttributesEx.RemainStealthed))
				{
					CasterUnit.Auras.RemoveWhere(aura => aura.Spell.DispelType == DispelType.Stealth);
				}
			}

			// revalidate targets for AI, amongst others
			if (IsAICast && !PrePerformAI())
			{
				return SpellFailedReason.NoValidTargets;
			}

			return SpellFailedReason.Ok;
		}

		/// <summary>
		/// Performs the actual Spell
		/// </summary>
		internal SpellFailedReason Perform()
		{
			try
			{
				var spell = Spell;

				SpellFailedReason failReason;
				if (Handlers == null)
				{
					// initialze Spell handlers
					failReason = InitHandlers();
					if (failReason != SpellFailedReason.Ok)
					{
						Cancel(failReason);
						return failReason;
					}
				}

				//var sw2 = Stopwatch.StartNew();
				failReason = PrePerform();
				if (failReason != SpellFailedReason.Ok)
				{
					Cancel(failReason);
					return failReason;
				}
				//if (CasterChar != null)
				//{
				//    CasterChar.SendSystemMessage("SpellCast (PrePerform): {0} ms", sw2.ElapsedTicks / 10000d);
				//}


				List<MissedTarget> missedTargets = CheckHit();
				RemoveFromHandlerTargets(missedTargets);

				SendSpellGo(missedTargets);

				// check whether impact is delayed
				int delay = CalculateImpactDelay();
				var delayedImpactIsNoticable = delay > Map.UpdateDelay / 1000f;

				if (delayedImpactIsNoticable)
				{
					DoDelayedImpact(delay);
					failReason = SpellFailedReason.Ok;
				}
				else
				{
					// instant impact
					failReason = Impact();
				}

				if (IsCasting && CasterUnit != null)
				{
					OnCasted();
				}

				if (IsCasting && !delayedImpactIsNoticable && !IsChanneling)
				{
					Cleanup(true);
				}
				return failReason;
			}
			catch (Exception e)
			{
				OnException(e);
				return SpellFailedReason.Error;
			}
		}

		private int CalculateImpactDelay()
		{
			if (Spell.ProjectileSpeed <= 0 || Targets.Count > 0)
			{
				return 0;
			}

			float distance;
			if (TriggerAction != null)
			{
				distance = TriggerAction.Attacker.GetDist(TriggerAction.Victim);
			}
			else if (CasterObject != null)
			{
				var target = Targets.First();
				//var distance = target.GetDistance(Caster) + 10;
				distance = target.GetDistance(CasterObject);
			}
			else
			{
				return 0;
			}

			return (int)((distance * 1000) / Spell.ProjectileSpeed);
		}

		private void DoDelayedImpact(int delay)
		{
			if (CasterObject != null)
			{
				CasterObject.CallDelayed(delay, DelayedImpact);
				if (!Spell.IsChanneled && this == CasterObject.SpellCast)
				{
					// reset SpellCast so it cannot be cancelled anymore
					CasterObject.SpellCast = null;
				}
			}
			else
			{
				Map.CallDelayed(delay, () => DelayedImpact(null));
			}
		}

		private void DelayedImpact(WorldObject obj)
		{
			CheckCasterValidity();
			foreach (var target in Targets.Where(target => !target.IsInWorld))
			{
				Remove(target);
			}
			try
			{
				Impact();

				// clean it up
				if (!Spell.IsChanneled && IsCasting)
				{
					Cleanup(true);
				}

				var caster = CasterObject;
				if (caster != null && caster.SpellCast == null && !IsPassive)
				{
					// recycle spell cast
					// TODO: Improve spellcast recycling
					caster.SpellCast = this;
				}
			}
			catch (Exception e)
			{
				OnException(e);
			}
		}

		#endregion

		#region Impact
		/// <summary>
		/// Validates targets and applies all SpellEffects
		/// </summary>
		public SpellFailedReason Impact()
		{
			if (!IsCasting)
			{
				return SpellFailedReason.Ok;
			}

			// apply effects
			foreach (var handler in Handlers)
			{
				if (handler.Effect.IsPeriodic || handler.Effect.IsStrikeEffect)
				{
					// weapon ability or handled by Aura or Channel
					continue;
				}

				handler.Apply();
				if (!IsCasting)
				{
					// the last handler cancelled the SpellCast
					return SpellFailedReason.DontReport;
				}
			}

			if (CasterObject is Unit && Spell.IsPhysicalAbility)
			{
				// strike at everyone
				foreach (var target in UnitTargets)
				{
					ProcHitFlags hitFlags = CasterUnit.Strike(GetWeapon(), target, this);
					m_hitInfoByTarget[target] = hitFlags;
				}
			}

			// open Channel and spawn DynamicObject
			DynamicObject dynObj = null;
			if (Spell.DOEffect != null)
			{
				dynObj = new DynamicObject(this, Spell.DOEffect.GetRadius(CasterReference));
			}

			if (!IsCasting)
			{
				return SpellFailedReason.Ok;
			}

			List<MissedTarget> missedTargets = null;
			// create auras
			List<IAura> auras = null;
			if (m_auraApplicationInfos != null)
			{
				CreateAuras(ref missedTargets, ref auras, dynObj);
			}

			// check for missed targets
			if (missedTargets != null)
			{
				if (missedTargets.Count > 0)
				{
					// TODO: Flash message ontop of missed heads when impact is delayed
					CombatLogHandler.SendSpellMiss(this, true, missedTargets);
					missedTargets.Clear();
				}

				CastMissListPool.Recycle(missedTargets);
			}

			// open channel
			if (Spell.IsChanneled && CasterObject != null)
			{
				Channel = SpellChannel.SpellChannelPool.Obtain();
				Channel.m_cast = this;

				if (CasterObject is Unit)
				{
					if (dynObj != null)
					{
						CasterUnit.ChannelObject = dynObj;
					}
					else if (Selected != null)
					{
						CasterUnit.ChannelObject = Selected;
						if (Selected is NPC && Spell.IsTame)
						{
							((NPC)Selected).CurrentTamer = CasterObject as Character;
						}
					}
				}

				var len = Handlers.Length;
				var channelEffectHandlers = SpellEffectHandlerListPool.Obtain();
				//var channelEffectHandlers = new List<SpellEffectHandler>(6);
				for (var i = 0; i < len; i++)
				{
					var handler = Handlers[i];
					if (handler.Effect.IsPeriodic)
					{
						channelEffectHandlers.Add(handler);
					}
				}
				Channel.Open(channelEffectHandlers, auras);
			}

			// start Auras
			if (auras != null)
			{
				for (var i = 0; i < auras.Count; i++)
				{
					var aura = auras[i];
					aura.Start(Spell.IsChanneled ? Channel : null, false);
				}

				if (!IsChanneling)
				{
					auras.Clear();
					AuraListPool.Recycle(auras);
					auras = null;
				}
			}

			// applying debuffs might cancel other Auras
			if (Spell.HasHarmfulEffects && !Spell.IsPreventionDebuff)
			{
				foreach (var target in Targets)
				{
					if (target is Unit && Spell.IsHarmfulFor(CasterReference, target))
					{
						((Unit)target).Auras.RemoveByFlag(AuraInterruptFlags.OnHostileSpellInflicted);
					}
				}
			}

			//if (CasterChar != null)
			//{
			//    CasterChar.SendSystemMessage("SpellCast (Impact): {0} ms", sw1.ElapsedTicks / 10000d);
			//}

			return SpellFailedReason.Ok;
		}
		#endregion

		#region OnCasted
		/// <summary>
		/// Called after a Unit caster casted a spell
		/// </summary>
		protected void OnCasted()
		{
			//var sw1 = Stopwatch.StartNew();

			var caster = CasterUnit;

			if (caster.IsAlive)
			{
				// sit while eating/drinking
				if (Spell.IsFood || Spell.IsDrink)
				{
					caster.StandState = StandState.Sit;
					// food emote, emote of drinking is handled in Aura.OnApply (each amplitude time)
					if (Spell.IsFood)
					{
						caster.Emote(EmoteType.SimpleEat);
					}
				}

				if (caster is Character)
				{
					// gain skill
					var chr = (Character)caster;
					if (Spell.Ability != null && Spell.Ability.CanGainSkill)
					{
						var skill = chr.Skills[Spell.Ability.Skill.Id];
						var skillVal = skill.CurrentValue;
						var max = (ushort)skill.ActualMax;
						if (skillVal < max)
						{
							skillVal += (ushort)Spell.Ability.Gain(skillVal);
							skill.CurrentValue = skillVal <= max ? skillVal : max;
						}
					}

					// Using a combat ability
					if (Spell.IsPhysicalAbility && Spell.IsRangedAbility)
					{
						if (Spell.IsThrow)
						{
							// Each throw reduces Durability by one
							var item = chr.RangedWeapon as Item;
							if (item != null)
							{
								item.Durability--;
							}
						}
						else
						{
							// Firing ranged weapons (still) consumes Ammo
							chr.Inventory.ConsumeAmmo();
						}
					}

					// Check for quest progress
					chr.QuestLog.OnSpellCast(this);																// can potentially execute arbitrary code
				}

				// casting a spell on a combatant also puts the Caster in combat mode
				if (!caster.IsInCombat)
				{
					foreach (var target in Targets)
					{
						if (target is Unit && ((Unit)target).IsInCombat)
						{
							caster.IsInCombat = true;
							break;
						}
					}
				}

				// casting resets the swing delay
				if (Spell.HasHarmfulEffects && !Spell.IsPreventionDebuff)
				{
					if (caster.IsInCombat)
					{
						caster.ResetSwingDelay();
					}
				}
			}

			// Used an item
			if (TargetItem != null)
			{
				CasterChar.Achievements.CheckPossibleAchievementUpdates(AchievementCriteriaType.UseItem, Spell.Id);
				TargetItem.OnUse();																				// can execute arbitrary code
			}

			// update AuraState
			if (Spell.RequiredCasterAuraState == AuraState.DodgeOrBlockOrParry)
			{
				caster.AuraState &= ~AuraStateMask.DodgeOrBlockOrParry;
			}

			var hasRunes = UsesRunes;
			if (!GodMode)
			{
				// add cooldown (if not autoshot & not triggered by another spell)
				if (!Spell.IsAutoRepeating && TriggerEffect == null)
				{
					caster.Spells.AddCooldown(Spell, CasterItem);
				}
				if (Client != null)
				{
					if (!Spell.Attributes.HasFlag(SpellAttributes.StartCooldownAfterEffectFade) &&
						CasterItem != null)
					{
						SpellHandler.SendItemCooldown(Client, Spell.Id, CasterItem);
					}
				}

				// consume runes
				if (hasRunes)
				{
					((Character)caster).PlayerSpells.Runes.ConsumeRunes(Spell);
				}

				// consume power (might cancel the cast due to dying)
				var powerCost = Spell.CalcPowerCost(caster,
													  Selected is Unit
														? ((Unit)Selected).GetLeastResistantSchool(Spell)
														: Spell.Schools[0]);
				if (Spell.PowerType != PowerType.Health)
				{
					caster.Power -= powerCost;
				}
				else
				{
					caster.Health -= powerCost;
					if (!IsCasting)
					{
						return; // we dead!
					}
				}
			}
			else if (!IsPassive && caster is Character)
			{
				// clear cooldowns
				var spells = ((Character)caster).PlayerSpells;
				if (spells != null)
				{
					spells.ClearCooldown(Spell);
				}
			}

			// add runic power
			if (hasRunes)
			{
				caster.Power += Spell.RuneCostEntry.RunicPowerGain;
			}

			// trigger spells after casting spells (used for Forbearance etc)
			if (Spell.TargetTriggerSpells != null)
			{
				for (var i = 0; i < Spell.TargetTriggerSpells.Length; i++)
				{
					var trigSpell = Spell.TargetTriggerSpells[i];
					Trigger(trigSpell, Targets.ToArray());													// can execute arbitrary code
					if (!IsCasting)
					{
						return; // should not happen (but might)
					}
				}
			}
			if (Spell.CasterTriggerSpells != null)
			{
				for (var i = 0; i < Spell.CasterTriggerSpells.Length; i++)
				{
					var trigSpell = Spell.CasterTriggerSpells[i];
					Trigger(trigSpell, Targets.ToArray());													// can execute arbitrary code
					if (!IsCasting)
					{
						return; // should not happen (but might)
					}
				}
			}

			// trigger dynamic post-cast spells, eg Shadow Weaving etc
			caster.Spells.TriggerSpellsFor(this);																// can execute arbitrary code
			if (!IsCasting)
			{
				return; // should not happen (but might)
			}

			// consume combopoints
			if (Spell.IsFinishingMove)
			{
				caster.ModComboState(null, 0);
			}

			// consumes spell modifiers (if required)
			caster.Auras.OnCasted(this);
			if (!IsCasting)
			{
				return; // should not happen (but might)
			}

			if (IsAICast)
			{
				OnAICasted();																					// can execute arbitrary code
				if (!IsCasting)
				{
					return; // should not happen (but might)
				}
			}

			// Casted event
			Spell.NotifyCasted(this);
			if (caster is Character)
			{
				CasterChar.Achievements.CheckPossibleAchievementUpdates(AchievementCriteriaType.CastSpell, Spell.Id);
			}

			TriggerProcOnCasted();

			m_hitInfoByTarget.Clear();

			//if (CasterChar != null)
			//{
			//    CasterChar.SendSystemMessage("SpellCast (Casted): {0} ms", sw1.ElapsedTicks / 10000d);
			//}
		}

		#region Procs
		void TriggerProcOnCasted()
		{
			// Set the flags for caster and target based on the spell
			ProcTriggerFlags casterProcFlags = ProcTriggerFlags.None;
			ProcTriggerFlags targetProcFlags = ProcTriggerFlags.None;

			switch (Spell.DamageType)
			{
				case DamageType.None:
				{
					if (Spell.IsBeneficial)
					{
						casterProcFlags |= ProcTriggerFlags.DoneBeneficialSpell;
						targetProcFlags |= ProcTriggerFlags.ReceivedBeneficialSpell;
					}
					else if (Spell.IsHarmful)
					{
						casterProcFlags |= ProcTriggerFlags.DoneHarmfulSpell;
						targetProcFlags |= ProcTriggerFlags.ReceivedHarmfulSpell;
					}
					break;
				}

				case DamageType.Magic:
				{
					if (Spell.IsBeneficial)
					{
						casterProcFlags |= ProcTriggerFlags.DoneBeneficialMagicSpell;
						targetProcFlags |= ProcTriggerFlags.ReceivedBeneficialMagicSpell;
					}
					else if (Spell.IsHarmful)
					{
						casterProcFlags |= ProcTriggerFlags.DoneHarmfulMagicSpell;
						targetProcFlags |= ProcTriggerFlags.ReceivedHarmfulMagicSpell;
					}
					break;
				}

				case DamageType.Melee:
				{
					casterProcFlags |= ProcTriggerFlags.DoneMeleeSpell;
					targetProcFlags |= ProcTriggerFlags.ReceivedMeleeSpell;
					break;
				}

				case DamageType.Ranged:
				{
					if (Spell.IsAutoRepeating)
					{
						casterProcFlags |= ProcTriggerFlags.DoneRangedAutoAttack;
						targetProcFlags |= ProcTriggerFlags.ReceivedRangedAutoAttack;
					}
					else
					{
						casterProcFlags |= ProcTriggerFlags.DoneRangedSpell;
						targetProcFlags |= ProcTriggerFlags.ReceivedRangedSpell;
					}
					break;
				}
			}

			ProcHitFlags casterHitFlags = TriggerProcOnTargets(targetProcFlags);

			TriggerProcOnCaster(casterProcFlags, casterHitFlags);
		}

		/// <summary>
		/// Triggers proc on all targets of SpellCast
		/// </summary>
		/// <param name="flags">What happened to targets ie. ProcTriggerFlags.ReceivedHarmfulSpell</param>
		/// <returns>Combination of hit result on all targets.</returns>
		private ProcHitFlags TriggerProcOnTargets(ProcTriggerFlags flags)
		{
			ProcHitFlags hitFlagsCombination = ProcHitFlags.None;

			foreach (var hitInfo in m_hitInfoByTarget)
			{
				Unit target = hitInfo.Key;
				ProcHitFlags targetHitFlags = hitInfo.Value;

				hitFlagsCombination |= targetHitFlags;

				var action = new SimpleUnitAction
				{
					Attacker = CasterUnit,
					Spell = Spell,
					Victim = target,
					IsCritical = targetHitFlags.HasAnyFlag(ProcHitFlags.CriticalHit)
				};

				target.Proc(flags, CasterUnit, action, true, targetHitFlags);
			}

			return hitFlagsCombination;
		}

		/// <summary>
		/// Trigger proc on the caster of the spell.
		/// </summary>
		/// <param name="flags">What spell caster casted ie. ProcTriggerFlags.DoneHarmfulSpell</param>
		/// <param name="hitFlags">Hit result of the spell</param>
		private void TriggerProcOnCaster(ProcTriggerFlags flags, ProcHitFlags hitFlags)
		{
			var casterAction = new SimpleUnitAction
			{
				Attacker = CasterUnit,
				Spell = Spell,
				Victim = m_hitInfoByTarget.Count > 0 ? m_hitInfoByTarget.First().Key : null,
				IsCritical = hitFlags.HasAnyFlag(ProcHitFlags.CriticalHit)
			};

			var triggerer = UnitTargets.FirstOrDefault();

			CasterUnit.Proc(flags, triggerer, casterAction, true, hitFlags);
		}
		#endregion
		#endregion
	}
}