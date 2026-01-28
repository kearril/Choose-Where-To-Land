using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ChooseWhereToLand
{
    [StaticConstructorOnStartup]
    public static class Notice
    {
        public static ChooseWhereToLand_Mod notice_Mod;
        public static ChooseWhereToLand_Settings notice_Settings;
        public static List<NoticeDef> noticeDefs;


        static Notice()
        {

            notice_Mod = LoadedModManager.GetMod<ChooseWhereToLand_Mod>();
            notice_Settings = notice_Mod.GetSettings<ChooseWhereToLand_Settings>();


            noticeDefs = DefDatabase<NoticeDef>.AllDefs.ToList();
        }


        public static void CreateNewVersionDialog(NoticeDef noticeDef)
        {

            Texture2D image = null;
            if (!string.IsNullOrEmpty(noticeDef.imagePath))
                image = ContentFinder<Texture2D>.Get(noticeDef.imagePath, true);


            Find.WindowStack.Add(new Dialog_CWTLNotice(
                text: noticeDef.description,
                buttonAText: "CWTL_accept".Translate(),
                buttonAAction: null,
                buttonBText: string.IsNullOrEmpty(noticeDef.url) ? null : "CWTL_Openmodlink".Translate(),
                buttonBAction: string.IsNullOrEmpty(noticeDef.url) ? null : new Action(() =>
                {
                    Application.OpenURL(noticeDef.url);
                }),
                title: noticeDef.LabelCap
            )
            {
                image = image
            });
        }
    }


    public class NoticeDef : Def
    {
        public string key;
        public string url;
        public string imagePath;
    }


    public class NoticeGameComponent : GameComponent
    {

        public NoticeGameComponent(Game game) { }

        public override void LoadedGame()
        {
            base.LoadedGame();
            LongEventHandler.ExecuteWhenFinished(() => ShowNoticesOnce());
        }

        public override void StartedNewGame()
        {
            base.StartedNewGame();
            LongEventHandler.ExecuteWhenFinished(() => ShowNoticesOnce());
        }


        private void ShowNoticesOnce()
        {
            return;
            
            foreach (NoticeDef noticeDef in Notice.noticeDefs)
            {
                string lastKey;

                bool shouldShow = !Notice.notice_Settings.noticeHistory.TryGetValue(noticeDef.defName, out lastKey)
                                  || noticeDef.key != lastKey;

                if (shouldShow)
                {
                    Notice.notice_Settings.noticeHistory[noticeDef.defName] = noticeDef.key;

                    Notice.CreateNewVersionDialog(noticeDef);
                }
            }


            Notice.notice_Mod.WriteSettings();
        }
    }
}
