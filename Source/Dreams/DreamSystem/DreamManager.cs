using RimWorld;
using Verse;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Dreams
{
    public class DreamManager : GameComponent
    {
        private Dictionary<Pawn, float> nextDreamTicks = new Dictionary<Pawn, float>();
        private const float BASE_DREAM_INTERVAL = 32f * 2500f; // 32 hours in ticks

        public DreamManager() { }

        public DreamManager(Game game) : base() { }

        public override void GameComponentTick()
        {
            if (Find.TickManager.TicksGame % 250 != 0) return; // Check every 250 ticks

            foreach (Map map in Find.Maps)
            {
                foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
                {
                    if (!pawn.RaceProps.Humanlike) continue;
                    if (!pawn.Awake() && pawn.CurJob?.def == JobDefOf.LayDown)
                    {
                        CheckForDream(pawn);
                    }
                }
            }
        }

        public void CheckForDream(Pawn pawn)
        {
            if (!nextDreamTicks.TryGetValue(pawn, out float nextDreamTick))
            {
                nextDreamTicks[pawn] = Find.TickManager.TicksGame + BASE_DREAM_INTERVAL;
                return;
            }

            if (Find.TickManager.TicksGame >= nextDreamTick)
            {
                TryGenerateDream(pawn);
                nextDreamTicks[pawn] = Find.TickManager.TicksGame + GetModifiedDreamInterval(pawn);
            }
        }

        private float GetModifiedDreamInterval(Pawn pawn)
        {
            float interval = BASE_DREAM_INTERVAL;

            // Modify based on bed quality
            Building_Bed bed = pawn.CurrentBed();
            if (bed != null)
            {
                interval *= Mathf.Lerp(1f, 0.8f, bed.GetStatValue(StatDefOf.BedRestEffectiveness));
            }

            return interval;
        }

        private void TryGenerateDream(Pawn pawn)
        {
            if (!IsPawnInDeepSleep(pawn)) return;

            Room room = pawn.GetRoom();
            if (room == null) return;

            float roomScore = room.GetStat(RoomStatDefOf.Impressiveness);
            Building_Bed bed = pawn.CurrentBed();
            float bedQuality = bed?.GetStatValue(StatDefOf.BedRestEffectiveness) ?? 0.5f;

            DreamPart dreamPart = DreamUtility.GenerateRandomDream(pawn, roomScore, bedQuality);
            if (dreamPart != null)
            {
                Building_DreamCatcher dreamCatcher = GetRoomDreamCatcher(room);
                if (dreamCatcher != null)
                {
                    dreamCatcher.NotifyDreamGenerated(pawn, dreamPart);
                }
                else
                {
                    Log.Message($"Dream generated for {pawn.LabelShort} but no DreamCatcher found.");
                }
            }
        }

        private bool IsPawnInDeepSleep(Pawn pawn)
        {
            return pawn?.needs?.rest != null &&
                   !pawn.Awake() &&
                   pawn.CurJob?.def == JobDefOf.LayDown &&
                   pawn.needs.rest.CurLevel < 0.3f; // Deep sleep threshold
        }

        private Building_DreamCatcher GetRoomDreamCatcher(Room room)
        {
            return room.ContainedAndAdjacentThings
                .OfType<Building_DreamCatcher>()
                .FirstOrDefault();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref nextDreamTicks, "nextDreamTicks", LookMode.Reference, LookMode.Value,
                ref pawnKeys, ref floatValues);
            if (Scribe.mode == LoadSaveMode.LoadingVars && nextDreamTicks == null)
            {
                nextDreamTicks = new Dictionary<Pawn, float>();
            }
        }

        private List<Pawn> pawnKeys = new List<Pawn>();
        private List<float> floatValues = new List<float>();
    }
}