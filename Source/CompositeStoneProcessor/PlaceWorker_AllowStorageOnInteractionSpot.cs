using RimWorld;
using Verse;

namespace CompositeStoneProcessor
{
    public class PlaceWorker_AllowStorageOnInteractionSpot : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef def, IntVec3 center, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thingToPlace = null)
        {
            ThingDef targetDef = def as ThingDef;
            if (targetDef == null)
                return AcceptanceReport.WasRejected;
            // Allow if it stores items (shelf, hopper, stockpile etc)
            if (targetDef.building?.maxItemsInCell > 0)
                return true;
            if (typeof(Building_Storage).IsAssignableFrom(targetDef.thingClass))
                return true;
            if (targetDef.GetCompProperties<CompProperties_ThingContainer>() != null)
                return true;
            return AcceptanceReport.WasRejected;
        }
    }
}