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
        public class InterGridComms
        {
            public Program P;

            public IngameTime time;

            public IMyProgrammableBlock me;

            public IMyIntergridCommunicationSystem comms;
            public IMyUnicastListener uniComms => comms.UnicastListener;
            public long MyId => comms.Me;

            public IMyBroadcastListener keepaliveChann;

            public long serverEndPoint;
            public TimeSpan lastKeepAlivePingTime;
            public Action endpointChangedCallback;

            public Dictionary<string, Action<object>> commands;

            public InterGridComms(IMyIntergridCommunicationSystem comms, IngameTime time, IMyProgrammableBlock me)
            {
                commands = new Dictionary<string, Action<object>>();
                keepaliveChann = comms.RegisterBroadcastListener("ServerKeepAlive");

                this.comms = comms;
                this.time = time;
                this.me = me;
            }

            public void CheckPendingMessages()
            {
                while (uniComms.HasPendingMessage)
                {
                    var message = uniComms.AcceptMessage();
                    ProcessUniCommsMessage(message);
                }

                while (keepaliveChann.HasPendingMessage)
                {
                    var message = keepaliveChann.AcceptMessage();
                    ServerKeepAlive((long)message.Data);
                }
            }

            public void RegisterCommand(string tag, Action<object> action)
            {
                commands[tag] = action;
            }

            private void ProcessUniCommsMessage(MyIGCMessage message)
            {
                P.Echo($"Processing UNICOMMS: {message.Tag}");

                if (commands.ContainsKey(message.Tag))
                    commands[message.Tag]?.Invoke(message.Data);
            }

            #region systemCommands
            private void ServerKeepAlive(long id)
            {
                P.Echo($"Processing SERVERKEEPALIVE: {id}");

                if (id != serverEndPoint)
                {
                    endpointChangedCallback?.Invoke();
                    serverEndPoint = id;
                }

                lastKeepAlivePingTime = time.Time;
                comms.SendUnicastMessage(id, "ReturnKeepAlive", MyId);
                comms.SendUnicastMessage(id, "ReturnPosition", me.GetPosition());
            }
            #endregion
        }
    }
}
