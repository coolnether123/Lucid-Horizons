using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Dreams
{
    [StaticConstructorOnStartup]
    public static class DreamsMod
    {
        static DreamsMod()
        {
            try
            {
                var harmony = new Harmony("CoolNether123.DreamCatcher");
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                Log.Message("DreamCatcher initialized.");
            }
            catch (Exception ex)
            {
                Log.Error($"Error initializing DreamCatcher mod: {ex}");
            }
        }
    }

    // Patch to handle dream generation during rest
    [HarmonyPatch(typeof(RestUtility), "InBed")]
    public static class Patch_RestUtility_InBed
    {
        public static void Postfix(Pawn p, ref bool __result)
        {
            if (!__result || p?.needs?.rest == null) return;

            var dreamManager = Current.Game?.GetComponent<DreamManager>();
            if (dreamManager == null)
            {
                Log.Warning("DreamManager is not initialized.");
                return;
            }

            if (p.needs.rest.CurLevel < 0.3f) // Deep sleep threshold
            {
                dreamManager.CheckForDream(p);
            }
        }
    }

    // Patch to ensure DreamManager is initialized on game load
    [HarmonyPatch(typeof(Game), "FinalizeInit")]
    public static class Patch_Game_FinalizeInit
    {
        public static void Postfix()
        {
            if (Current.Game.GetComponent<DreamManager>() == null)
            {
                Log.Message("Initializing DreamManager.");
                Current.Game.components.Add(new DreamManager(Current.Game));
            }
        }
    }
}
