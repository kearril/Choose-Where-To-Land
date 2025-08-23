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
    public static class Notice
    {
        public static ChooseWhereToLand_Mod notice_Mod;           // 模组实例
        public static ChooseWhereToLand_Settings notice_Settings; // 模组设置
        public static List<NoticeDef> noticeDefs;      // 所有通知定义列表

        // 静态构造函数，在游戏启动时执行
        static Notice()
        {
            // 获取模组实例和设置
            notice_Mod = LoadedModManager.GetMod<ChooseWhereToLand_Mod>();
            notice_Settings = notice_Mod.GetSettings<ChooseWhereToLand_Settings>();

            // 获取所有公告定义
            noticeDefs = DefDatabase<NoticeDef>.AllDefs.ToList();
        }

        // 创建并显示通知窗口
        public static void CreateNewVersionDialog(NoticeDef noticeDef)
        {
            // 加载图片，如果 imagePath 不为空
            Texture2D image = null;
            if (!string.IsNullOrEmpty(noticeDef.imagePath))
                image = ContentFinder<Texture2D>.Get(noticeDef.imagePath, true);

            // 创建并加入 Dialog_MessageBox 弹窗
            Find.WindowStack.Add(new Dialog_MessageBox(
                text: noticeDef.description,    // 正文内容
                buttonAText: "CWTL_accept".Translate(),
                buttonAAction: null,           // 确认按钮动作可为空
                buttonBText: string.IsNullOrEmpty(noticeDef.url) ? null : "CWTL_Openmodlink".Translate(),
                buttonBAction: string.IsNullOrEmpty(noticeDef.url) ? null : new Action(() =>
                {
                    Application.OpenURL(noticeDef.url); // 打开链接
                }),
                title: noticeDef.LabelCap       // 弹窗标题
            )
            {
                image = image                  // 可选图片
            });
        }
    }

    // -------------------------
    // 公告定义类：用于 XML 配置
    // -------------------------
    public class NoticeDef : Def
    {
        public string key;        // 唯一标识当前通知版本，用于判断是否已显示
        public string url;        // 链接 URL，可点击打开
        public string imagePath;  // 图片路径，可选
    }

    // -------------------------
    // GameComponent：在存档加载完成或新游戏开始时触发通知
    // -------------------------
    public class NoticeGameComponent : GameComponent
    {
        // 构造函数必须有 Game 参数
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
