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
        private static readonly Color CBorder = new Color(0.5f, 0.5f, 0.5f, 0.4f);

        protected override void FillTab()
        {
            Building_CompositeStoneProcessor p = SelThing as Building_CompositeStoneProcessor;
            if (p == null) return;

            List<RecipeDef> all = DefDatabase<RecipeDef>.AllDefsListForReading.Where(r => r.GetModExtension<MachineUpdateRecipeExtension>() != null).ToList();
            all.Sort((a, b) => (a.GetExt()?.sortOrder ?? 999).CompareTo(b.GetExt()?.sortOrder ?? 999));

            Color bg = CompositeStoneProcessorMod.settings.BgColor;
            Color boxBg = new Color(bg.r * 0.8f, bg.g * 0.8f, bg.b * 0.8f);

            float boxWidth = this.size.x - 28f;

            float totalSpeed = p.TotalSpeed;
            float tempFactor = p.GetTempFactor();
            float finalSpeed = totalSpeed * tempFactor;
            
            string h1 = "FinalSpeedStatus".Translate(finalSpeed.ToString("F2"), totalSpeed.ToString("F2"), (tempFactor * 100f).ToString("F0"));
            

            Text.Font = GameFont.Medium;
            float headerTitleH = 28f;
            Text.Font = GameFont.Small;
            float h1H = Text.CalcHeight(h1, boxWidth);
                        float headerH = headerTitleH + h1H + 8f;
            float headerBoxH = headerH - 4f;

            float[] boxHs = new float[all.Count];
            float totalH = headerH + 10f;
            for (int i = 0; i < all.Count; i++)
            {
                RecipeDef d = all[i];
                UState st = GetState(p, d);

                string cs = GetCost(d);
                float costH = Text.CalcHeight("UpgradeCostLabel".Translate() + " " + cs, boxWidth);
                float speedUp = d.GetExt()?.speedUp ?? 0;
                float speedH = (speedUp > 0) ? Text.CalcHeight("SpeedInfo".Translate(speedUp.ToString("F2"), d.GetExt()?.skillLevel ?? 0), boxWidth) : 0f;

                string thirdLine = "";
                if (st == UState.Locked && d.researchPrerequisite != null)
                    thirdLine = "RequiresResearchStatus".Translate(d.researchPrerequisite.LabelCap);
                else if (st == UState.Installed && d.GetExt()?.unlockRecipe != null && d.GetExt()?.unlockRecipe.Count > 0)
                {
                    string ul = "UnlockedRecipes".Translate() + ": ";
                    foreach (var r in d.GetExt()?.unlockRecipe) ul += r.LabelCap + " ";
                    thirdLine = ul;
                }
                float thirdH = (thirdLine.Length > 0) ? Text.CalcHeight(thirdLine, boxWidth) : 0f;

                float h = 34f + costH + 4f + speedH + 4f + thirdH + 4f;
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

            Text.Font = GameFont.Medium;
            GUI.color = Color.white;
            GUI.color = boxBg;
            GUI.DrawTexture(new Rect(2f, y, view.width - 4f, headerBoxH), BaseContent.WhiteTex);
            GUI.color = CBorder;
            Widgets.DrawBox(new Rect(2f, y, view.width - 4f, headerBoxH), 1);
            GUI.color = Color.white;
            Widgets.Label(new Rect(10f, y + 3f, view.width - 20f, headerTitleH), "UpgradeSummaryLabel".Translate());
            y += headerTitleH + 2f;

            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(10f, y, view.width - 20f, h1H), h1);
            
            y += h1H + 8f;

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(4f, y, view.width, 24f), "ConfigurableComponentsLabel".Translate());
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

                Widgets.Label(new Rect(10f, y + 3f, view.width * 0.55f, 26f), d.label);

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

                // Fully dynamic content layout using Text.CalcHeight
                float dy = y + 34f;

                string cs2 = GetCost(d);
                float costH2 = Text.CalcHeight("UpgradeCostLabel".Translate() + " " + cs2, view.width - 20f);
                Widgets.Label(new Rect(10f, dy, view.width - 20f, costH2), "UpgradeCostLabel".Translate() + " " + cs2);
                dy += costH2 + 4f;

                float speedUp2 = d.GetExt()?.speedUp ?? 0;
                if (speedUp2 > 0)
                {
                    string si2 = "SpeedInfo".Translate(speedUp2.ToString("F2"), d.GetExt()?.skillLevel ?? 0);
                    float speedH2 = Text.CalcHeight(si2, view.width - 20f);
                    Widgets.Label(new Rect(10f, dy, view.width - 20f, speedH2), si2);
                    dy += speedH2 + 4f;
                }

                if (st == UState.Locked && d.researchPrerequisite != null)
                {
                    GUI.color = CGray;
                    string t3 = "RequiresResearchStatus".Translate(d.researchPrerequisite.LabelCap);
                    float h3 = Text.CalcHeight(t3, view.width - 20f);
                    Widgets.Label(new Rect(10f, dy, view.width - 20f, h3), t3);
                }
                else if (st == UState.Installed && d.GetExt()?.unlockRecipe != null && d.GetExt()?.unlockRecipe.Count > 0)
                {
                    GUI.color = CGreen;
                    string ul = "UnlockedRecipes".Translate() + ": ";
                    foreach (var r in d.GetExt()?.unlockRecipe) ul += r.LabelCap + " ";
                    float h3 = Text.CalcHeight(ul, view.width - 20f);
                    Widgets.Label(new Rect(10f, dy, view.width - 20f, h3), ul);
                }
                else if (st == UState.Installing && p.PendingUpgradeResources != null && p.PendingUpgradeResources.Count > 0)
                {
                    GUI.color = COrange;
                    string prog = "DeliveryProgress".Translate() + " ";
                    foreach (var o in d.ingredients)
                    {
                        int total = (int)o.GetBaseCount();
                        ThingDef oDef = o.filter?.AnyAllowedDef;
                        int remaining = total;
                        if (oDef != null)
                        {
                            for (int j = 0; j < p.PendingUpgradeResources.Count; j++)
                                if (p.PendingUpgradeResources[j].thingDef == oDef)
                                { remaining = p.PendingUpgradeResources[j].count; break; }
                            prog += oDef.LabelCap + " " + (total - remaining) + "/" + total + " ";
                        }
                    }
                    float h3 = Text.CalcHeight(prog, view.width - 20f);
                    Widgets.Label(new Rect(10f, dy, view.width - 20f, h3), prog);
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