using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using RimWorld;
using UnityEngine;


namespace Dreams
{
    public class Building_DreamCatcher : Building
    {
        private List<DreamPart> storedDreamParts = new List<DreamPart>();
        private const int MaxStoredDreams = 4;

        public List<DreamPart> StoredDreamParts => storedDreamParts;

        public bool CanStoreDreams => storedDreamParts.Count < MaxStoredDreams;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (storedDreamParts == null)
            {
                storedDreamParts = new List<DreamPart>();
            }
        }

        public bool TryAddDreamPart(DreamPart dreamPart)
        {
            if (!CanStoreDreams) return false;

            storedDreamParts.Add(dreamPart);
            return true;
        }

        public override string GetInspectString()
        {
            string baseString = base.GetInspectString();
            string dreamString = $"\nStored dreams: {storedDreamParts.Count}/{MaxStoredDreams}";

            if (!string.IsNullOrEmpty(baseString))
            {
                return baseString + dreamString;
            }
            return dreamString.TrimStart();
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo g in base.GetGizmos())
            {
                yield return g;
            }

            if (storedDreamParts.Count > 0)
            {
                yield return new Command_Action
                {
                    defaultLabel = "Collect Dreams",
                    defaultDesc = "Collect stored dream parts from the dream catcher.",
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/CollectDreams", true),
                    action = () =>
                    {
                        Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("CollectDreamPart"), this);
                        Map.mapPawns.FreeColonists.RandomElement().jobs.TryTakeOrderedJob(job);
                    }
                };
            }
        }

        public override void Tick()
        {
            base.Tick();

            // Every 2 seconds (120 ticks)
            if (Find.TickManager.TicksGame % 120 == 0 && storedDreamParts.Count > 0)
            {
                // Visual effect for stored dreams
                Vector3 pos = DrawPos;
                pos.y += 0.5f;
                FleckMaker.ThrowLightningGlow(pos, Map, Rand.Range(0.3f, 0.4f));
            }
        }

        public void NotifyDreamGenerated(Pawn pawn, DreamPart dreamPart)
        {
            if (CanStoreDreams)
            {
                if (TryAddDreamPart(dreamPart))
                {
                    // Visual effect for dream capture
                    Vector3 startPos = pawn.DrawPos;
                    startPos.y += 0.5f;
                    Vector3 endPos = DrawPos;
                    endPos.y += 0.5f;

                    FleckMaker.ThrowLightningGlow(startPos, Map, 0.5f);
                    FleckMaker.ThrowLightningGlow(endPos, Map, 0.5f);

                    Messages.Message($"The dream catcher captured {pawn.LabelShort}'s dream.",
                        new LookTargets(new TargetInfo[] { new TargetInfo(this), new TargetInfo(pawn) }),
                        MessageTypeDefOf.NeutralEvent);
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref storedDreamParts, "storedDreamParts", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                if (storedDreamParts == null)
                {
                    storedDreamParts = new List<DreamPart>();
                }
            }
        }
    }
}
