using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
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

        public override void TravellingTransportersArrived(List<ActiveTransporterInfo> transporters, Map map)
        {
            Find.ScreenshotModeHandler.Active = true;

            if (transporters.IsShuttle())
            {
                ActiveTransporterInfo transporter = transporters.FirstOrDefault();
                Thing shuttle = transporter.GetShuttle();
                ThingDef shuttleDef = shuttle?.def ?? ThingDefOf.Shuttle;
                shuttleRotation = shuttleDef.defaultPlacingRot;

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
                        Find.ScreenshotModeHandler.Active = false;
                        Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
                    },
                    CompLaunchable.TargeterMouseAttachment,
                    true,
                    delegate
                    {
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

                        if (!Find.TickManager.Paused)
                        {
                            Find.TickManager.Pause();
                        }

                        if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
                        {
                            Find.ScreenshotModeHandler.Active = false;
                            Event.current.Use();
                        }

                        if (KeyBindingDefOf.Cancel.KeyDownEvent)
                        {
                            Event.current.Use();
                            Find.ScreenshotModeHandler.Active = false;

                            if (!DropCellFinder.TryFindRaidDropCenterClose(out var spot, map))
                                spot = DropCellFinder.FindRaidDropCenterDistant(map, true, false);

                            TransportersArrivalActionUtility.DropShuttle(transporter, map, spot);
                            Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
                            Find.Targeter.StopTargeting();
                        }
                    });
            }
            else
            {
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
                            Find.TickManager.Pause();
                        }

                        if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
                        {
                            Find.ScreenshotModeHandler.Active = false;
                            Event.current.Use();
                        }

                        if (KeyBindingDefOf.Cancel.KeyDownEvent)
                        {
                            Event.current.Use();
                            Find.ScreenshotModeHandler.Active = false;

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
                return "CWTL_Disallowedlandingspot".Translate();
            }
            if (!x.Cell.InBounds(map))
            {
                return "CWTL_Disallowedlandingspot".Translate();
            }
            if (x.Cell.Fogged(map))
            {
                return "CWTL_Disallowedlandingspot".Translate();
            }
            if (!DropCellFinder.CanPhysicallyDropInto(x.Cell, map, canRoofPunch: true))
            {
                return "CWTL_Disallowedlandingspot".Translate();
            }
            return true;
        }

        public override bool TryResolveRaidSpawnCenter(IncidentParms parms)
        {
            return true;
        }
    }
}
