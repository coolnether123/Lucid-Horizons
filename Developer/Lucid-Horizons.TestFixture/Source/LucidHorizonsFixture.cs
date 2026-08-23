using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dreams;
using LudeonTK;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace LucidHorizons.TestFixture
{
    public static class LucidHorizonsFixtureActions
    {
        private const string Prefix = "[Lucid Horizons Fixture]";

        [DebugAction(
            "Lucid Horizons Fixture",
            "Run full gameplay lifecycle",
            actionType = DebugActionType.Action)]
        public static void RunFullGameplayLifecycle()
        {
            Map map = Find.CurrentMap;
            if (map == null)
            {
                Log.Error(Prefix + " lifecycle=failed reason=no-map");
                return;
            }

            Pawn pawn = map.mapPawns.FreeColonists.FirstOrDefault();
            if (pawn == null)
            {
                Log.Error(Prefix + " lifecycle=failed reason=no-free-colonist");
                return;
            }

            FinishResearch();
            Log.Message(
                Prefix + " setup=map pawns=" + map.mapPawns.FreeColonists.Count);

            Building dreamCatcher = SpawnBuilding("DreamCatcher", map, pawn.Position);
            Building dreamLab = SpawnBuilding("DreamLab", map, pawn.Position);
            Building dreamPod = SpawnBuilding("DreamPod", map, pawn.Position);

            ReportBuildingType("dream-catcher", dreamCatcher, "Dreams.Building_DreamCatcher");
            ReportBuildingType("dream-lab", dreamLab, "Dreams.Building_DreamLab");
            ReportBuildingType("dream-pod", dreamPod, "Dreams.Building_DreamPod");

            Building_DreamCatcher catcher = dreamCatcher as Building_DreamCatcher;
            Building_DreamLab lab = dreamLab as Building_DreamLab;
            Building_DreamPod pod = dreamPod as Building_DreamPod;

            if (lab != null)
            {
                CompPowerTrader power = lab.GetComp<CompPowerTrader>();
                if (power != null)
                {
                    power.PowerOn = true;
                    Log.Message(Prefix + " lab-power=on");
                }
            }

            if (pod != null)
            {
                CompRefuelable fuel = pod.GetComp<CompRefuelable>();
                if (fuel != null)
                {
                    fuel.Refuel(75f);
                    Log.Message(
                        Prefix + " pod-fuel=" + fuel.Fuel + " hasFuel=" + fuel.HasFuel);
                }
            }

            DreamPart generatedDream = GenerateDream(pawn);
            if (catcher != null && generatedDream != null)
            {
                catcher.NotifyDreamGenerated(pawn, generatedDream);
                Log.Message(
                    Prefix + " capture stored=" + catcher.StoredDreamParts.Count +
                    " dream=" + generatedDream.ThingID);

                Job collectJob = JobMaker.MakeJob(
                    DefDatabase<JobDef>.GetNamed("CollectDreamPart"),
                    catcher);
                Pawn collectPawn = map.mapPawns.FreeColonists.Skip(1).FirstOrDefault() ?? pawn;
                bool collectQueued = collectPawn.jobs.TryTakeOrderedJob(collectJob);
                Log.Message(
                    Prefix + " collect-job queued=" + collectQueued +
                    " def=" + collectJob.def.defName +
                    " pawn=" + collectPawn.LabelShort);
            }
            else
            {
                Log.Error(
                    Prefix + " capture=failed catcherType=" +
                    (dreamCatcher == null ? "null" : dreamCatcher.GetType().FullName));
            }

            if (lab != null)
            {
                QueueCraftingJob(lab, pawn, map);
            }

            if (pod != null)
            {
                ExercisePod(pod);
            }

            LogCurrentState(map, "lifecycle-end");
        }

        [DebugAction(
            "Lucid Horizons Fixture",
            "Report gameplay state",
            actionType = DebugActionType.Action)]
        public static void ReportGameplayState()
        {
            Map map = Find.CurrentMap;
            if (map == null)
            {
                Log.Error(Prefix + " report=failed reason=no-map");
                return;
            }

            LogCurrentState(map, "report");
        }

        [DebugAction(
            "Lucid Horizons Fixture",
            "Open translation UI probe",
            actionType = DebugActionType.Action)]
        public static void OpenTranslationUiProbe()
        {
            Find.WindowStack.Add(new TranslationProbeWindow());
            Log.Message(Prefix + " translation-ui=open");
        }

        private static void FinishResearch()
        {
            foreach (string defName in new[]
            {
                "DreamTechnology",
                "AdvancedDreamCrafting",
                "DreamEngineering"
            })
            {
                ResearchProjectDef project =
                    DefDatabase<ResearchProjectDef>.GetNamed(defName, false);
                if (project != null && !project.IsFinished)
                {
                    Find.ResearchManager.FinishProject(
                        project,
                        doCompletionDialog: false,
                        researcher: null,
                        doCompletionLetter: false);
                    Log.Message(Prefix + " research-finished=" + defName);
                }
            }
        }

        private static Building SpawnBuilding(string defName, Map map, IntVec3 origin)
        {
            ThingDef def = DefDatabase<ThingDef>.GetNamed(defName, false);
            if (def == null)
            {
                Log.Error(Prefix + " spawn=failed def=" + defName + " reason=missing-def");
                return null;
            }

            IntVec3 placementOrigin = origin;
            if (defName == "DreamLab")
            {
                placementOrigin += new IntVec3(8, 0, 0);
            }
            else if (defName == "DreamPod")
            {
                placementOrigin += new IntVec3(-8, 0, 0);
            }

            IntVec3 cell;
            if (!TryFindFreeCell(map, placementOrigin, out cell))
            {
                Log.Error(Prefix + " spawn=failed def=" + defName + " reason=no-cell");
                return null;
            }

            Thing thing = ThingMaker.MakeThing(
                def,
                def.MadeFromStuff ? GenStuff.DefaultStuffFor(def) : null);
            thing.SetFaction(Faction.OfPlayer);
            return GenSpawn.Spawn(thing, cell, map) as Building;
        }

        private static bool TryFindFreeCell(Map map, IntVec3 origin, out IntVec3 cell)
        {
            foreach (IntVec3 candidate in GenRadial.RadialCellsAround(origin, 12f, true))
            {
                if (candidate.InBounds(map) && candidate.Standable(map) &&
                    candidate.GetEdifice(map) == null)
                {
                    cell = candidate;
                    return true;
                }
            }

            cell = IntVec3.Invalid;
            return false;
        }

        private static DreamPart GenerateDream(Pawn pawn)
        {
            DreamManager manager = Current.Game.GetComponent<DreamManager>();
            if (manager != null)
            {
                MethodInfo method = typeof(DreamManager).GetMethod(
                    "TryGenerateDream",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                if (method != null)
                {
                    try
                    {
                        method.Invoke(manager, new object[] { pawn });
                        Log.Message(Prefix + " dream-manager invoked=true");
                    }
                    catch (Exception exception)
                    {
                        Log.Error(Prefix + " dream-manager exception=" + exception);
                    }
                }
            }

            DreamPart dream = DreamUtility.GenerateRandomDream(pawn, 100f, 1f);
            if (dream == null)
            {
                Log.Error(Prefix + " dream-generated=false");
                return null;
            }

            Log.Message(
                Prefix + " dream-generated=true type=" + dream.type +
                " vividness=" + dream.vividness + " subject=" + dream.subject.Label);
            return dream;
        }

        private static void QueueCraftingJob(Building_DreamLab lab, Pawn pawn, Map map)
        {
            RecipeDef recipe = DefDatabase<RecipeDef>.GetNamed("CombineDreamParts", false);
            if (recipe == null)
            {
                Log.Error(Prefix + " craft=failed reason=missing-recipe");
                return;
            }

            Bill_Production bill = new Bill_Production(recipe);
            lab.BillStack.AddBill(bill);
            Log.Message(Prefix + " craft-bill added=" + recipe.defName);

            for (int i = 0; i < 3; i++)
            {
                DreamPart part = MakeDreamPart(pawn);
                IntVec3 cell;
                if (part != null && TryFindFreeCell(map, lab.Position, out cell))
                {
                    GenSpawn.Spawn(part, cell, map);
                }
            }

            Job job = JobMaker.MakeJob(JobDefOf.DoBill, lab);
            job.bill = bill;
            bool queued = pawn.jobs.TryTakeOrderedJob(job);
            Log.Message(Prefix + " craft-job queued=" + queued);
        }

        private static DreamPart MakeDreamPart(Pawn pawn)
        {
            DreamPart part = ThingMaker.MakeThing(
                DefDatabase<ThingDef>.GetNamed("DreamPart")) as DreamPart;
            if (part == null)
            {
                return null;
            }

            part.type = DreamType.Epic;
            part.vividness = DreamVividness.Vivid;
            part.subject = new DreamSubject
            {
                type = DreamSubject.SubjectType.Pawn,
                reference = pawn.ThingID
            };
            return part;
        }

        private static void ExercisePod(Building_DreamPod pod)
        {
            CraftedDream dream = ThingMaker.MakeThing(
                DefDatabase<ThingDef>.GetNamed("CraftedDream")) as CraftedDream;
            Faction target = Find.FactionManager.AllFactions.FirstOrDefault(
                faction => !faction.IsPlayer && !faction.Hidden &&
                    !faction.HostileTo(Faction.OfPlayer));
            if (dream == null || target == null)
            {
                Log.Error(
                    Prefix + " pod=failed dreamType=" +
                    (dream == null ? "null" : dream.GetType().FullName) +
                    " target=" + (target == null ? "null" : target.Name));
                return;
            }

            dream.type = DreamType.Inspirational;
            dream.vividness = DreamVividness.Normal;
            dream.subject = new DreamSubject
            {
                type = DreamSubject.SubjectType.Abstract,
                reference = "fixture"
            };
            pod.TryAcceptThing(dream);
            Log.Message(
                Prefix + " pod-loaded=" + pod.LoadingInProgress +
                " held=" + pod.GetDirectlyHeldThings().Count);
            pod.LaunchDreamToFaction(target);
            Log.Message(
                Prefix + " pod-launched held=" + pod.GetDirectlyHeldThings().Count +
                " loading=" + pod.LoadingInProgress);
        }

        private static void ReportBuildingType(
            string label,
            Building building,
            string expectedType)
        {
            string actualType = building == null ? "null" : building.GetType().FullName;
            Log.Message(
                Prefix + " type=" + label + " actual=" + actualType +
                " expected=" + expectedType +
                " pass=" + string.Equals(actualType, expectedType, StringComparison.Ordinal));
        }

        private static void LogCurrentState(Map map, string phase)
        {
            List<Thing> things = map.listerThings.AllThings;
            string counts = string.Join(
                ",",
                new[] { "DreamCatcher", "DreamLab", "DreamPod", "DreamPart", "CraftedDream" }
                    .Select(defName => defName + "=" +
                        things.Count(thing => thing.def.defName == defName)));
            int customCatchers = things.Count(thing => thing is Building_DreamCatcher);
            int storedDreams = things.OfType<Building_DreamCatcher>()
                .Sum(catcher => catcher.StoredDreamParts.Count);
            Pawn pawn = map.mapPawns.FreeColonists.FirstOrDefault();
            Log.Message(
                Prefix + " state phase=" + phase + " tick=" +
                Find.TickManager.TicksGame + " counts=" + counts +
                " customCatchers=" + customCatchers +
                " storedDreams=" + storedDreams +
                " pawnJob=" + (pawn?.CurJobDef?.defName ?? "none"));
        }
    }

    public sealed class TranslationProbeWindow : Window
    {
        public override Vector2 InitialSize => new Vector2(600f, 260f);

        public TranslationProbeWindow()
        {
            doCloseButton = true;
            doCloseX = true;
            absorbInputAroundWindow = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(
                new Rect(inRect.x, inRect.y, inRect.width, 34f),
                "SelectTargetFaction".Translate());
            Text.Font = GameFont.Small;
            Widgets.Label(
                new Rect(inRect.x, inRect.y + 42f, inRect.width, 30f),
                "DreamCaptured".Translate("Fixture Pawn"));
            Widgets.Label(
                new Rect(inRect.x, inRect.y + 78f, inRect.width, 30f),
                "DreamLabNoPower".Translate());
            Widgets.Label(
                new Rect(inRect.x, inRect.y + 114f, inRect.width, 30f),
                "DreamPodNotLoaded".Translate());
        }
    }
}
