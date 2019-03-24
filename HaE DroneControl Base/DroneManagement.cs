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
    partial class Program
    {
        public class DroneManagement
        {
            public Program P;

            public IngameTime time;

            public IMyIntergridCommunicationSystem comms;
            public IMyUnicastListener uniComms => comms.UnicastListener;
            public long MyId => comms.Me;

            public Dictionary<long, Drone> drones;
            public Dictionary<string, Action<object>> commands;

            public DroneManagement(IngameTime time, IMyIntergridCommunicationSystem comms)
            {
                drones = new Dictionary<long, Drone>();
                commands = new Dictionary<string, Action<object>>();

                this.time = time;
                this.comms = comms;
            }

            public void PingKeepAlive()
            {
                P?.Echo($"Pinging drones: {time.Time.TotalSeconds}");
                comms.SendBroadcastMessage("ServerKeepAlive", comms.Me);
            }

            public void CheckPendingMessages()
            {
                while (uniComms.HasPendingMessage)
                {
                    var message = uniComms.AcceptMessage();
                    ProcessMessage(message);
                }
            }

            public void RegisterCommand(string tag, Action<object> action)
            {
                commands[tag] = action;
            }

            private void ProcessMessage(MyIGCMessage message)
            {
                P?.Echo($"Message Received: {message.Tag}");

                switch (message.Tag)
                {
                    case "ReturnKeepAlive":
                        ReturnKeepAlive(message);
                        break;
                    case "ReturnPosition":
                        ReturnPosition(message);
                        break;
                    case "UpdateStatus":
                        UpdateStatus(message);
                        break;
                }

                if (commands.ContainsKey(message.Tag))
                    commands[message.Tag]?.Invoke(message.Data);
            }

            public void SendDroneToPosition(Vector3D position)
            {
                foreach (var drone in drones.Values)
                {
                    if (drone.autopilotStatus == AutoPilot.AutopilotMode.Idle)
                    {
                        P?.Echo($"Sending Drone {drone.id} to position: {position}");
                        drone.FlyToPoint(position);
                        return;
                    }
                }
            }

            private void UpdateStatus(MyIGCMessage message)
            {
                long droneID = message.Source;
                Drone drone;
                if (drones.TryGetValue(droneID, out drone))
                {
                    drone.UpdateStatus(message.Data);
                }
            }

            private void ReturnPosition(MyIGCMessage message)
            {
                long droneID = message.Source;
                Drone drone;
                if (drones.TryGetValue(droneID, out drone))
                {
                    drone.ReturnPosition((Vector3D)message.Data);
                }
            }

            private void ReturnKeepAlive(MyIGCMessage message)
            {
                long droneID = (long)message.Data;
                Drone drone;
                if (drones.TryGetValue(droneID, out drone))
                {
                    drone.ReturnKeepAlive(droneID, time.Time);
                }
                else
                {
                    drone = new Drone(droneID, this);
                    drones[droneID] = drone;
                }
            }

            public class Drone
            {
                public enum CurrentTask
                {
                    Idle,
                    Travelling,
                }

                public DroneManagement parent;

                public long id;
                public TimeSpan lastKeepAlivePingTime;
                public Vector3D position;
                public AutoPilot.AutopilotMode autopilotStatus = AutoPilot.AutopilotMode.Idle;

                public Drone(long id, DroneManagement parent)
                {
                    this.id = id;
                    this.parent = parent;
                }

                public void ReturnKeepAlive(long id, TimeSpan time)
                {
                    lastKeepAlivePingTime = time;
                }

                public void ReturnPosition(Vector3D position)
                {
                    this.position = position;
                }

                public void UpdateStatus(object status)
                {
                    autopilotStatus = (AutoPilot.AutopilotMode)status;
                }

                public void FlyToPoint(Vector3D position)
                {
                    parent.P?.Echo($"Sending command: FlyToPoint");
                    parent.comms.SendUnicastMessage(id, "FlyToPoint", position);
                }
            }
        }
    }
}
