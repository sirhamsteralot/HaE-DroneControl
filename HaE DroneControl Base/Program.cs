using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        IngameTime time;
        DroneManagement droneManagement;

        public Program()
        {
            time = new IngameTime();
            droneManagement = new DroneManagement(time, IGC);
            droneManagement.P = this;

            Runtime.UpdateFrequency = UpdateFrequency.Update1 | UpdateFrequency.Update100;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if ((updateSource & UpdateType.Update1) != 0)
                Update1();
            if ((updateSource & UpdateType.Update100) != 0)
                Update100();

            if ((updateSource & UpdateType.Terminal | UpdateType.Trigger) != 0)
                HandleUserCommands(argument);
        }

        public void HandleUserCommands(string arg)
        {
            switch (arg)
            {
                case "Test":
                    Vector3D targetpos = Vector3D.Zero;
                    droneManagement.SendDroneToPosition(targetpos);
                    break;
            }
        }

        public void Update1()
        {
            droneManagement.CheckPendingMessages();
            time.Tick(Runtime.TimeSinceLastRun);
        }

        int tickcounter;
        public void Update100()
        {
            if ((tickcounter++ % 10) != 0)
                return;

            droneManagement.PingKeepAlive();
        }
    }
}