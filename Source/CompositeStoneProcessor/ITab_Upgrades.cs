using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace CompositeStoneProcessor
{
    public class ITab_Upgrades : ITab
    {
        private Vector2 scrollPosition;
        public override bool IsVisible => SelThing is Building_CompositeStoneProcessor;
        public ITab_Upgrades() { labelKey = "TabUpgradesLabel"; }

        private enum UState { Locked, Ready, Installing, Installed }

        private static readonly Color COrange = new Color(1f, 0.6f, 0f);
        private static readonly Color CGreen = new Color(0.3f, 0.9f, 0.3f);
        private static readonly Color CGray = new Color(0.5f, 0.5f, 0.5f);

        protected override void FillTab()
        {
            Building_CompositeStoneProcessor p = SelThing as Building_CompositeStoneProcessor;
            if (p == null) return;

            List<RecipeDef> all = DefDatabase<RecipeDef>.AllDefsListForReading.Where(r => r.GetModExtension<MachineUpdateRecipeExtension>() != null).ToList();
            all.Sort((a, b) => (a.GetExt()?.sortOrder ?? 999).CompareTo(b.GetExt()?.sortOrder ?? 999));

            Color bg = CompositeStoneProcessorMod.settings.BgColor;
            Color boxBg = new Color(bg.r * 0.8f, bg.g * 0.8f, bg.b * 0.8f);

            float boxWidth = this.size.x - 28f;
            float headerH = 60f;
            float[] boxHs = new float[all.Count];
            float totalH = headerH + 10f;
            for (int i = 0; i < all.Count; i++)
            {
                RecipeDef d = all[i];
                UState st = GetState(p, d);
                float h = 68f;
                string thirdLine = "";
                if (st == UState.Locked && d.researchPrerequisite != null)
                    thirdLine = "RequiresResearchStatus".Translate(d.researchPrerequisite.LabelCap);
                else if (st == UState.Installed && d.GetExt()?.unlockRecipe != null && d.GetExt()?.unlockRecipe.Count > 0)
                {
                    string ul = "UnlockedRecipes".Translate() + ": ";
                    foreach (var r in d.GetExt()?.unlockRecipe) ul += r.LabelCap + " ";
                    thirdLine = ul;
                }
                if (thirdLine.Length > 0) h += Text.CalcHeight(thirdLine, boxWidth);
                boxHs[i] = h;
                totalH += h + 6f;
            }

            Vector2 sz = this.size;
            Rect outer = new Rect(0f, 0f, sz.x, sz.y);
            GUI.color = bg; GUI.DrawTexture(outer, BaseContent.WhiteTex); GUI.color = Color.white;

            Rect inner = outer.ContractedBy(6f);
            Rect view = new Rect(0f, 0f, inner.width - 16f, totalH);
            Widgets.BeginScrollView(inner, ref scrollPosition, view);

            float y = 2f;

            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            float totalSpeed = p.TotalSpeed;
            float tempFactor = p.GetTempFactor();
            float finalSpeed = totalSpeed * tempFactor;
            Widgets.Label(new Rect(4f, y, view.width, 22f), "FinalSpeedStatus".Translate(finalSpeed.ToString("F2"), totalSpeed.ToString("F2"), (tempFactor * 100f).ToString("F0")));
            y += 24f;
            float tmp = GenTemperature.GetTemperatureForCell(p.Position, p.Map);
            Widgets.Label(new Rect(4f, y, view.width, 22f), "TempStatus".Translate(tmp.ToString("F0"), (tempFactor * 100f).ToString("F0")));
            y += 30f;

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(4f, y, view.width, 22f), "TabUpgradesLabel".Translate());
            y += 26f;
            Text.Font = GameFont.Small;

            for (int idx = 0; idx < all.Count; idx++)
            {
                RecipeDef d = all[idx];
                UState st = GetState(p, d);
                float boxH = boxHs[idx];
                Color tc = st switch { UState.Locked => CGray, UState.Installing => COrange, UState.Installed => CGreen, _ => Color.white };
                string bt = st switch
                {
                    UState.Locked => "UpgradeLocked".Translate(),
                    UState.Ready => "UpgradeReady".Translate(),
                    UState.Installing => "UpgradeInstalling".Translate(),
                    UState.Installed => "UpgradeConfigured".Translate(),
                };

                GUI.color = boxBg;
                GUI.DrawTexture(new Rect(2f, y, view.width - 4f, boxH), BaseContent.WhiteTex);
                GUI.color = tc;
                Widgets.DrawBox(new Rect(2f, y, view.width - 4f, boxH), 1);

                Rect nl = new Rect(10f, y + 3f, view.width * 0.55f, 26f);
                Widgets.Label(nl, d.label);

                Rect br = new Rect(view.width * 0.58f, y + 2f, view.width * 0.38f, 28f);
                if (st == UState.Ready && p.CanInstall(d))
                {
                    GUI.color = Color.white;
                    if (Widgets.ButtonText(br, bt))
                    {
                        p.RequestUpgradeInstall(d);
                        Messages.Message("UpgradeRequestedMsg".Translate(d.LabelCap), p, MessageTypeDefOf.TaskCompletion, false);
                    }
                }
                else
                {
                    GUI.color = tc;
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label(br, bt);
                    Text.Anchor = TextAnchor.UpperLeft;
                }

                GUI.color = tc * 0.4f;
                Widgets.DrawLineHorizontal(8f, y + 32f, view.width - 20f);
                GUI.color = tc;

                string cs = GetCost(d);
                Widgets.Label(new Rect(10f, y + 36f, view.width - 20f, 20f), "UpgradeCostLabel".Translate() + " " + cs);

                string si = "SpeedInfo".Translate((d.GetExt()?.speedUp ?? 0).ToString("F2"), d.GetExt()?.skillLevel ?? 0);
                Widgets.Label(new Rect(10f, y + 56f, view.width - 20f, 20f), si);

                float thirdY = y + 68f;
                if (st == UState.Locked && d.researchPrerequisite != null)
                {
                    GUI.color = CGray;
                    string t3 = "RequiresResearchStatus".Translate(d.researchPrerequisite.LabelCap);
                    float h3 = Text.CalcHeight(t3, view.width - 20f);
                    Widgets.Label(new Rect(10f, thirdY, view.width - 20f, h3), t3);
                }
                else if (st == UState.Installed && d.GetExt()?.unlockRecipe != null && d.GetExt()?.unlockRecipe.Count > 0)
                {
                    GUI.color = CGreen;
                    string ul = "UnlockedRecipes".Translate() + ": ";
                    foreach (var r in d.GetExt()?.unlockRecipe) ul += r.LabelCap + " ";
                    float h3 = Text.CalcHeight(ul, view.width - 20f);
                    Widgets.Label(new Rect(10f, thirdY, view.width - 20f, h3), ul);
                }

                y += boxH + 6f;
            }

            Widgets.EndScrollView();
            GUI.color = Color.white;
        }

        private UState GetState(Building_CompositeStoneProcessor p, RecipeDef d)
        {
            if (p.IsInstalled(d)) return UState.Installed;
            if (p.IsPending(d)) return UState.Installing;
            if (d.researchPrerequisite != null && !d.researchPrerequisite.IsFinished) return UState.Locked;
            return UState.Ready;
        }

        private string GetCost(RecipeDef d)
        {
            if (d.ingredients == null || d.ingredients.Count == 0) return "UpgradeNoCost".Translate();
            string s = "";
            foreach (var r in d.ingredients) s += (int)r.GetBaseCount() + " " + r.filter.AnyAllowedDef.LabelCap + " ";
            return s.Trim();
        }

        protected override void UpdateSize() { base.UpdateSize(); this.size = new Vector2(400f, 500f); }
    }
}