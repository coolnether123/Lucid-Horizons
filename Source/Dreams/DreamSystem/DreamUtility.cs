using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Dreams
{
    public static class DreamUtility
    {
        private static readonly float ROOM_SCORE_THRESHOLD_TERRIBLE = 30f;
        private static readonly float ROOM_SCORE_THRESHOLD_POOR = 50f;
        private static readonly float ROOM_SCORE_THRESHOLD_DECENT = 70f;
        private static readonly float ROOM_SCORE_THRESHOLD_GOOD = 100f;
        private static readonly float ROOM_SCORE_THRESHOLD_EXCELLENT = 150f;

        public static DreamPart GenerateRandomDream(Pawn pawn, float roomScore, float bedQuality)
        {
            DreamPart dream = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("DreamPart")) as DreamPart;
            if (dream == null) return null;

            dream.type = GenerateDreamType(roomScore, bedQuality);
            dream.subject = GenerateDreamSubject(pawn);
            dream.vividness = GenerateDreamVividness(roomScore, bedQuality);

            return dream;
        }

        private static DreamType GenerateDreamType(float roomScore, float bedQuality)
        {
            // Base weights for each dream type
            Dictionary<DreamType, float> weights = new Dictionary<DreamType, float>
            {
                { DreamType.NightTerror, 1f },
                { DreamType.Nightmare, 2f },
                { DreamType.Mid, 4f },
                { DreamType.Epic, 2f },
                { DreamType.Inspirational, 1f }
            };

            // Modify weights based on room score
            float roomMultiplier = GetRoomScoreMultiplier(roomScore);
            if (roomMultiplier > 1f)
            {
                weights[DreamType.Epic] *= roomMultiplier;
                weights[DreamType.Inspirational] *= roomMultiplier;
                weights[DreamType.NightTerror] /= roomMultiplier;
                weights[DreamType.Nightmare] /= roomMultiplier;
            }
            else
            {
                weights[DreamType.NightTerror] *= (2f - roomMultiplier);
                weights[DreamType.Nightmare] *= (2f - roomMultiplier);
                weights[DreamType.Epic] /= (2f - roomMultiplier);
                weights[DreamType.Inspirational] /= (2f - roomMultiplier);
            }

            // Modify weights based on bed quality
            float bedMultiplier = Mathf.Lerp(0.5f, 1.5f, bedQuality);
            weights[DreamType.Epic] *= bedMultiplier;
            weights[DreamType.Inspirational] *= bedMultiplier;

            return weights.RandomElementByWeight(kvp => kvp.Value).Key;
        }

        private static float GetRoomScoreMultiplier(float roomScore)
        {
            if (roomScore <= ROOM_SCORE_THRESHOLD_TERRIBLE) return 0.5f;
            if (roomScore <= ROOM_SCORE_THRESHOLD_POOR) return 0.75f;
            if (roomScore <= ROOM_SCORE_THRESHOLD_DECENT) return 1f;
            if (roomScore <= ROOM_SCORE_THRESHOLD_GOOD) return 1.25f;
            if (roomScore <= ROOM_SCORE_THRESHOLD_EXCELLENT) return 1.5f;
            return 2f;
        }

        private static DreamSubject GenerateDreamSubject(Pawn pawn)
        {
            DreamSubject subject = new DreamSubject();

            // Weights for different subject types
            float pawnWeight = 0.4f;
            float factionWeight = 0.2f;
            float skillWeight = 0.2f;
            float abstractWeight = 0.2f;

            float totalWeight = pawnWeight + factionWeight + skillWeight + abstractWeight;
            float random = Rand.Value * totalWeight;

            if (random < pawnWeight)
            {
                subject.type = DreamSubject.SubjectType.Pawn;
                subject.reference = GetRandomRelevantPawn(pawn)?.ThingID ?? "unknown";
            }
            else if (random < pawnWeight + factionWeight)
            {
                subject.type = DreamSubject.SubjectType.Faction;
                subject.reference = GetRandomRelevantFaction(pawn)?.def.defName ?? "unknown";
            }
            else if (random < pawnWeight + factionWeight + skillWeight)
            {
                subject.type = DreamSubject.SubjectType.Skill;
                subject.reference = GetRandomRelevantSkill(pawn)?.defName ?? "unknown";
            }
            else
            {
                subject.type = DreamSubject.SubjectType.Abstract;
                subject.reference = GetRandomAbstractConcept();
            }

            return subject;
        }

        private static Pawn GetRandomRelevantPawn(Pawn dreamer)
        {
            List<Pawn> potentialPawns = dreamer.Map.mapPawns.AllPawnsSpawned
                .Where(p => p.RaceProps.Humanlike && p != dreamer)
                .OrderByDescending(p => dreamer.relations.OpinionOf(p))
                .Take(5)
                .ToList();

            return potentialPawns.Any() ? potentialPawns.RandomElement() : null;
        }

        private static Faction GetRandomRelevantFaction(Pawn dreamer)
        {
            return Find.FactionManager.AllFactions
                .Where(f => f != dreamer.Faction && !f.Hidden)
                .RandomElementWithFallback();
        }

        private static SkillDef GetRandomRelevantSkill(Pawn dreamer)
        {
            if (dreamer.skills?.skills != null)
            {
                List<SkillDef> relevantSkills = dreamer.skills.skills
                    .Where(s => s.passion != Passion.None || s.Level > 5)
                    .Select(s => s.def)
                    .ToList();

                if (relevantSkills.Any())
                {
                    return relevantSkills.RandomElement();
                }
            }

            return DefDatabase<SkillDef>.AllDefs.RandomElement();
        }

        private static string GetRandomAbstractConcept()
        {
            List<string> concepts = new List<string>
            {
                "home", "freedom", "wealth", "power", "love",
                "death", "life", "nature", "technology", "art",
                "music", "war", "peace", "family", "adventure"
            };
            return concepts.RandomElement();
        }

        private static DreamVividness GenerateDreamVividness(float roomScore, float bedQuality)
        {
            // Combine room score and bed quality to determine vividness chances
            float quality = (roomScore / 100f + bedQuality) / 2f;
            quality = Mathf.Clamp01(quality);

            if (quality < 0.2f) return DreamVividness.Faint;
            if (quality < 0.4f) return DreamVividness.Dull;
            if (quality < 0.6f) return DreamVividness.Normal;
            if (quality < 0.8f) return DreamVividness.Vivid;
            return DreamVividness.Lucid;
        }

        public static bool IsPawnInDeepSleep(Pawn pawn)
        {
            if (pawn?.needs?.rest == null) return false;
            
            return !pawn.Awake() && 
                   pawn.CurJob?.def == JobDefOf.LayDown && 
                   pawn.needs.rest.CurLevel < 0.3f; // Deep sleep threshold
        }
    }
}
