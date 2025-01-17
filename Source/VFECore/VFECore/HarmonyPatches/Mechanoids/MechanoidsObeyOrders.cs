﻿using AnimalBehaviours;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using VFE.Mechanoids.Needs;
using VFEMech;

namespace VFE.Mechanoids.HarmonyPatches
{
	[StaticConstructorOnStartup]
	public static class MechanoidDraftCompInitializer
	{
		static MechanoidDraftCompInitializer()
		{
			foreach (var pawn in DefDatabase<PawnKindDef>.AllDefs)
			{
				var compPropsMachine = pawn.race.GetCompProperties<CompProperties_Machine>();
				if (compPropsMachine != null && compPropsMachine.violent)
				{
					if (pawn.race.GetCompProperties<CompProperties_Draftable>() is null)
					{
						pawn.race.comps.Add(new CompProperties_Draftable());
					}
				}
			}
		}
	}

	[HarmonyPatch(typeof(FloatMenuMakerMap), "CanTakeOrder")]
    public static class MechanoidsObeyOrders
    {
        public static void Postfix(Pawn pawn, ref bool __result)
        {
            if (pawn.drafter != null && pawn.RaceProps.IsMechanoid && pawn.Faction != null && pawn.Faction.IsPlayer)
                __result = true;
        }
    }

	// an attempt to make draftable mechanoids be selected with other colonists, didn't find a time to make a proper patch
	//[HarmonyPatch]
	//public static class MakeMechanoidsMultiSelectable
    //{
	//	[HarmonyTargetMethod]
	//	public static MethodBase GetMethod()
    //    {
	//		return typeof(Selector).GetNestedTypes(AccessTools.all).Select(type => type.GetMethods(AccessTools.all).FirstOrDefault(method => method.Name.Contains("<SelectAllMatchingObjectUnderMouseOnScreen>")
	//		&& method.ReturnType == typeof(bool) && method.GetParameters().FirstOrDefault()?.ParameterType == typeof(Thing))).FirstOrDefault();
    //    }
	//
	//	public static void Postfix(Thing t)
    //    {
	//		Log.Message("T: " + t);
    //    }
    //}

    [HarmonyPatch(typeof(FloatMenuMakerMap), "AddDraftedOrders")]
    public static class AddDraftedOrders_Patch
    {
        public static bool Prefix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
        {
            if (pawn.RaceProps.IsMechanoid && pawn.needs.TryGetNeed<Need_Power>() is Need_Power need && need.CurLevel <= 0f)
            {
                return false;
            }
            return true;
        }
    }

	[StaticConstructorOnStartup]
	public static class SimpleSidearmsPatch
    {
		public static bool SimpleSidearmsActive;
		static SimpleSidearmsPatch()
        {
			SimpleSidearmsActive = ModsConfig.IsActive("PeteTimesSix.SimpleSidearms");
			if (SimpleSidearmsActive)
            {
				var type = AccessTools.TypeByName("PeteTimesSix.SimpleSidearms.Extensions");
				if (type != null)
				{
					var target = AccessTools.Method(type, "IsValidSidearmsCarrier");
					VFECore.VFECore.harmonyInstance.Patch(target, postfix: new HarmonyMethod(AccessTools.Method(typeof(SimpleSidearmsPatch), nameof(IsValidSidearmsCarrierPostfix))));
					type = AccessTools.TypeByName("SimpleSidearms.rimworld.CompSidearmMemory");
					target = AccessTools.Method(type, "GetMemoryCompForPawn");
					VFECore.VFECore.harmonyInstance.Patch(target, prefix: new HarmonyMethod(AccessTools.Method(typeof(SimpleSidearmsPatch), nameof(GetMemoryCompForPawnPrefix))));
					type = AccessTools.TypeByName("SimpleSidearms.rimworld.Gizmo_SidearmsList");
					target = AccessTools.Method(type, "DrawGizmoLabel");
					VFECore.VFECore.harmonyInstance.Patch(target, prefix: new HarmonyMethod(AccessTools.Method(typeof(SimpleSidearmsPatch), nameof(GizmoLabelFixer))));
				}
				else
                {
					Log.Error("[Vanilla Expanded Framework] Patching Simple Sidearms failed.");
                }
			}
        }
		public static void IsValidSidearmsCarrierPostfix(ref bool __result, Pawn pawn)
        {
			if (!__result)
            {
				var compMachine = pawn.GetComp<CompMachine>();
				if (compMachine != null && compMachine.Props.canPickupWeapons)
                {
					__result = true;
                }
			}
        }

		public static void GizmoLabelFixer(ref string labelText, Rect gizmoRect)
        {
			labelText = labelText.Replace(" (godmode)", "");

		}
		public static IEnumerable<Gizmo> SimpleSidearmsGizmos(Pawn __instance)
		{
			if (PeteTimesSix.SimpleSidearms.Extensions.IsValidSidearmsCarrier(__instance) && __instance.equipment != null && __instance.inventory != null)
			{
				var gizmo = GetSimpleSidearmsGizmo(__instance);
				if (gizmo != null)
                {
					yield return gizmo;
                }
			}
		}

