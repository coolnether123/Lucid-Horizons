using RimWorld;
using Verse;
using System;
using System.Linq;

namespace Dreams
{
    public class DreamPart : ThingWithComps
    {
        public DreamType type;
        public DreamSubject subject;
        public DreamVividness vividness;

        public override string Label
        {
            get
            {
                return $"{vividness} {type} Dream about {subject.Label}";
            }
        }

        public override string DescriptionDetailed
        {
            get
            {
                return $"A {vividness.ToString().ToLower()} dream {GetDreamTypeDescription(type)} about {subject.Label}. {GetEffectDescription()}";
            }
        }

        private string GetDreamTypeDescription(DreamType dreamType)
        {
            switch (dreamType)
            {
                case DreamType.NightTerror:
                    return "filled with absolute terror and dread";
                case DreamType.Nightmare:
                    return "of fear and anxiety";
                case DreamType.Mid:
                    return "neither good nor bad";
                case DreamType.Epic:
                    return "of wonder and amazement";
                case DreamType.Inspirational:
                    return "of pure inspiration and possibility";
                default:
                    return "of unknown quality";
            }
        }

        public float GetMoodEffect()
        {
            // Base effect from dream type
            float baseEffect;
            switch (type)
            {
                case DreamType.NightTerror:
                    baseEffect = -20f;
                    break;
                case DreamType.Nightmare:
                    baseEffect = -12f;
                    break;
                case DreamType.Mid:
                    baseEffect = 6f;
                    break;
                case DreamType.Epic:
                    baseEffect = 12f;
                    break;
                case DreamType.Inspirational:
                    baseEffect = 20f;
                    break;
                default:
                    baseEffect = 0f;
                    break;
            }

            // Modify based on vividness
            float vividnessMultiplier;
            switch (vividness)
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

            return baseEffect * vividnessMultiplier;
        }

        private string GetEffectDescription()
        {
            float effect = GetMoodEffect();
            string direction = effect > 0 ? "improve" : "worsen";
            return $"When used, this dream will {direction} mood by {Math.Abs(effect):F1}.";
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref type, "type");
            Scribe_Deep.Look(ref subject, "subject");
            Scribe_Values.Look(ref vividness, "vividness");
        }
    }

    public enum DreamType
    {
        NightTerror,
        Nightmare,
        Mid,
        Epic,
        Inspirational
    }

    public enum DreamVividness
    {
        Faint,
        Dull,
        Normal,
        Vivid,
        Lucid
    }

    public class DreamSubject : IExposable
    {
        public enum SubjectType
        {
            Pawn,
            Faction,
            Skill,
            Abstract
        }

        public SubjectType type;
        public string reference; // Could be pawn ID, faction def name, skill def name, or abstract concept
        public string Label => GetLabel();

        private string GetLabel()
        {
            switch (type)
            {
                case SubjectType.Pawn:
                    Pawn pawn = Find.WorldPawns.AllPawnsAlive.FirstOrDefault(p => p.ThingID == reference);
                    return pawn?.LabelShort ?? "someone";
                case SubjectType.Faction:
                    Faction faction = Find.FactionManager.AllFactions.FirstOrDefault(f => f.def.defName == reference);
                    return faction?.Name ?? "a faction";
                case SubjectType.Skill:
                    SkillDef skill = DefDatabase<SkillDef>.GetNamed(reference, false);
                    return skill?.label ?? "a skill";
                case SubjectType.Abstract:
                    return reference;
                default:
                    return "something";
            }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref type, "type");
            Scribe_Values.Look(ref reference, "reference");
        }
    }
}
