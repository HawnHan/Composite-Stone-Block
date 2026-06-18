using RimWorld;
using Verse;
using Verse.AI;
using System.Collections.Generic;

namespace CompositeStoneProcessor
{
    public class WorkGiver_UpgradeHaul : WorkGiver_Scanner
    {
        private static ThingDef processorDef;
        private static ThingDef ProcessorDef
        {
            get
            {
                if (processorDef == null) processorDef = DefDatabase<ThingDef>.GetNamed("CompositeStoneProcessor");
                return processorDef;
            }
        }

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            foreach (Thing t in pawn.Map.listerThings.ThingsOfDef(ProcessorDef))
            {
                if (t is Building_CompositeStoneProcessor b && b.Spawned && b.HasPendingUpgradeResources)
                {
                    foreach (var req in b.PendingUpgradeResources)
                    {
                        foreach (Thing res in pawn.Map.listerThings.ThingsOfDef(req.thingDef))
                        {
                            if (!res.IsForbidden(pawn) && pawn.CanReserve(res))
                                yield return res;
                        }
                    }
                }
            }
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Building_CompositeStoneProcessor proc = FindProcessor(pawn, t.def);
            if (proc == null) return null;
            if (!pawn.CanReserve(proc.Position)) return null;
            int needed = proc.GetRemainingCount(t.def);
            if (needed <= 0) return null;
            Job job = JobMaker.MakeJob(JobDefOf.HaulToCell, t, proc.Position);
            job.count = (needed < t.stackCount) ? needed : t.stackCount;
            return job;
        }

        private Building_CompositeStoneProcessor FindProcessor(Pawn pawn, ThingDef def)
        {
            foreach (Thing t in pawn.Map.listerThings.ThingsOfDef(ProcessorDef))
            {
                if (t is Building_CompositeStoneProcessor b && b.Spawned && b.NeedsResource(def))
                    return b;
            }
            return null;
        }
    }
}