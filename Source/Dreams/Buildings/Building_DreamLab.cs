using System.Collections.Generic;
using Verse;
using RimWorld;
using UnityEngine;
using System.Linq;


namespace Dreams
{
    public class Building_DreamLab : Building_WorkTable
    {
        private CompPowerTrader powerComp;
        private const float PowerOnGlowRadius = 3f;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            powerComp = GetComp<CompPowerTrader>();
        }

        public override void Tick()
        {
            base.Tick();

            // Visual effects when powered and working
            if (powerComp.PowerOn && IsWorkTableInUse() && Find.TickManager.TicksGame % 100 == 0)
            {
                Vector3 position = DrawPos;
                position.y += 0.5f;
                
                FleckMaker.ThrowLightningGlow(position, Map, Rand.Range(0.3f, 0.5f));
                
                // Add sparkles around the workbench
                for (int i = 0; i < 3; i++)
                {
                    Vector3 randomPos = position + new Vector3(
                        Rand.Range(-0.5f, 0.5f),
                        0f,
                        Rand.Range(-0.5f, 0.5f));
                    
                    FleckMaker.ThrowMicroSparks(randomPos, Map);
                }
            }
        }

        private bool IsWorkTableInUse()
        {
            return this.BillStack?.AnyShouldDoNow ?? false;
        }

        public override string GetInspectString()
        {
            string text = base.GetInspectString();
            
            if (!powerComp.PowerOn)
            {
                if (!text.NullOrEmpty())
                {
                    text += "\n";
                }
                text += "NoPower".Translate();
            }

            return text;
        }

        public override void DrawExtraSelectionOverlays()
        {
            base.DrawExtraSelectionOverlays();
            
            // Show interaction cell
            GenDraw.DrawFieldEdges(new List<IntVec3> { InteractionCell }, Color.white);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo g in base.GetGizmos())
            {
                yield return g;
            }

            if (Faction == Faction.OfPlayer)
            {
                // Add any custom gizmos specific to the Dream Lab
                if (powerComp.PowerOn)
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "Analyze Dreams",
                        defaultDesc = "Study stored dream parts.",
                        icon = ContentFinder<Texture2D>.Get("UI/Commands/AnalyzeDreams", true),
                        action = () =>
                        {
                            // TODO: Implement dream analysis functionality
                            Messages.Message("Dream analysis is not available yet.",
                                MessageTypeDefOf.RejectInput);
                        }
                    };
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            // Add any additional data that needs to be saved
        }

        public bool AcceptsThing(Thing thing)
        {
            return thing is DreamPart && this.BillStack != null && this.BillStack.Count > 0;
        }

        protected override void ReceiveCompSignal(string signal)
        {
            base.ReceiveCompSignal(signal);

            // Handle power state changes
            if (signal == "PowerTurnedOn" || signal == "PowerTurnedOff")
            {
                // Update visuals or functionality based on power state
                Map?.mapDrawer.MapMeshDirty(Position, MapMeshFlagDefOf.Things);
            }
        }
    }
}
