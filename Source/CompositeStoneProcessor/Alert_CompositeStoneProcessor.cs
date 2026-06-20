using RimWorld;
using Verse;
using System.Collections.Generic;

namespace CompositeStoneProcessor
{
    public class Alert_CompositeStoneProcessor : Alert
    {
        public override AlertReport GetReport()
        {
            if (!CompositeStoneProcessorMod.settings.alertEnabled) return false;

            foreach (Map map in Find.Maps)
            {
                foreach (Building t in map.listerBuildings.allBuildingsColonist)
                {
                    if (t is Building_CompositeStoneProcessor b && !b.Destroyed && b.Spawned)
                    {
                        bool hasP = b.HasPower || (b.RefuelableComp != null && b.RefuelableComp.HasFuel);
                        if ((hasP && b.ChunkCount == 0 && b.BillCount > 0) || (!hasP && b.BillCount > 0))
                            return true;
                    }
                }
            }
            return false;
        }

        public override TaggedString GetExplanation()
        {
            string text = "";
            foreach (Map map in Find.Maps)
            {
                foreach (Building t in map.listerBuildings.allBuildingsColonist)
                {
                    if (t is Building_CompositeStoneProcessor b && !b.Destroyed && b.Spawned)
                    {
                        bool hasP = b.HasPower || (b.RefuelableComp != null && b.RefuelableComp.HasFuel);
                        if (hasP && b.ChunkCount == 0 && b.BillCount > 0)
                            text += "AlertNeedChunks".Translate() + ": " + b.LabelCap + "\n";
                        else if (!hasP && b.BillCount > 0)
                            text += "AlertNeedPower".Translate() + ": " + b.LabelCap + "\n";
                    }
                }
            }
            return text;
        }
    }
}
