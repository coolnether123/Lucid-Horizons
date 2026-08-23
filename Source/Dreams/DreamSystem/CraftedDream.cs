using RimWorld;
using Verse;

namespace Dreams
{
    public class CraftedDream : ThingWithComps
    {
        public DreamType type;
        public DreamSubject subject;
        public DreamVividness vividness;

        public override string Label
        {
            get
            {
                return $"Crafted {vividness} {type} Dream about {subject.Label}";
            }
        }

        public override string DescriptionDetailed
        {
            get
            {
                return $"A {vividness.ToString().ToLower()} crafted dream about {subject.Label}. " +
                    $"It is {GetDreamTypeDescription(type)}. {GetEffectDescription()}";
            }
        }

        private string GetDreamTypeDescription(DreamType dreamType)
        {
            switch (dreamType)
            {
                case DreamType.NightTerror:
                    return "terrifying";
                case DreamType.Nightmare:
                    return "full of fear and anxiety";
                case DreamType.Mid:
                    return "strange but neither pleasant nor upsetting";
                case DreamType.Epic:
                    return "full of wonder";
                case DreamType.Inspirational:
                    return "an inspiring glimpse of new possibilities";
                default:
                    return "hard to understand";
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

            return baseEffect * vividnessMultiplier * 1.5f; // Crafted dreams are 50% more potent
        }

        private string GetEffectDescription()
        {
            float effect = GetMoodEffect();
            string direction = effect > 0 ? "improve" : "worsen";
            return $"This crafted dream will {direction} mood by {System.Math.Abs(effect):F1}.";
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref type, "type");
            Scribe_Deep.Look(ref subject, "subject");
            Scribe_Values.Look(ref vividness, "vividness");
        }
    }
}
