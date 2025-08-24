using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace ChooseWhereToLand
{
    [StaticConstructorOnStartup]
    public static class Notice// 静态类：管理通知系统，notice的具体实例
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

            
            Find.WindowStack.Add(new Dialog_MessageBox(
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
        public string key;        // 标识符
        public string url;        // 链接 URL
        public string imagePath;  // 图片路径，可选
    }

  
    public class NoticeGameComponent : GameComponent
    {
       
        public NoticeGameComponent(Game game) { }

        // 存档加载完成时触发
        public override void LoadedGame()
        {
            base.LoadedGame();
            LongEventHandler.ExecuteWhenFinished(() => ShowNoticesOnce());
        }

        // 新游戏开始时触发
        public override void StartedNewGame()
        {
            base.StartedNewGame();
            LongEventHandler.ExecuteWhenFinished(() => ShowNoticesOnce());
        }

        // 显示一次性通知
        private void ShowNoticesOnce()
        {
            foreach (NoticeDef noticeDef in Notice.noticeDefs)
            {
                string lastKey;

                // 判断是否已经显示过该通知
                bool shouldShow = !Notice.notice_Settings.noticeHistory.TryGetValue(noticeDef.defName, out lastKey)
                                  || noticeDef.key != lastKey;

                if (shouldShow)
                {
                    // 更新历史记录为最新 key
                    Notice.notice_Settings.noticeHistory[noticeDef.defName] = noticeDef.key;

                    // 创建并显示通知窗口
                    Notice.CreateNewVersionDialog(noticeDef);
                }
            }

            // 保存设置
            Notice.notice_Mod.WriteSettings();
        }
    }
}
