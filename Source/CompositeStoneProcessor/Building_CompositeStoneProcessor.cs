using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

namespace CompositeStoneProcessor
{
    public class Building_CompositeStoneProcessor : Building_WorkTable, IStoreSettingsParent, ISlotGroupParent, IThingHolder
    {
        private SlotGroup slotGroup;
        private StorageSettings storageSettings;
        private List<IntVec3> slotCellsList;
        private ThingOwner<Thing> innerContainer;

        public SlotGroup GetSlotGroup()
        {
            if (slotGroup == null && Spawned) { slotGroup = new SlotGroup(this); UpdateSlotCellsList(); }
            return slotGroup;
        }
        public IEnumerable<IntVec3> AllSlotCells() { yield return Position; }
        public List<IntVec3> AllSlotCellsList() => slotCellsList ?? new List<IntVec3> { Position };
        public StorageSettings SlotGroupSettings => storageSettings;
        public string SlotYielderCategory() => "None";
        public string SlotYielderLabel() => def.LabelCap;
        public bool IgnoreStoredThingsBeauty => true;
        public int GroupingOrder => 0;
        public string GroupingLabel => def.LabelCap;
        public void Notify_LostThing(Thing thing) { }
        public void Notify_ReceivedThing(Thing thing)
        {
            if (thing == null) return;
            if (thing.def.thingCategories?.Contains(ThingCategoryDefOf.StoneChunks) == true)
                AbsorbChunk(thing);
            if (pendingUpgradeResources != null && pendingUpgradeResources.Count > 0)
                TryAcceptUpgradeResource(thing);
        }

        public StorageSettings StoreSettings => storageSettings;
        public bool StorageTabVisible => true;
        public StorageSettings GetStoreSettings() => storageSettings;
        private StorageSettings parentStoreSettings;
        public StorageSettings GetParentStoreSettings()
        {
            if (parentStoreSettings == null) { parentStoreSettings = new StorageSettings(); parentStoreSettings.filter.SetAllow(ThingCategoryDefOf.StoneChunks, true); }
            return parentStoreSettings;
        }
        public void Notify_SettingsChanged() { }
        public bool Accepts(Thing t) => storageSettings != null && storageSettings.filter.Allows(t.def);
        public bool HaulDestinationEnabled => Spawned;
        public ThingOwner GetDirectlyHeldThings() => innerContainer;
        public void GetChildHolders(List<IThingHolder> outChildren) { }

        public bool IsProcessing => isProcessing;
        public bool HasPower => powerComp != null && powerComp.PowerOn;
        public CompRefuelable RefuelableComp => refuelableComp;
        public int ChunkCount => CountChunks();
        public int BillCount { get { int c = 0; for (int i = 0; i < billStack.Count; i++) if (billStack[i].ShouldDoNow()) c++; return c; } }

        private List<RecipeDef> installedUpgrades = new List<RecipeDef>();
        public List<RecipeDef> InstalledUpgrades => installedUpgrades;
        public float TotalSpeed => 1.0f + installedUpgrades.DefaultIfEmpty().Sum(u => u?.GetExt()?.speedUp ?? 0f);
        public int EffectiveSkill => Mathf.Max(CompositeStoneProcessorMod.settings.defaultSkillLevel, installedUpgrades.DefaultIfEmpty().Max(u => u?.GetExt()?.skillLevel ?? 0));

        private CompRefuelable refuelableComp;
        private CompPowerTrader powerComp;
        private int ProcTickInterval => CompositeStoneProcessorMod.settings.tickInterval;

        public float GetTempFactor()
        {
            float t = GenTemperature.GetTemperatureForCell(Position, Map);
            if (t < -30f || t > 120f) return 0f;
            if (t >= -10f && t <= 80f) return 1f;
            if (t < -10f) return 1f - (-10f - t) / 20f;
            return 1f - (t - 80f) / 40f;
        }

        private int progressTicks;
        private int displayProgressPct = -1;
        private bool isProcessing;
        private RecipeDef currentRecipe;
        private int currentIngredientCount;
        private const int MAX_CHUNKS = 30;

        private List<RecipeDef> pendingUpgradeQueue = new List<RecipeDef>();
        private List<ThingDefCountClass> pendingUpgradeResources;
        private int upgradeProgressTicks;

