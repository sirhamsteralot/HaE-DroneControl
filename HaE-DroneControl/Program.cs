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
        public string controllerName { get { return (string)iniSerializer.GetValue("controllerName"); } }

        EntityTracking_Module entityTracking;
        AutoPilot autoPilot;
        IngameTime time;
        Scheduler scheduler;
        InterGridComms comms;

        INISerializer iniSerializer;

        IMyShipController controller;

        bool loaded = false;
        GridTerminalSystemUtils GTS;

        public Program()
        {
            scheduler = new Scheduler();
            scheduler.AddTask(Init());
            Runtime.UpdateFrequency = UpdateFrequency.Update1 | UpdateFrequency.Update100; ;
        }

        public IEnumerator<bool> Init()
        {
            iniSerializer = new INISerializer("DroneControl");
            iniSerializer.AddValue("controllerName", x => x, "Controller");

            if (Me.CustomData == "")
            {
                string temp = Me.CustomData;
                iniSerializer.FirstSerialization(ref temp);
                Me.CustomData = temp;
            }
            else
            {
                iniSerializer.DeSerialize(Me.CustomData);
            }
            yield return true;

            time = new IngameTime();
            GTS = new GridTerminalSystemUtils(Me, GridTerminalSystem);
            yield return true;

            controller = GTS.GetBlockWithNameOnGrid(controllerName) as IMyShipController;
            yield return true;

            entityTracking = new EntityTracking_Module(GTS, controller, null, EntityTracking_Module.refExpSettings.Turret);
            autoPilot = new AutoPilot(GTS, controller, time, entityTracking);
            yield return true;

            comms = new InterGridComms(IGC, time, Me);
            comms.P = this;
            yield return true;
            RegisterCommands();
            loaded = true;
        }

        public void Save()
        {

        }

        public void Main(string argument, UpdateType updateSource)
        {
            if ((updateSource & UpdateType.Update1) != 0)
                Update1();
            if ((updateSource & UpdateType.Update100) != 0)
                Update100();
        }

        public void Update1()
        {
            if (loaded)
            {
                time.Tick(Runtime.TimeSinceLastRun);
                comms.CheckPendingMessages();
                autoPilot.Main();
            }
            scheduler.Main();
        }

        public void Update100()
        {
            if (loaded)
                UpdateStatus();
        }

        public void RegisterCommands()
        {
            comms.RegisterCommand("FlyToPoint", FlyToPoint);
        }

        #region commands
        private void FlyToPoint(object point)
        {
            Vector3D vPoint = (Vector3D)point;

            autoPilot.TravelToPosition(vPoint, false);
        }
        #endregion
        #region send
        int tickcounter;
        private void UpdateStatus()
        {
            if ((tickcounter++ % 10) != 0)
                return;

            Echo($"Sending Status Message: {time.Time.TotalSeconds}");
            comms.comms.SendUnicastMessage(comms.serverEndPoint, "UpdateStatus", (int)autoPilot.currentMode);
        }
        #endregion
    }
}