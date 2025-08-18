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

        // 当运输器抵达地图时调用，弹出选点界面
        public override void TravellingTransportersArrived(List<ActiveTransporterInfo> transporters, Map map)
        {
            // 隐藏大部分 UI 控件
            Find.ScreenshotModeHandler.Active = true;

            // 判断是否是穿梭机运输器
            if (transporters.IsShuttle())
            {
                // 获取第一个穿梭机运输器
                ActiveTransporterInfo transporter = transporters.FirstOrDefault();

                // 获取穿梭机实体
                Thing shuttle = transporter.GetShuttle();

                // 获取穿梭机的定义，如果没有则使用默认通用穿梭机
                ThingDef shuttleDef = shuttle?.def ?? ThingDefOf.Shuttle;

                // 获取穿梭机默认放置朝向
                shuttleRotation = shuttleDef.defaultPlacingRot;

                // 弹出落点选取界面
                Find.Targeter.BeginTargeting(
                    TargetingParameters.ForCell(),    // 允许选择地图单格作为目标
                    delegate (LocalTargetInfo x)      // 玩家确认落点后的回调
                    {
                        //恢复 UI
                        Find.ScreenshotModeHandler.Active = false;
                        // 将穿梭机部署到指定位置
                        TransportersArrivalActionUtility.DropShuttle(transporter, map, x.Cell, shuttleRotation);
                    },
                    delegate (LocalTargetInfo x)      // 可视化预览
                    {
                        RoyalTitlePermitWorker_CallShuttle.DrawShuttleGhost(x, map, shuttleDef, shuttleRotation);
                    },
                    delegate (LocalTargetInfo x)      // 验证落点是否合法
                    {
                        AcceptanceReport report = RoyalTitlePermitWorker_CallShuttle.ShuttleCanLandHere(x, map, shuttleDef, shuttleRotation);

                        // 不合法时弹出提示
                        if (!report.Accepted)
                        {
                            Messages.Message(report.Reason, new LookTargets(x.Cell, map), MessageTypeDefOf.RejectInput, historical: false);
                        }
                        return report.Accepted;
                    },
                    caster: null,                 
                    actionWhenFinished: () =>
                    {
                        // 选点完成或取消时恢复 UI
                        Find.ScreenshotModeHandler.Active = false;
                        Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
                    },
                    CompLaunchable.TargeterMouseAttachment, 
                    true, // 音效
                    delegate                          // 每帧执行的逻辑
                    {
                        // 快捷键旋转穿梭机
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

                        // 强制暂停游戏
                        if (!Find.TickManager.Paused)
                        {
                            Find.TickManager.TogglePaused();
                        }

                        // 鼠标右键恢复ui
                        if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
                        {
                            Find.ScreenshotModeHandler.Active = false;
                            Event.current.Use();
                        }

                        // 按 Esc 使用默认中心降落
                        if (KeyBindingDefOf.Cancel.KeyDownEvent)
                        {
                            Event.current.Use();

                            Find.ScreenshotModeHandler.Active = false;

                            // 找到默认中心落点
                            IntVec3 spot;
                            if (!DropCellFinder.TryFindRaidDropCenterClose(out spot, map))
                                spot = DropCellFinder.FindRaidDropCenterDistant(map, true, false);

                            // 执行中心降落
                            TransportersArrivalActionUtility.DropShuttle(transporter, map, spot);
                            Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
                            Find.Targeter.StopTargeting();
                        }
                    });
            }
            else
            {
                // 非穿梭机运输器处理逻辑（普通运输舱）
                var capturedTransporters = new List<ActiveTransporterInfo>(transporters);

                // 弹出落点选择界面
                Find.Targeter.BeginTargeting(
                    TargetingParameters.ForDropPodsDestination(), // 可投放落点参数
                    delegate (LocalTargetInfo x)                   // 确认落点回调
                    {
                        Find.ScreenshotModeHandler.Active = false;
                        TransportersArrivalActionUtility.DropTravellingDropPods(capturedTransporters, x.Cell, map);
                    },
                    null, // 无高亮动作
                    delegate (LocalTargetInfo x) // 验证落点合法性
                    {
                        AcceptanceReport report = CheckDropCellReport(x, map);

                        if (!report.Accepted)
                        {
                            Messages.Message(report.Reason, new LookTargets(x.Cell, map), MessageTypeDefOf.RejectInput, historical: false);
                        }

                        return report.Accepted;
                    },
                    null, 
                    actionWhenFinished: () =>
                    {
                        Find.ScreenshotModeHandler.Active = false;
                        Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
                    },
                    CompLaunchable.TargeterMouseAttachment, 
                    true, 
                    delegate (LocalTargetInfo x) // 每帧逻辑
                    {
                        if (!Find.TickManager.Paused)
                        {
                            Find.TickManager.TogglePaused();
                        }

                        // 右键恢复ui
                        if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
                        {
                            Find.ScreenshotModeHandler.Active = false;
                            Event.current.Use();
                        }

                        // 按 Esc 使用默认中心落点
                        if (KeyBindingDefOf.Cancel.KeyDownEvent)
                        {
                            Event.current.Use();

                            Find.ScreenshotModeHandler.Active = false;

                            IntVec3 spot;
                            if (!DropCellFinder.TryFindRaidDropCenterClose(out spot, map))
                                spot = DropCellFinder.FindRaidDropCenterDistant(map, true, false);

                            TransportersArrivalActionUtility.DropTravellingDropPods(capturedTransporters, spot, map);
                            Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
                            Find.Targeter.StopTargeting();
                        }
                    },
                    null // 无额外更新动作
                );
            }
        }

       
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
                return "CWTL_Disallowedlandingspot".Translate(); // 目标不可落点（允许打穿屋顶）
            }

            return true;
        }

      
        public override bool TryResolveRaidSpawnCenter(IncidentParms parms)
        {
            return true;
        }
    }
}
