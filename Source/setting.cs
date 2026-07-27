using System.Collections.Generic;
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

            listing.End();
        }

        public override string SettingsCategory() => "CWTL_Setting".Translate();
    }

    public class ChooseWhereToLand_Settings : ModSettings
    {
        
        public bool useCustomLandingSpot = true;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref useCustomLandingSpot, "useCustomLandingSpot", true);
        }
    }
}
