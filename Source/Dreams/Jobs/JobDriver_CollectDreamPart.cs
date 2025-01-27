using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;

namespace Dreams
{
    public class JobDriver_CollectDreamPart : JobDriver
    {
        private const int CollectDuration = 100;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOnBurningImmobile(TargetIndex.A);

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            Toil collectDream = new Toil()
                .FailOnDespawnedOrNull(TargetIndex.A)
                .FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch)
                .WithProgressBarToilDelay(TargetIndex.A, CollectDuration)
                .WithEffect(EffecterDefOf.Research, TargetIndex.A);

            collectDream.initAction = () =>
            {
                Pawn actor = collectDream.actor;
                Building dreamCatcher = (Building)job.targetA.Thing;
                actor.rotationTracker.FaceCell(dreamCatcher.Position);
                actor.pather.StopDead();
            };

            collectDream.tickAction = () =>
            {
                if (collectDream.actor.jobs.curDriver.ticksLeftThisToil % 30 == 0)
                {
                    FleckMaker.ThrowLightningGlow(job.targetA.Thing.TrueCenter(), job.targetA.Thing.Map, 0.5f);
                }
            };

            collectDream.defaultCompleteMode = ToilCompleteMode.Delay;
            collectDream.defaultDuration = CollectDuration;

            yield return collectDream;

            yield return new Toil
            {
                initAction = () =>
                {
                    Building_DreamCatcher dreamCatcher = job.targetA.Thing as Building_DreamCatcher;
                    if (dreamCatcher != null && dreamCatcher.StoredDreamParts.Count > 0)
                    {
                        DreamPart dreamPart = dreamCatcher.StoredDreamParts[0];
                        dreamCatcher.StoredDreamParts.RemoveAt(0);

                        if (!pawn.inventory.innerContainer.TryAdd(dreamPart))
                        {
                            GenPlace.TryPlaceThing(dreamPart, pawn.Position, pawn.Map, ThingPlaceMode.Near);
                        }
                    }
                }
            };
        }
    }
}