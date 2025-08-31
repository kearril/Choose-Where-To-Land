using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace ChooseWhereToLand
{

    public class ChooseWhereToLand_Mod : Mod
    {

        public static ChooseWhereToLand_Settings settings;


        public ChooseWhereToLand_Mod(ModContentPack content) : base(content)
        {
            settings = GetSettings<ChooseWhereToLand_Settings>();
        }


        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);


            listing.CheckboxLabeled(
                "CWTL_UseCustomLandingSpot".Translate(),
                ref settings.useCustomLandingSpot
            );

            // 开发者模式下显示重置按钮，用于清空key记录
            if (Prefs.DevMode)
            {
                if (listing.ButtonText("CWTL_Reset".Translate()))
                {
                    settings.noticeHistory.Clear();
                }
            }

            listing.End();
        }


        public override string SettingsCategory() => "CWTL_Setting".Translate();
    }


    public class ChooseWhereToLand_Settings : ModSettings
    {

        public Dictionary<string, string> noticeHistory = new Dictionary<string, string>();


        // 是否启用自定义落点

        public bool useCustomLandingSpot = true;


        public override void ExposeData()
        {
            Scribe_Collections.Look(ref noticeHistory, "noticeHistory");
            Scribe_Values.Look(ref useCustomLandingSpot, "useCustomLandingSpot", true);
        }
    }
}