        public override void PostMake() { base.PostMake(); InitStorage(); }
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            InitStorage(); UpdateSlotCellsList();
            refuelableComp = this.GetComp<CompRefuelable>();
            powerComp = this.GetComp<CompPowerTrader>();
        }
        private void InitStorage()
        {
            if (storageSettings == null)
            {
                storageSettings = new StorageSettings(this);
                storageSettings.filter.SetAllow(ThingCategoryDefOf.StoneChunks, true);
                storageSettings.filter.SetAllow(ThingDefOf.Steel, true);
                storageSettings.filter.SetAllow(ThingDefOf.ComponentIndustrial, true);
            }
            if (innerContainer == null) innerContainer = new ThingOwner<Thing>(this);
        }
        private void UpdateSlotCellsList()
        {
            if (slotCellsList == null) slotCellsList = new List<IntVec3>();
            slotCellsList.Clear(); slotCellsList.Add(Position);
        }
        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            if (Spawned && innerContainer != null && innerContainer.Count > 0)
            {
                for (int i = innerContainer.Count - 1; i >= 0; i--)
                {
                    Thing t = innerContainer[i];
                    Thing split = t.SplitOff(t.stackCount);
                    innerContainer.Remove(split);
                    GenPlace.TryPlaceThing(split, Position, Map, ThingPlaceMode.Near);
                }
            }
            pendingUpgradeQueue.Clear();
            pendingUpgradeResources = null;
            base.DeSpawn(mode);
            slotGroup = null;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref storageSettings, "storageSettings", this);
            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
            if (innerContainer == null) innerContainer = new ThingOwner<Thing>(this);
            Scribe_Collections.Look(ref installedUpgrades, "installedUpgrades", LookMode.Def);
            Scribe_Values.Look(ref progressTicks, "progressTicks", 0);
            Scribe_Values.Look(ref isProcessing, "isProcessing", false);
            Scribe_Defs.Look(ref currentRecipe, "currentRecipe");
            Scribe_Values.Look(ref currentIngredientCount, "currentIngredientCount", 0);
            Scribe_Collections.Look(ref pendingUpgradeQueue, "pendingUpgradeQueue", LookMode.Def);
            Scribe_Values.Look(ref upgradeProgressTicks, "upgradeProgressTicks", 0);
            Scribe_Collections.Look(ref pendingUpgradeResources, "pendingUpgradeResources", LookMode.Deep);
        }

        protected override void Tick()
        {
            base.Tick();
            if (!Spawned) return;
            int ti = ProcTickInterval;
            if (ti <= 0) return;

            if (pendingUpgradeQueue.Count > 0 && upgradeProgressTicks < 0 && (pendingUpgradeResources == null || pendingUpgradeResources.Count == 0))
                upgradeProgressTicks = 0;

            if (pendingUpgradeQueue.Count > 0 && upgradeProgressTicks >= 0 && upgradeProgressTicks < (int)pendingUpgradeQueue[0].workAmount && this.IsHashIntervalTick(ti))
            {
                if (powerComp != null && powerComp.PowerOn || (refuelableComp != null && refuelableComp.HasFuel))
                {
                    upgradeProgressTicks += Mathf.RoundToInt(ti * 0.5f);
                    if (upgradeProgressTicks >= (int)pendingUpgradeQueue[0].workAmount) CompleteUpgrade();
                }
            }

            bool hp = powerComp != null && powerComp.PowerOn;
            bool hf = refuelableComp != null && refuelableComp.HasFuel;
            if (!hp && !hf) { SetPowerConsumption(0); CancelProcessing(); return; }

            if (!isProcessing)
            {
                if (this.IsHashIntervalTick(ProcTickInterval))
                {
                    Bill_Production candidate = FindBill();
                    if (candidate != null && HasIngredient(candidate))
                    {
                        currentRecipe = candidate.recipe;
                        currentIngredientCount = (currentRecipe.ingredients.NullOrEmpty() ? 0 : (int)currentRecipe.ingredients[0].GetBaseCount());
                        progressTicks = 0; displayProgressPct = 0; isProcessing = true;
                    }
                }
                if (!isProcessing) { SetPowerConsumption(0); }
                return;
            }
            if (!this.IsHashIntervalTick(ti)) return;
            if (currentRecipe == null || !BillStillActive()) { SetPowerConsumption(0); CancelProcessing(); return; }

            float tf = GetTempFactor();
            if (tf <= 0f) { SetPowerConsumption(0); CancelProcessing(); return; }

            MachineRecipeExtension ext = currentRecipe.GetModExtension<MachineRecipeExtension>();
            if (ext != null)
            {
                if (!hp && hf)
                {
                    float fuelAmt = Mathf.Max(0.001f, ext.fuelConsumptionRate);
                    refuelableComp.ConsumeFuel(Mathf.Max(1, Mathf.RoundToInt(fuelAmt)));
                }
                SetPowerConsumption(hp ? Mathf.Max(0, ext.powerConsume) : 0);
            }
            else
            {
                if (!hp && hf)
                {
                    refuelableComp.ConsumeFuel(Mathf.Max(1, Mathf.RoundToInt(CompositeStoneProcessorMod.settings.defaultFuelRate)));
                    if (powerComp != null) powerComp.PowerOutput = -1;
                }
                SetPowerConsumption(hp ? CompositeStoneProcessorMod.settings.defaultPowerConsume : 0);
            }

            float speed = TotalSpeed * tf;
            progressTicks += Mathf.RoundToInt(ti * speed);
            displayProgressPct = (int)((float)progressTicks / currentRecipe.workAmount * 100f);
            if (progressTicks >= currentRecipe.workAmount) CompleteProcessing();
        }

        private void SetPowerConsumption(int extra)
        {
            if (powerComp != null)
            {
                bool onGrid = powerComp.PowerNet != null;
                int target = onGrid ? -(50 + extra) : 0;
                if (powerComp.PowerOutput != target) powerComp.PowerOutput = target;
            }
        }

        private int CountIngredient(RecipeDef r)
        {
            int c = 0;
            for (int i = 0; i < innerContainer.Count; i++)
                if (r.fixedIngredientFilter.Allows(innerContainer[i].def)) c += innerContainer[i].stackCount;
            return c;
        }
        private int CountChunks()
        {
            if (innerContainer == null) return 0; int c = 0;
            for (int i = 0; i < innerContainer.Count; i++)
                if (innerContainer[i].def.thingCategories?.Contains(ThingCategoryDefOf.StoneChunks) == true) c += innerContainer[i].stackCount;
            return c;
        }
        private void AbsorbChunk(Thing thing)
        {
            if (innerContainer.Count >= MAX_CHUNKS) return;
            int space = MAX_CHUNKS - innerContainer.Count;
            int take = Math.Min(thing.stackCount, space);
            Thing split = thing.SplitOff(take);
            if (split != null && !innerContainer.TryAdd(split)) split.Destroy(DestroyMode.Vanish);
        }
        private void TryAcceptUpgradeResource(Thing thing)
        {
            if (pendingUpgradeResources == null || pendingUpgradeResources.Count == 0)
            {
                if (thing.Spawned) GenPlace.TryPlaceThing(thing, thing.Position, Map, ThingPlaceMode.Near);
                return;
            }
            for (int i = 0; i < pendingUpgradeResources.Count; i++)
            {
                if (pendingUpgradeResources[i].thingDef == thing.def && pendingUpgradeResources[i].count > 0)
                {
                    int take = (thing.stackCount < pendingUpgradeResources[i].count) ? thing.stackCount : pendingUpgradeResources[i].count;
                    pendingUpgradeResources[i].count -= take;
                    thing.SplitOff(take).Destroy(DestroyMode.Vanish);
                    if (pendingUpgradeResources[i].count <= 0)
                        pendingUpgradeResources.RemoveAt(i);
                    break;
                }
            }
        }
        private Bill_Production FindBill()
        {
            for (int i = 0; i < billStack.Count; i++)
            {
                if (billStack[i] is Bill_Production bill && bill.ShouldDoNow())
                {
                    int req = (bill.recipe.skillRequirements.NullOrEmpty())
                        ? CompositeStoneProcessorMod.settings.defaultSkillLevel
                        : bill.recipe.skillRequirements[0].minLevel;
                    if (EffectiveSkill >= req)
                        return bill;
                }
            }
            return null;
        }
        private bool HasIngredient(Bill_Production bill)
        {
            if (bill?.recipe == null) return false;
            int req = bill.recipe.ingredients.NullOrEmpty() ? 0 : (int)bill.recipe.ingredients[0].GetBaseCount();
            return CountIngredient(bill.recipe) >= req;
        }
        private void CompleteProcessing()
        {
            if (currentRecipe == null) { SetPowerConsumption(0); CancelProcessing(); return; }
            int rem = currentIngredientCount;
            var candidates = new List<Thing>();
            for (int i = 0; i < innerContainer.Count; i++)
                if (currentRecipe.fixedIngredientFilter.Allows(innerContainer[i].def))
                    candidates.Add(innerContainer[i]);
            for (int i = candidates.Count - 1; i >= 0 && rem > 0; i--)
            {
                Thing t = candidates[i];
                int take = Math.Min(rem, t.stackCount);
                Thing split = t.SplitOff(take); innerContainer.Remove(split); split.Destroy(DestroyMode.Vanish);
                rem -= take;
            }
            if (currentRecipe.products != null)
                foreach (var prod in currentRecipe.products)
                {
                    IntVec3 drop = InteractionCell;
                    if (!drop.Standable(Map)) drop = GenAdj.CellsAdjacentCardinal(this).FirstOrDefault(c => c.Standable(Map));
                    if (drop.IsValid)
                    {
                        Thing spawned = GenSpawn.Spawn(prod.thingDef, drop, Map);
                        if (spawned != null)
                        {
                            spawned.stackCount = prod.count;
                            TryStoreInContainer(spawned, drop);
                        }
                    }
                }
            FindBill()?.Notify_IterationCompleted(null, null);
            SetPowerConsumption(0);
            CancelProcessing();
        }
        private void TryStoreInContainer(Thing thing, IntVec3 cell)
        {
            List<Thing> things = Map.thingGrid.ThingsListAt(cell);
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i] is IThingHolder holder && things[i] != this)
                {
                    ThingOwner container = holder.GetDirectlyHeldThings();
                    if (container != null)
                    {
                        if (!container.TryAdd(thing))
                        {
                            GenSpawn.Spawn(thing, cell, Map);
                        }
                        return;
                    }
                }
            }
        }
        private bool BillStillActive()
        {
            for (int i = 0; i < billStack.Count; i++)
                if (billStack[i] is Bill_Production bp && bp.recipe == currentRecipe) return true;
            return false;
        }
        private void CancelProcessing()
        {
            if (isProcessing) { isProcessing = false; currentRecipe = null; currentIngredientCount = 0; progressTicks = 0; displayProgressPct = -1; }
        }

        // Upgrade system
        public bool IsInstalled(RecipeDef def) => installedUpgrades.Contains(def);
        public bool IsPending(RecipeDef def) => pendingUpgradeQueue.Contains(def);
        public bool CanInstall(RecipeDef def)
        {
            if (def == null) return false;
            if (installedUpgrades.Contains(def)) return false;
            if (def.researchPrerequisite != null && !def.researchPrerequisite.IsFinished) return false;
            MachineUpdateRecipeExtension ext = def.GetExt();
            if (ext?.componentsPrerequisites != null)
            {
                foreach (var prereq in ext.componentsPrerequisites)
                    if (!installedUpgrades.Contains(prereq)) return false;
            }
            return true;
        }
        public void RequestUpgradeInstall(RecipeDef def)
        {
            if (def == null || installedUpgrades.Contains(def)) return;
            if (def.researchPrerequisite != null && !def.researchPrerequisite.IsFinished) return;
            if (pendingUpgradeQueue.Contains(def)) return;
            pendingUpgradeQueue.Add(def);
            if (pendingUpgradeQueue.Count == 1)
            {
                pendingUpgradeResources = CloneIngredientList(def.ingredients);
                upgradeProgressTicks = -1;
            }
        }
        public bool HasPendingUpgradeResources => !pendingUpgradeResources.NullOrEmpty();
        public List<ThingDefCountClass> PendingUpgradeResources => pendingUpgradeResources;
        public bool NeedsResource(ThingDef def) => pendingUpgradeResources?.Any(r => r.thingDef == def && r.count > 0) ?? false;
        public int GetRemainingCount(ThingDef def) => pendingUpgradeResources?.FirstOrDefault(r => r.thingDef == def)?.count ?? 0;
        private List<ThingDefCountClass> CloneIngredientList(List<IngredientCount> source)
        {
            if (source == null) return new List<ThingDefCountClass>();
            var list = new List<ThingDefCountClass>();
            foreach (var s in source)
                list.Add(new ThingDefCountClass(s.filter.AnyAllowedDef, (int)s.GetBaseCount()));
            return list;
        }
        private void CompleteUpgrade()
        {
            if (pendingUpgradeQueue.Count == 0) return;
            RecipeDef completed = pendingUpgradeQueue[0];
            if (!installedUpgrades.Contains(completed))
            {
                installedUpgrades.Add(completed);
                if (completed.GetExt()?.unlockRecipe != null)
                {
                    foreach (var r in completed.GetExt().unlockRecipe)
                    {
                        if (r != null)
                        {
                            bool alreadyHas = false;
                            for (int i = 0; i < billStack.Count; i++)
                                if (billStack[i] is Bill_Production bp && bp.recipe == r) { alreadyHas = true; break; }
                            if (!alreadyHas) billStack.AddBill(new Bill_Production(r));
                        }
                    }
                }
            }
            pendingUpgradeQueue.RemoveAt(0);
            if (pendingUpgradeQueue.Count > 0)
            {
                pendingUpgradeResources = CloneIngredientList(pendingUpgradeQueue[0].ingredients);
                upgradeProgressTicks = -1;
            }
            else
            {
                pendingUpgradeResources = null;
                upgradeProgressTicks = 0;
            }
            Messages.Message("CompositeStoneProcessor_UpgradeInstalled".Translate(), this, MessageTypeDefOf.TaskCompletion, false);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo g in base.GetGizmos()) yield return g;
            if (CountChunks() > 0)
                yield return new Command_Action { defaultLabel = "UnloadChunksLabel".Translate(), defaultDesc = "UnloadChunksDesc".Translate(), action = EjectChunks };
        }
        private void EjectChunks()
        {
            if (innerContainer.Count == 0) return;
            List<IntVec3> cells = new List<IntVec3>();
            for (int radius = 1; radius <= 4; radius++)
            {
                foreach (IntVec3 c in GenRadial.RadialCellsAround(Position, radius, true))
                {
                    if (c == Position) continue;
                    if (c.InBounds(Map) && c.Standable(Map) && Map.thingGrid.ThingsListAt(c).Count == 0) cells.Add(c);
                }
                if (cells.Count >= innerContainer.Count) break;
            }
            if (cells.Count == 0) { Messages.Message("UnloadNoSpace".Translate(), this, MessageTypeDefOf.RejectInput, false); return; }
            int cellIdx = 0;
            for (int i = innerContainer.Count - 1; i >= 0 && cellIdx < cells.Count; i--)
            { Thing t = innerContainer[i]; if (t.def.thingCategories?.Contains(ThingCategoryDefOf.StoneChunks) == true) { Thing split = t.SplitOff(t.stackCount); innerContainer.Remove(split); GenSpawn.Spawn(split, cells[cellIdx], Map); cellIdx++; } }
        }

        public override void DrawGUIOverlay()
        {
            if (refuelableComp != null && refuelableComp.HasFuel) return;
            base.DrawGUIOverlay();
        }

        public override string GetInspectString()
        {
            string text = base.GetInspectString();
            text += "\n" + "StorageStatus".Translate(CountChunks(), MAX_CHUNKS);
            if (isProcessing && currentRecipe != null && displayProgressPct >= 0)
                text += "\n" + "ProcessingStatus".Translate(displayProgressPct);
            float tf = GetTempFactor();
            float tmp = GenTemperature.GetTemperatureForCell(Position, Map);
            text += "\n" + "TempStatus".Translate(tmp.ToString("F0"), (tf * 100f).ToString("F0"));
            text += "\n" + "PowerStatus".Translate(powerComp != null && powerComp.PowerOn ? "PowerElectric".Translate() : (refuelableComp != null && refuelableComp.HasFuel ? "PowerFuel".Translate() : "PowerNone".Translate()));
            if (pendingUpgradeQueue.Count > 0)
            {
                if (upgradeProgressTicks < 0 && pendingUpgradeResources != null && pendingUpgradeResources.Count > 0)
                {
                    string prog = "DeliveryProgress".Translate() + " ";
                    var orig = pendingUpgradeQueue[0].ingredients;
                    foreach (var o in orig)
                    {
                        int total = (int)o.GetBaseCount();
                        ThingDef oDef = o.filter?.AnyAllowedDef;
                        int remaining = total;
                        if (oDef != null)
                        {
                            for (int i = 0; i < pendingUpgradeResources.Count; i++)
                                if (pendingUpgradeResources[i].thingDef == oDef) { remaining = pendingUpgradeResources[i].count; break; }
                            prog += oDef.LabelCap + " " + (total - remaining) + "/" + total + " ";
                        }
                    }
                    text += "\n" + prog;
                }
                else
                {
                    int pct = (upgradeProgressTicks >= 0) ? Mathf.Min(100, upgradeProgressTicks * 100 / (int)pendingUpgradeQueue[0].workAmount) : 0;
                    text += "\n" + "UpgradeProgressStatus".Translate(pendingUpgradeQueue[0].label, pct);
                }
            }
            return text;
        }
    }
}