		public static Gizmo GetSimpleSidearmsGizmo(Pawn __instance)
        {
			IEnumerable<ThingWithComps> carriedWeapons = PeteTimesSix.SimpleSidearms.Extensions.getCarriedWeapons(__instance, includeEquipped: true, includeTools: true);
			SimpleSidearms.rimworld.CompSidearmMemory pawnMemory = SimpleSidearms.rimworld.CompSidearmMemory.GetMemoryCompForPawn(__instance);
			if (pawnMemory != null)
			{
				List<SimpleSidearms.rimworld.ThingDefStuffDefPair> rangedWeaponMemories = new List<SimpleSidearms.rimworld.ThingDefStuffDefPair>();
				List<SimpleSidearms.rimworld.ThingDefStuffDefPair> meleeWeaponMemories = new List<SimpleSidearms.rimworld.ThingDefStuffDefPair>();
				foreach (SimpleSidearms.rimworld.ThingDefStuffDefPair weapon in pawnMemory.RememberedWeapons)
				{
					if (weapon.thing.IsMeleeWeapon)
					{
						meleeWeaponMemories.Add(weapon);
					}
					else if (weapon.thing.IsRangedWeapon)
					{
						rangedWeaponMemories.Add(weapon);
					}
				}
				return new SimpleSidearms.rimworld.Gizmo_SidearmsList(__instance, carriedWeapons, pawnMemory.RememberedWeapons);
			}
			return null;
		}
		public static bool GetMemoryCompForPawnPrefix(ref object __result, Pawn pawn, bool fillExistingIfCreating = true)
		{
			var compMachine = pawn.GetComp<CompMachine>();
			if (compMachine != null && compMachine.Props.canPickupWeapons)
			{
				__result = pawn.TryGetComp<SimpleSidearms.rimworld.CompSidearmMemory>();
				return false;
			}
			return true;
		}
	}

    [HarmonyPatch(typeof(FloatMenuMakerMap), "ChoicesAtFor")]
    public static class FloatMenuMakerMap_ChoicesAtFor_Patch
    {
        public static void Postfix(ref List<FloatMenuOption> __result, Vector3 clickPos, Pawn pawn, bool suppressAutoTakeableGoto = false)
        {
            if (!pawn.RaceProps.Humanlike)
            {
                var compMachine = pawn.GetComp<CompMachine>();
                if (compMachine != null && compMachine.Props.canPickupWeapons)
				{
					IntVec3 c = IntVec3.FromVector3(clickPos);
					ThingWithComps equipment = null;
					List<Thing> thingList2 = c.GetThingList(pawn.Map);
					for (int i = 0; i < thingList2.Count; i++)
					{
						if (thingList2[i].TryGetComp<CompEquippable>() != null)
						{
							equipment = (ThingWithComps)thingList2[i];
							break;
						}
					}
					if (equipment != null)
					{
						string labelShort = equipment.LabelShort;
						FloatMenuOption item6;
						string cantReason;
						if (equipment.def.IsWeapon && pawn.WorkTagIsDisabled(WorkTags.Violent))
						{
							item6 = new FloatMenuOption("CannotEquip".Translate(labelShort) + ": " + "IsIncapableOfViolenceLower".Translate(pawn.LabelShort, pawn), null);
						}
						else if (equipment.def.IsRangedWeapon && pawn.WorkTagIsDisabled(WorkTags.Shooting))
						{
							item6 = new FloatMenuOption("CannotEquip".Translate(labelShort) + ": " + "IsIncapableOfShootingLower".Translate(pawn), null);
						}
						else if (!pawn.CanReach(equipment, PathEndMode.ClosestTouch, Danger.Deadly))
						{
							item6 = new FloatMenuOption("CannotEquip".Translate(labelShort) + ": " + "NoPath".Translate().CapitalizeFirst(), null);
						}
						else if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
						{
							item6 = new FloatMenuOption("CannotEquip".Translate(labelShort) + ": " + "Incapable".Translate(), null);
						}
						else if (equipment.IsBurning())
						{
							item6 = new FloatMenuOption("CannotEquip".Translate(labelShort) + ": " + "BurningLower".Translate(), null);
						}
						else if (pawn.IsQuestLodger() && !EquipmentUtility.QuestLodgerCanEquip(equipment, pawn))
						{
							item6 = new FloatMenuOption("CannotEquip".Translate(labelShort) + ": " + "QuestRelated".Translate().CapitalizeFirst(), null);
						}
						else if (!EquipmentUtility.CanEquip(equipment, pawn, out cantReason, checkBonded: false))
						{
							item6 = new FloatMenuOption("CannotEquip".Translate(labelShort) + ": " + cantReason.CapitalizeFirst(), null);
						}
						else
						{
							string text4 = "Equip".Translate(labelShort);
							if (equipment.def.IsRangedWeapon && pawn.story != null && pawn.story.traits.HasTrait(TraitDefOf.Brawler))
							{
								text4 += " " + "EquipWarningBrawler".Translate();
							}
							if (EquipmentUtility.AlreadyBondedToWeapon(equipment, pawn))
							{
								text4 += " " + "BladelinkAlreadyBonded".Translate();
								TaggedString dialogText = "BladelinkAlreadyBondedDialog".Translate(pawn.Named("PAWN"), equipment.Named("WEAPON"), pawn.equipment.bondedWeapon.Named("BONDEDWEAPON"));
								item6 = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text4, delegate
								{
									Find.WindowStack.Add(new Dialog_MessageBox(dialogText));
								}, MenuOptionPriority.High), pawn, equipment);
							}
							else
							{
								item6 = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text4, delegate
								{
									string personaWeaponConfirmationText = EquipmentUtility.GetPersonaWeaponConfirmationText(equipment, pawn);
									if (!personaWeaponConfirmationText.NullOrEmpty())
									{
										Find.WindowStack.Add(new Dialog_MessageBox(personaWeaponConfirmationText, "Yes".Translate(), delegate
										{
											Equip();
										}, "No".Translate()));
									}
									else
									{
										Equip();
									}
								}, MenuOptionPriority.High), pawn, equipment);
							}
						}
						__result.Add(item6);
						if (SimpleSidearmsPatch.SimpleSidearmsActive)
						{
							AppendSidearmsOptions(pawn, equipment, ref __result);
						}
					}

