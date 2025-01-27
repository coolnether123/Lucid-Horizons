using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;

namespace Dreams
{
    public class JobDriver_UseDream : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOnBurningImmobile(TargetIndex.A);

            // Go to the dream item
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            // Use the dream
            Toil useDream = new Toil();
            useDream.initAction = () =>
            {
                Pawn actor = useDream.actor;
                Thing dreamItem = job.targetA.Thing;

                if (dreamItem is DreamPart dreamPart)
                {
                    // Apply mood effect based on dream type and vividness
                    float moodEffect = dreamPart.GetMoodEffect();
                    ThoughtDef thoughtDef = GetThoughtDefForDream(dreamPart.type);
                    
                    if (thoughtDef != null)
                    {
                        actor.needs?.mood?.thoughts?.memories?.TryGainMemory(thoughtDef);
                    }

                    // Apply additional thought if dream is about friend/rival
                    if (dreamPart.subject.type == DreamSubject.SubjectType.Pawn)
                    {
                        Pawn dreamPawn = Find.World.worldPawns.AllPawnsAlive
                            .FirstOrDefault(p => p.ThingID == dreamPart.subject.reference);
                            
                        if (dreamPawn != null)
                        {
                            int opinion = actor.relations.OpinionOf(dreamPawn);
                            if (opinion >= 20)
                            {
                                actor.needs?.mood?.thoughts?.memories?.TryGainMemory(ThoughtDef.Named("DreamAboutFriend"));
                            }
                            else if (opinion <= -20)
                            {
                                actor.needs?.mood?.thoughts?.memories?.TryGainMemory(ThoughtDef.Named("DreamAboutRival"));
                            }
                        }
                    }

                    // Destroy the dream item after use
                    dreamItem.Destroy();
                }
            };
            useDream.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return useDream;
        }

        private ThoughtDef GetThoughtDefForDream(DreamType type)
        {
            switch (type)
            {
                case DreamType.NightTerror:
                    return ThoughtDef.Named("ExperiencedNightTerror");
                case DreamType.Nightmare:
                    return ThoughtDef.Named("ExperiencedNightmare");
                case DreamType.Mid:
                    return ThoughtDef.Named("ExperiencedMidDream");
                case DreamType.Epic:
                    return ThoughtDef.Named("ExperiencedEpicDream");
                case DreamType.Inspirational:
                    return ThoughtDef.Named("ExperiencedInspirationalDream");
                default:
                    return null;
            }
        }
    }
}
