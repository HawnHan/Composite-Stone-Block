using RimWorld;
using Verse;
using UnityEngine;

namespace CompositeStoneProcessor
{
    public class CompositeStoneProcessorMod : Mod
    {
        public static CompositeStoneProcessorSettings settings;
        private Vector2 scrollPos;

        public CompositeStoneProcessorMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<CompositeStoneProcessorSettings>();
        }

        public override void DoSettingsWindowContents(UnityEngine.Rect inRect)
        {
            Rect viewRect = new Rect(0f, 0f, inRect.width - 20f, 680f);
            Widgets.BeginScrollView(inRect, ref scrollPos, viewRect);
            Listing_Standard list = new Listing_Standard();
            list.Begin(viewRect);
            list.Gap(12f);

            bool alert = settings.alertEnabled;
            list.CheckboxLabeled("ModSetting_Alert".Translate(), ref alert);
            settings.alertEnabled = alert;
            list.Gap(12f);

            int interval = settings.tickInterval;
            list.Label("ModSetting_Interval".Translate(interval));
            interval = Mathf.RoundToInt(list.Slider(interval, 60f, 1000f) / 10f) * 10;
            settings.tickInterval = interval;
            list.Gap(6f);

            list.Label("ModSetting_BGColor".Translate(settings.bgColorHex));
            string bgInput = settings.bgColorHex;
            string result = Widgets.TextField(new Rect(viewRect.x + 10f, viewRect.y + list.CurHeight, 100f, 22f), bgInput);
            list.Gap(26f);
            if (result.Length == 6 || result.Length == 7)
            {
                string clean = result.TrimStart('#');
                if (clean.Length == 6) settings.bgColorHex = clean;
            }
            list.Gap(6f);

            float fuelDef = settings.defaultFuelRate;
            list.Label("ModSetting_DefaultFuel".Translate(fuelDef.ToString("F1")));
            fuelDef = Mathf.Round(list.Slider(fuelDef, 0.1f, 3.0f) * 10f) / 10f;
            settings.defaultFuelRate = fuelDef;
            list.Gap(6f);

            int pwrDef = settings.defaultPowerConsume;
            list.Label("ModSetting_DefaultPower".Translate(pwrDef));
            pwrDef = Mathf.RoundToInt(list.Slider(pwrDef, 50f, 500f) / 10f) * 10;
            settings.defaultPowerConsume = pwrDef;
            list.Gap(6f);

            int skillDef = settings.defaultSkillLevel;
            list.Label("ModSetting_DefaultSkillLevel".Translate(skillDef));
            skillDef = Mathf.RoundToInt(list.Slider((float)skillDef, 1f, 20f));
            settings.defaultSkillLevel = skillDef;

            list.GapLine(12f);
            if (list.ButtonText("ModSetting_Reset".Translate()))
            {
                settings.alertEnabled = true; settings.tickInterval = 120; settings.bgColorHex = "282828";
                settings.defaultFuelRate = 0.5f; settings.defaultPowerConsume = 200; settings.defaultSkillLevel = 6;
            }
            list.Gap(4f);
            Text.Font = GameFont.Tiny; GUI.color = Color.gray;
            list.Label("ButterPlusPlusNote".Translate());
            GUI.color = Color.white; Text.Font = GameFont.Small;

            list.End();
            Widgets.EndScrollView();
            settings.Write();
        }

        public override string SettingsCategory() => "Composite Stone Processor";
    }

    public class CompositeStoneProcessorSettings : ModSettings
    {
        public bool alertEnabled = true;
        public int tickInterval = 120;
        public string bgColorHex = "282828";
        public float defaultFuelRate = 0.5f;
        public int defaultPowerConsume = 200;
        public int defaultSkillLevel = 6;

        public Color BgColor
        {
            get
            {
                Color c;
                if (ColorUtility.TryParseHtmlString("#" + bgColorHex.TrimStart('#'), out c))
                    return c;
                return new Color(0.157f, 0.157f, 0.157f);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref alertEnabled, "alertEnabled", true);
            Scribe_Values.Look(ref tickInterval, "tickInterval", 120);
            Scribe_Values.Look(ref bgColorHex, "bgColorHex", "282828");
            Scribe_Values.Look(ref defaultFuelRate, "defaultFuelRate", 0.5f);
            Scribe_Values.Look(ref defaultPowerConsume, "defaultPowerConsume", 200);
            Scribe_Values.Look(ref defaultSkillLevel, "defaultSkillLevel", 6);
        }
    }
}