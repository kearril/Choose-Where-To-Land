using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace ChooseWhereToLand
{
    public class PawnsArrivalModeWorker_ChooseWhereToLand : PawnsArrivalModeWorker
    {
        // 穿梭机默认的朝向，初始为东
        private static Rot4 shuttleRotation = Rot4.East;

        public override void Arrive(List<Pawn> pawns, IncidentParms parms)
        {
        }

        // 当运输器抵达地图时调用，开启选点界面
        public override void TravellingTransportersArrived(List<ActiveTransporterInfo> transporters, Map map)
        {
            // 进入隐藏UI的截图模式，隐藏大部分UI控件
            Find.ScreenshotModeHandler.Active = true;

            // 判断是否是穿梭机
            if (transporters.IsShuttle())
            {
                // 取第一个穿梭机运输器信息
                ActiveTransporterInfo transporter = transporters.FirstOrDefault();

                // 获取穿梭机实体
                Thing shuttle = transporter.GetShuttle();

                // 获取穿梭机的定义（如果无则默认使用通用穿梭机定义）
                ThingDef shuttleDef = shuttle?.def ?? ThingDefOf.Shuttle;

                // 获取穿梭机默认放置朝向
                shuttleRotation = shuttleDef.defaultPlacingRot;

                // 弹出目标点选取界面
                Find.Targeter.BeginTargeting(
                    TargetingParameters.ForCell(),    // 目标类型为地图上的单格
                    delegate (LocalTargetInfo x)      // 玩家确认落点后回调
                    {
                        // 确认后关闭截图模式，恢复UI
                        Find.ScreenshotModeHandler.Active = false;
                        // 将穿梭机部署到目标位置
                        TransportersArrivalActionUtility.DropShuttle(transporter, map, x.Cell, shuttleRotation);
                    },
                    delegate (LocalTargetInfo x)      // 选点时预览穿梭机的模型
                    {
                        RoyalTitlePermitWorker_CallShuttle.DrawShuttleGhost(x, map, shuttleDef, shuttleRotation);
                    },
                    delegate (LocalTargetInfo x)      // 验证落点是否合法
                    {
                        AcceptanceReport report = RoyalTitlePermitWorker_CallShuttle.ShuttleCanLandHere(x, map, shuttleDef, shuttleRotation);

                        // 不合法则弹出消息提示
                        if (!report.Accepted)
                        {
                            Messages.Message(report.Reason, new LookTargets(x.Cell, map), MessageTypeDefOf.RejectInput, historical: false);
                        }
                        return report.Accepted;
                    },
                    caster: null,                    // 没有施法者
                    actionWhenFinished: () =>
                    {
                        // 无论确认或取消，结束后恢复UI显示
                        Find.ScreenshotModeHandler.Active = false;
                        Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
                    },
                    CompLaunchable.TargeterMouseAttachment, // 鼠标样式为运输舱相关样式
                    true, // 播放选点音效
                    delegate                          // 选点期间每帧调用
                    {
                        // 支持玩家通过快捷键旋转穿梭机朝向
                        if (shuttleDef.rotatable)
                        {
                            if (KeyBindingDefOf.Designator_RotateRight.KeyDownEvent)
                            {
                                shuttleRotation = shuttleRotation.Rotated(RotationDirection.Clockwise);
                            }
                            if (KeyBindingDefOf.Designator_RotateLeft.KeyDownEvent)
                            {
                                shuttleRotation = shuttleRotation.Rotated(RotationDirection.Counterclockwise);
                            }
                        }

                        // 选点时强制暂停游戏
                        if (!Find.TickManager.Paused)
                        {
                            Find.TickManager.TogglePaused();
                        }

                        // 禁止鼠标右键取消选点
                        if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
                        {
                            Event.current.Use();
                        }

                        // Esc 启动默认中心降落
                        if (KeyBindingDefOf.Cancel.KeyDownEvent)
                        {
                            Event.current.Use();

                            // 退出截图模式
                            Find.ScreenshotModeHandler.Active = false;

                            // 找到默认中心落点
                            IntVec3 spot;
                            if (!DropCellFinder.TryFindRaidDropCenterClose(out spot, map))
                                spot = DropCellFinder.FindRaidDropCenterDistant(map);

                            // 执行中心降落
                            TransportersArrivalActionUtility.DropShuttle(transporter, map, spot);
                            // 恢复游戏速度
                            Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
                            // 停止选点
                            Find.Targeter.StopTargeting();
                        }



                    });
            }
            else
            {
                // 非穿梭机情况，处理普通运输舱的投放
                var capturedTransporters = new List<ActiveTransporterInfo>(transporters);

                // 弹出目标点选择界面
                Find.Targeter.BeginTargeting(
                    TargetingParameters.ForDropPodsDestination(), // 运输舱可投放的落点参数
                    delegate (LocalTargetInfo x)                   // 玩家确认落点后的回调
                    {
                        // 关闭截图模式，恢复UI
                        Find.ScreenshotModeHandler.Active = false;
                        // 执行运输舱投放操作
                        TransportersArrivalActionUtility.DropTravellingDropPods(capturedTransporters, x.Cell, map);
                    },
                    null, // 无高亮动作
                    delegate (LocalTargetInfo x) // 验证落点是否合法
                    {
                        AcceptanceReport report = CheckDropCellReport(x, map);

                        // 不合法时弹出提示
                        if (!report.Accepted)
                        {
                            Messages.Message(report.Reason, new LookTargets(x.Cell, map), MessageTypeDefOf.RejectInput, historical: false);
                        }

                        return report.Accepted;
                    },
                    null, // 无施法者
                    actionWhenFinished: () =>
                    {
                        // 选点结束时恢复UI显示
                        Find.ScreenshotModeHandler.Active = false;
                        Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
                    },
                    CompLaunchable.TargeterMouseAttachment, // 鼠标样式
                    true, // 播放选点音效
                    delegate (LocalTargetInfo x) // 选点期间每帧调用
                    {
                        // 选点时强制暂停游戏
                        if (!Find.TickManager.Paused)
                        {
                            Find.TickManager.TogglePaused();
                        }

                        // 禁止鼠标右键取消
                        if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
                        {
                            Event.current.Use();
                        }
                        // Esc 启动默认中心降落
                        if (KeyBindingDefOf.Cancel.KeyDownEvent)
                        {
                            Event.current.Use();

                            // 退出截图模式
                            Find.ScreenshotModeHandler.Active = false;
                          
                            // 找到默认中心落点
                            IntVec3 spot;
                            if (!DropCellFinder.TryFindRaidDropCenterClose(out spot, map))
                                spot = DropCellFinder.FindRaidDropCenterDistant(map);

                            // 执行中心降落
                            TransportersArrivalActionUtility.DropTravellingDropPods(capturedTransporters, spot, map);

                            // 恢复游戏速度
                            Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;

                            // 停止选点
                            Find.Targeter.StopTargeting();
                        }


                    },
                    null // 无额外的更新动作
                );
            }
        }

        // 自定义验证运输舱落点是否合法的函数，返回AcceptanceReport，包含翻译提示
        private static AcceptanceReport CheckDropCellReport(LocalTargetInfo x, Map map)
        {
            if (!x.IsValid)
            {
                return "CWTL_Disallowedlandingspot".Translate(); // 坐标无效
            }
            if (!x.Cell.InBounds(map))
            {
                return "CWTL_Disallowedlandingspot".Translate(); // 坐标越界
            }
            if (x.Cell.Fogged(map))
            {
                return "CWTL_Disallowedlandingspot".Translate(); // 目标在迷雾中
            }
            if (!DropCellFinder.CanPhysicallyDropInto(x.Cell, map, canRoofPunch: true))
            {
                return "CWTL_Disallowedlandingspot".Translate(); // 有障碍物或不可落点（允许打穿屋顶）
            }

            return true;
        }


        // 禁用默认的突袭落点中心解析，避免干扰
        public override bool TryResolveRaidSpawnCenter(IncidentParms parms)
        {
            return true;
        }
    }
}
