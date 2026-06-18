using RimWorld;
using Verse;
using System.Collections.Generic;

namespace CompositeStoneProcessor
{
    public class MachineUpdateRecipeExtension : DefModExtension
    {
        public float speedUp;
        public int sortOrder = 999;
        public int skillLevel;
        public List<RecipeDef> unlockRecipe;
    }

    public static class UpgradeHelper
    {
        public static MachineUpdateRecipeExtension GetExt(this RecipeDef r)
        {
            return r.GetModExtension<MachineUpdateRecipeExtension>();
        }
    }
}