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

                // 弹出目标点选取界面（用于玩家选择落点）
                Find.Targeter.BeginTargeting(
                    TargetingParameters.ForCell(),    // 目标类型为地图上的 Cell
                    delegate (LocalTargetInfo x)      // 玩家确认目标后调用
                    {
                        // 将穿梭机部署到目标位置
                        TransportersArrivalActionUtility.DropShuttle(transporter, map, x.Cell, shuttleRotation);
                    },
                    delegate (LocalTargetInfo x)      // 预览阶段绘制穿梭机模型
                    {
                        RoyalTitlePermitWorker_CallShuttle.DrawShuttleGhost(x, map, shuttleDef, shuttleRotation);
                    },
                    delegate (LocalTargetInfo x)      // 验证落点是否合法
                    {
                        // 使用已有函数验证落点
                        AcceptanceReport report = RoyalTitlePermitWorker_CallShuttle.ShuttleCanLandHere(x, map, shuttleDef, shuttleRotation);

                        // 如果验证失败，弹出提示
                        if (!report.Accepted)
                        {
                            Messages.Message(report.Reason, new LookTargets(x.Cell, map), MessageTypeDefOf.RejectInput, historical: false);
                        }

                        // 返回是否允许落点
                        return report.Accepted;
                    },
                    caster: null,                    // 无施法者
                    actionWhenFinished: null,       // 无附加操作
                    CompLaunchable.TargeterMouseAttachment, // 鼠标样式为运输仓样式
                    true, // 播放选点音效
                    delegate                          // GUI帧更新逻辑
                    {
                        // 支持旋转操作
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

                        // 强制暂停游戏（便于选点）
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
                        // 执行运输舱投放逻辑
                        TransportersArrivalActionUtility.DropTravellingDropPods(capturedTransporters, x.Cell, map);
                    },
                    null, // highlightAction：无特殊高亮
                    delegate (LocalTargetInfo x) // 验证落点是否合法
                    {
                        // 使用自定义函数验证落点
                        AcceptanceReport report = CheckDropCellReport(x, map);

                        // 如果不合法，手动提示
                        if (!report.Accepted)
                        {
                            Messages.Message(report.Reason, new LookTargets(x.Cell, map), MessageTypeDefOf.RejectInput, historical: false);
                        }

                        // 返回验证结果
                        return report.Accepted;
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

        // 自定义的运输舱落点合法性验证函数，返回带翻译的提示信息
        private static AcceptanceReport CheckDropCellReport(LocalTargetInfo x, Map map)
        {
            if (!x.IsValid)
            {
                return "CWTL_Disallowedlandingspot".Translate(); // 无效坐标
            }
            if (!x.Cell.InBounds(map))
            {
                return "CWTL_Disallowedlandingspot".Translate(); // 越界
            }
            if (x.Cell.Fogged(map))
            {
                return "CWTL_Disallowedlandingspot".Translate(); // 迷雾
            }
            if (!DropCellFinder.CanPhysicallyDropInto(x.Cell, map, canRoofPunch: true))
            {
                return "CWTL_Disallowedlandingspot".Translate(); // 有屋顶或障碍物
            }

            return true;
        }

        // 强制禁用默认突袭落点解析（防止干扰）
        public override bool TryResolveRaidSpawnCenter(IncidentParms parms)
        {
            return true;
        }
    }
}