					void Equip()
					{
						equipment.SetForbidden(value: false);
						pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.Equip, equipment), JobTag.Misc);
						FleckMaker.Static(equipment.DrawPos, equipment.MapHeld, FleckDefOf.FeedbackEquip);
						PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.EquippingWeapons, KnowledgeAmount.Total);
					}
				}
            }
        }

		public static void AppendSidearmsOptions(Pawn pawn, ThingWithComps equipment, ref List<FloatMenuOption> __result)
        {
			try
			{
				string labelShort = equipment.LabelShort;
				if (!pawn.CanReach(new LocalTargetInfo(equipment), PathEndMode.ClosestTouch, Danger.Deadly) || !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) || !equipment.def.IsWeapon || equipment.IsBurning() || pawn.IsQuestLodger())
				{
					return;
				}
				FloatMenuOption item3;
				if (!PeteTimesSix.SimpleSidearms.Utilities.StatCalculator.canCarrySidearmInstance(equipment, pawn, out var errStr))
				{
					"CannotEquip".Translate();
					item3 = new FloatMenuOption("CannotEquip".Translate(labelShort) + " (" + errStr + ")", null);
					__result.Add(item3);
					return;
				}
				string text2 = "Equip".Translate(labelShort);
				text2 = (((pawn.CombinedDisabledWorkTags & WorkTags.Violent) == 0 && !PeteTimesSix.SimpleSidearms.Extensions.isToolNotWeapon
					(PeteTimesSix.SimpleSidearms.Extensions.toThingDefStuffDefPair(equipment))) ? ((string)(text2 + "AsSidearm".Translate())) : ((string)(text2 + "AsTool".Translate())));
				if (equipment.def.IsRangedWeapon && pawn.story != null && pawn.story.traits.HasTrait(TraitDefOf.Brawler))
				{
					text2 = text2 + " " + "EquipWarningBrawler".Translate();
				}
				item3 = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text2, delegate
				{
					equipment.SetForbidden(value: false);
					pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(SimpleSidearms.rimworld.SidearmsDefOf.EquipSecondary, equipment), JobTag.Misc);
					PlayerKnowledgeDatabase.KnowledgeDemonstrated(SimpleSidearms.rimworld.SidearmsDefOf.Concept_SimpleSidearmsBasic, KnowledgeAmount.SmallInteraction);
				}, MenuOptionPriority.High), pawn, equipment);
				__result.Add(item3);
			}
			catch (Exception e)
			{
				Log.Error("Exception during SimpleSidearms floatmenumaker intercept. Cancelling intercept. Exception: " + e.ToString());
			}
		}
    }

    [HarmonyPatch(typeof(WanderUtility), "GetColonyWanderRoot")]
    public static class GetColonyWanderRoot_Patch
    {
        public static void Postfix(ref IntVec3 __result, Pawn pawn)
        {
            try
            {
                if (pawn.Map != null && pawn.RaceProps.IsMechanoid && pawn.Faction == Faction.OfPlayer && __result.IsForbidden(pawn) && pawn.playerSettings?.EffectiveAreaRestrictionInPawnCurrentMap.ActiveCells.Count() > 0)
                {
                    __result = pawn.playerSettings.EffectiveAreaRestrictionInPawnCurrentMap.ActiveCells.OrderBy(x => x.DistanceTo(pawn.Position))
                        .Where(x => x.Walkable(pawn.Map) && pawn.CanReserveAndReach(x, PathEndMode.OnCell, Danger.Deadly)).Take(10).RandomElement();
                }
            }
            catch { }
        }
    }
}
