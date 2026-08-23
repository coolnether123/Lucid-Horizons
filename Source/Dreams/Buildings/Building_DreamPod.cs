using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;

namespace Dreams
{
    public class Building_DreamPod : Building, IThingHolder
    {
        private CompRefuelable fuelComp;
        private bool loadingInProgress;
        private ThingOwner<Thing> innerContainer;

        public bool LoadingInProgress => loadingInProgress;
        public bool CanLaunchNow => fuelComp.HasFuel && LoadingInProgress;

        public Building_DreamPod()
        {
            innerContainer = new ThingOwner<Thing>(this);
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            fuelComp = GetComp<CompRefuelable>();
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            innerContainer.TryDropAll(Position, Map, ThingPlaceMode.Near);
            base.DeSpawn(mode);
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            innerContainer.TryDropAll(Position, Map, ThingPlaceMode.Near);
            base.Destroy(mode);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo g in base.GetGizmos())
            {
                yield return g;
            }

            if (Faction == Faction.OfPlayer)
            {
                // Add custom launch gizmo for dreams
                if (CanLaunchNow)
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "Launch Dream",
                        defaultDesc = "Launch a dream to affect another faction.",
                        icon = ContentFinder<Texture2D>.Get("UI/Commands/LaunchDream", true),
                        action = () =>
                        {
                            Find.WindowStack.Add(new Dialog_SelectFactionTarget(this));
                        }
                    };
                }
            }
        }

        public bool AcceptsDreamItem(Thing thing)
        {
            return thing is DreamPart || thing is CraftedDream;
        }

        public void TryAcceptThing(Thing thing)
        {
            if (!AcceptsDreamItem(thing)) return;

            if (innerContainer.TryAdd(thing))
            {
                loadingInProgress = true;
            }
        }

        public void LaunchDreamToFaction(Faction faction)
        {
            if (!CanLaunchNow)
            {
                Messages.Message("The pod is not ready or needs fuel.",
                    MessageTypeDefOf.RejectInput);
                return;
            }

            // Get the dream item from the pod
            Thing dreamItem = null;
            foreach (Thing t in innerContainer)
            {
                if (t is DreamPart || t is CraftedDream)
                {
                    dreamItem = t;
                    break;
                }
            }

            if (dreamItem == null)
            {
                Messages.Message("Load a dream before launch.",
                    MessageTypeDefOf.RejectInput);
                return;
            }

            // Calculate faction relation impact
            float relationshipImpact = CalculateRelationshipImpact(dreamItem);

            // Apply the relationship change
            faction.TryAffectGoodwillWith(Faction.OfPlayer, Mathf.RoundToInt(relationshipImpact));

            // Visual effects
            Vector3 position = DrawPos;
            position.y += 0.5f;
            FleckMaker.ThrowLightningGlow(position, Map, 1f);
            FleckMaker.ThrowSmoke(position, Map, 2f);

            // Message about the effect
            string effectType = relationshipImpact > 0 ? "improved" : "worsened";
            Messages.Message($"Dream sent to {faction.Name}. Relations have {effectType}.",
                MessageTypeDefOf.NeutralEvent);

            // Consume fuel and destroy the dream item
            fuelComp.ConsumeFuel(fuelComp.Props.fuelConsumptionRate);
            dreamItem.Destroy();

            // Reset the pod
            innerContainer.Clear();
            loadingInProgress = false;
        }

        private float CalculateRelationshipImpact(Thing dreamItem)
        {
            if (dreamItem is DreamPart dreamPart)
            {
                // Base impact from dream type
                float baseImpact;
                switch (dreamPart.type)
                {
                    case DreamType.NightTerror:
                        baseImpact = -15f;
                        break;
                    case DreamType.Nightmare:
                        baseImpact = -10f;
                        break;
                    case DreamType.Mid:
                        baseImpact = 5f;
                        break;
                    case DreamType.Epic:
                        baseImpact = 10f;
                        break;
                    case DreamType.Inspirational:
                        baseImpact = 15f;
                        break;
                    default:
                        baseImpact = 0f;
                        break;
                }

                // Modify based on vividness
                float vividnessMultiplier;
                switch (dreamPart.vividness)
                {
                    case DreamVividness.Faint:
                        vividnessMultiplier = 0.5f;
                        break;
                    case DreamVividness.Dull:
                        vividnessMultiplier = 0.75f;
                        break;
                    case DreamVividness.Normal:
                        vividnessMultiplier = 1f;
                        break;
                    case DreamVividness.Vivid:
                        vividnessMultiplier = 1.25f;
                        break;
                    case DreamVividness.Lucid:
                        vividnessMultiplier = 1.5f;
                        break;
                    default:
                        vividnessMultiplier = 1f;
                        break;
                }

                return baseImpact * vividnessMultiplier;
            }

            // Default impact for other dream items
            return 5f;
        }

        public override string GetInspectString()
        {
            string text = base.GetInspectString();

            if (!text.NullOrEmpty())
            {
                text += "\n";
            }

            // Add dream-specific info
            Thing dreamItem = innerContainer.FirstOrDefault(t => t is DreamPart || t is CraftedDream);
            if (dreamItem != null)
            {
                text += "Loaded: " + dreamItem.Label;
            }
            else
            {
                text += "Empty";
            }

            return text;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
            Scribe_Values.Look(ref loadingInProgress, "loadingInProgress");
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return innerContainer;
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }
    }

    public class Dialog_SelectFactionTarget : Window
    {
        private Building_DreamPod dreamPod;
        private Vector2 scrollPosition;
        private const float EntryHeight = 30f;

        public Dialog_SelectFactionTarget(Building_DreamPod pod)
        {
            dreamPod = pod;
            doCloseButton = true;
            doCloseX = true;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = true;
        }

        public override Vector2 InitialSize => new Vector2(400f, 600f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Small;

            Rect titleRect = inRect.TopPartPixels(30f);
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(titleRect, "Select Target Faction");
            Text.Anchor = TextAnchor.UpperLeft;

            Rect listRect = new Rect(inRect.x, titleRect.yMax + 10f, inRect.width,
                inRect.height - titleRect.height - 10f - CloseButSize.y);

            List<Faction> factions = Find.FactionManager.AllFactions
                .Where(f => !f.IsPlayer && !f.Hidden && !f.HostileTo(Faction.OfPlayer))
                .ToList();

            float viewHeight = factions.Count * EntryHeight;
            Rect viewRect = new Rect(0f, 0f, listRect.width - 16f, viewHeight);

            Widgets.BeginScrollView(listRect, ref scrollPosition, viewRect);
            float curY = 0f;

            foreach (Faction faction in factions)
            {
                Rect entryRect = new Rect(0f, curY, viewRect.width, EntryHeight);
                if (Widgets.ButtonText(entryRect, faction.Name))
                {
                    dreamPod.LaunchDreamToFaction(faction);
                    Close();
                }
                curY += EntryHeight;
            }

            Widgets.EndScrollView();
        }
    }
}
