using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace ChooseWhereToLand
{
    /// <summary>
    /// 主模组类：管理设置和模组界面
    /// </summary>
    public class ChooseWhereToLand_Mod : Mod
    {
        /// <summary>
        /// 模组全局设置（静态字段，方便 Patch 或其他类访问）
        /// </summary>
        public static ChooseWhereToLand_Settings settings;

        /// <summary>
        /// 构造函数：初始化模组设置
        /// </summary>
        public ChooseWhereToLand_Mod(ModContentPack content) : base(content)
        {
            settings = GetSettings<ChooseWhereToLand_Settings>();
        }

        /// <summary>
        /// 绘制模组设置界面
        /// </summary>
        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            // 勾选框：是否启用自定义落点
            listing.CheckboxLabeled(
                "CWTL_UseCustomLandingSpot".Translate(),  // 标签文字，可本地化
                ref settings.useCustomLandingSpot         // 绑定到设置字段
            );

            // 开发者模式下显示重置按钮，用于清空通知历史
            if (Prefs.DevMode)
            {
                if (listing.ButtonText("CWTL_Reset".Translate()))
                {
                    settings.noticeHistory.Clear();
                }
            }

            listing.End();
        }

        /// <summary>
        /// 设置界面分类名称
        /// </summary>
        public override string SettingsCategory() => "CWTL_Setting".Translate();
    }

    /// <summary>
    /// 模组设置类：存储用户设置和通知历史
    /// </summary>
    public class ChooseWhereToLand_Settings : ModSettings
    {
        /// <summary>
        /// 已显示的通知历史
        /// key: NoticeDef.defName, value: 当前通知的 key
        /// </summary>
        public Dictionary<string, string> noticeHistory = new Dictionary<string, string>();

        /// <summary>
        /// 是否启用自定义落点
        /// </summary>
        public bool useCustomLandingSpot = true;

        /// <summary>
        /// 存档/加载方法
        /// </summary>
        public override void ExposeData()
        {
            Scribe_Collections.Look(ref noticeHistory, "noticeHistory");
            Scribe_Values.Look(ref useCustomLandingSpot, "useCustomLandingSpot", true);
        }
    }
}
