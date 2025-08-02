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
       
        private static Rot4 shuttleRotation = Rot4.East;

        
        public override void Arrive(List<Pawn> pawns, IncidentParms parms)
        {

        }

        // 当运输器抵达地图时调用
        public override void TravellingTransportersArrived(List<ActiveTransporterInfo> transporters, Map map)
        {
            // 如果是穿梭机
            if (transporters.IsShuttle())
            {
                // 获取第一个运输器信息
                ActiveTransporterInfo transporter = transporters.FirstOrDefault();

                // 获取穿梭机本体
                Thing shuttle = transporter.GetShuttle();

                // 获取穿梭机的定义
                ThingDef shuttleDef = shuttle?.def ?? ThingDefOf.Shuttle;

                // 读取穿梭机的默认放置朝向
                shuttleRotation = shuttleDef.defaultPlacingRot;

                // 弹出目标点选取界面
                Find.Targeter.BeginTargeting(
                    TargetingParameters.ForCell(),    // 目标类型：地图上的cell
                    delegate (LocalTargetInfo x)      // 玩家确认目标后调用
                    {
                        TransportersArrivalActionUtility.DropShuttle(transporter, map, x.Cell, shuttleRotation);
                    },
                    delegate (LocalTargetInfo x)      // 预览阶段绘制穿梭机模型
                    {
                        RoyalTitlePermitWorker_CallShuttle.DrawShuttleGhost(x, map, shuttleDef, shuttleRotation);
                    },
                    delegate (LocalTargetInfo x)      // 验证落点是否合法
                    {
                        AcceptanceReport report = RoyalTitlePermitWorker_CallShuttle.ShuttleCanLandHere(x, map, shuttleDef, shuttleRotation);
                        if (!report.Accepted)
                        {
                            Messages.Message(report.Reason, new LookTargets(x.Cell, map), MessageTypeDefOf.RejectInput, historical: false);
                        }
                        return report.Accepted;
                    },
                    caster: null,                    
                    actionWhenFinished: null,       
                    mouseAttachment: CompLaunchable.TargeterMouseAttachment, // 鼠标样式
                    playSoundOnAction: true,         // 播放点击音效
                    delegate                          // GUI帧更新逻辑
                    {
                        // 支持旋转
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

                        // 禁止右键取消
                        if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
                        {
                            Event.current.Use();
                        }

                        // 禁止 Esc 取消
                        if (KeyBindingDefOf.Cancel.KeyDownEvent)
                        {
                            Event.current.Use();
                        }
                    });
            }
            else
            {
                // 非穿梭机路径，普通运输舱处理逻辑
                var capturedTransporters = new List<ActiveTransporterInfo>(transporters);

                // 弹出目标点选择界面
                Find.Targeter.BeginTargeting(
                    TargetingParameters.ForDropPodsDestination(), // 针对运输舱落点的参数
                    delegate (LocalTargetInfo x)                   // 落点确认后执行
                    {
                        TransportersArrivalActionUtility.DropTravellingDropPods(capturedTransporters, x.Cell, map);
                    },
                    null, // highlightAction：无特殊高亮
                    delegate (LocalTargetInfo x) // 验证逻辑
                    {
                        if (!x.IsValid)
                            return false;

                        IntVec3 cell = x.Cell;

                        if (!cell.InBounds(map))          // 超出地图边界
                            return false;

                        if (cell.Fogged(map))             // 处于迷雾中
                            return false;

                        if (!DropCellFinder.CanPhysicallyDropInto(cell, map, canRoofPunch: true)) // 无法强制砸落
                            return false;

                        return true; // 合法
                    },
                    null, // caster
                    null, // actionWhenFinished
                    CompLaunchable.TargeterMouseAttachment, // 鼠标样式
                    true, // 播放选点音效
                    delegate (LocalTargetInfo x) // GUI更新回调
                    {
                        // 强制暂停游戏
                        if (!Find.TickManager.Paused)
                        {
                            Find.TickManager.TogglePaused();
                        }

                        // 禁止右键
                        if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
                        {
                            Event.current.Use();
                        }

                        // 禁止Esc
                        if (KeyBindingDefOf.Cancel.KeyDownEvent)
                        {
                            Event.current.Use();
                        }
                    },
                    null // onUpdateAction
                );
            }
        }

        
        public override bool TryResolveRaidSpawnCenter(IncidentParms parms)
        {
            return true;
        }
    }
}
