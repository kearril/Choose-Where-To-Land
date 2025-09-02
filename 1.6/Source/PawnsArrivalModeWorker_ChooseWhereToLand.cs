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

        // 抵达逻辑
        public override void TravellingTransportersArrived(List<ActiveTransporterInfo> transporters, Map map)
        {

            Find.ScreenshotModeHandler.Active = true;

            //穿梭机降落
            if (transporters.IsShuttle())
            {

                ActiveTransporterInfo transporter = transporters.FirstOrDefault();


                Thing shuttle = transporter.GetShuttle();


                ThingDef shuttleDef = shuttle?.def ?? ThingDefOf.Shuttle;


                shuttleRotation = shuttleDef.defaultPlacingRot;

                // 弹出落点选取界面
                Find.Targeter.BeginTargeting(
                    TargetingParameters.ForCell(),
                    delegate (LocalTargetInfo x)
                    {

                        Find.ScreenshotModeHandler.Active = false;

                        TransportersArrivalActionUtility.DropShuttle(transporter, map, x.Cell, shuttleRotation);
                    },
                    delegate (LocalTargetInfo x)
                    {
                        RoyalTitlePermitWorker_CallShuttle.DrawShuttleGhost(x, map, shuttleDef, shuttleRotation);
                    },
                    delegate (LocalTargetInfo x)
                    {
                        AcceptanceReport report = RoyalTitlePermitWorker_CallShuttle.ShuttleCanLandHere(x, map, shuttleDef, shuttleRotation);


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
                    true,
                    delegate
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
                            Find.TickManager.Pause();
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

                            // 找到默认落点
                            //IntVec3 spot;
                            if (!DropCellFinder.TryFindRaidDropCenterClose(out var spot, map))
                                spot = DropCellFinder.FindRaidDropCenterDistant(map, true, false);

                            // 执行降落
                            TransportersArrivalActionUtility.DropShuttle(transporter, map, spot);
                            Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
                            Find.Targeter.StopTargeting();
                        }
                    });
            }
            else
            {
                // 普通运输舱
                var capturedTransporters = new List<ActiveTransporterInfo>(transporters);


                Find.Targeter.BeginTargeting(
                    TargetingParameters.ForDropPodsDestination(),
                    delegate (LocalTargetInfo x)
                    {
                        Find.ScreenshotModeHandler.Active = false;
                        TransportersArrivalActionUtility.DropTravellingDropPods(capturedTransporters, x.Cell, map);
                    },
                    null,
                    delegate (LocalTargetInfo x)
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
                    delegate (LocalTargetInfo x)
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

                            //IntVec3 spot;
                            if (!DropCellFinder.TryFindRaidDropCenterClose(out var spot, map))
                                spot = DropCellFinder.FindRaidDropCenterDistant(map);

                            TransportersArrivalActionUtility.DropTravellingDropPods(capturedTransporters, spot, map);
                            Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
                            Find.Targeter.StopTargeting();
                        }
                    },
                    null
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
