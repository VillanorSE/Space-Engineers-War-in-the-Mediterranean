using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace WW2WitM.WarNewsSystem
{
    // Minimal client for the MES mod API: the endpoints War News, the refresh command and supply use.
    // MES sends its API dictionary to channel 1521905890 on the server in BeforeStart
    // (MES API/LocalAPI.cs SendApiToMods, Core/MES_SessionCore.cs); keys from LocalAPI.GetApiDictionary.
    public class MesApiLite
    {
        private const long MesModId = 1521905890;

        public bool Ready;
        public bool HasWatcher { get { return triggerWatcher != null; } }
        public bool HasCustomActions { get { return registerCustomAction != null; } }
        public bool HasRefreshApi { get { return chatCommand != null && processStatics != null && sendBehaviorCommand != null; } }
        public bool HasSpawnApi { get { return registerSpawnAction != null && registerDespawnWatcher != null; } }
        private Action<bool, Action<IMyRemoteControl, string, string, IMyEntity, Vector3D>> triggerWatcher;
        private Action<bool, string, Action<object[]>> registerCustomAction;
        private Action<string, MatrixD, long, ulong> chatCommand;
        private Action<Vector3D> processStatics;
        private Action<List<string>, Vector3D, string, double, long> sendBehaviorCommand;
        private Action<Action<IMyCubeGrid>, bool> registerSpawnAction;
        private Func<IMyCubeGrid, Action<IMyCubeGrid, string>, bool> registerDespawnWatcher;

        public MesApiLite()
        {
            MyAPIGateway.Utilities.RegisterMessageHandler(MesModId, OnMessage);
        }

        public void Unload()
        {
            MyAPIGateway.Utilities.UnregisterMessageHandler(MesModId, OnMessage);
        }

        // Called by MES for every behavior action that runs: (Remote Control, trigger id, action id, target, waypoint).
        public void BehaviorTriggerActivationWatcher(bool register, Action<IMyRemoteControl, string, string, IMyEntity, Vector3D> action)
        {
            if (triggerWatcher != null) triggerWatcher(register, action);
        }

        // Custom actions are run by MES Event actions ([ActivateCustomAction:true] [CustomActionName:...]).
        // MES quirk: registering a name that already exists removes it, so always unregister on unload.
        public void RegisterCustomAction(bool register, string name, Action<object[]> action)
        {
            if (registerCustomAction != null) registerCustomAction(register, name, action);
        }

        // Runs a /MES chat command on the server as admin (MES ChatManager.ChatFromApi forces IsAdmin): check permissions first.
        public void ChatCommand(string message, MatrixD position, long identityId, ulong steamId)
        {
            if (chatCommand != null) chatCommand(message, position, identityId, steamId);
        }

        // Attempts every pending static encounter whose TriggerRadius contains the position, now and without a player (MES LocalAPI).
        public void ProcessStaticEncountersAtLocation(Vector3D position)
        {
            if (processStatics != null) processStatics(position);
        }

        // Sends a behavior command from a position, as an MES Event would (no antenna needed). Empty code / radius < 0 use the profile's.
        public void SendBehaviorCommand(List<string> commandProfileIds, Vector3D origin, string code, double radius, long ownerId)
        {
            if (sendBehaviorCommand != null) sendBehaviorCommand(commandProfileIds, origin, code, radius, ownerId);
        }

        // Called once per spawned MES grid, after its attributes are applied (MES NpcData AnnounceSpawnInApi, saved per grid).
        public void RegisterSuccessfulSpawnAction(Action<IMyCubeGrid> action, bool register)
        {
            if (registerSpawnAction != null) registerSpawnAction(action, register);
        }

        // Called with (grid, despawn source) when MES removes the grid (Tasks/GridCleanup.cs). Not saved: register again after a load.
        public bool RegisterDespawnWatcher(IMyCubeGrid grid, Action<IMyCubeGrid, string> action)
        {
            return registerDespawnWatcher != null && registerDespawnWatcher(grid, action);
        }

        private void OnMessage(object data)
        {
            var dict = data as Dictionary<string, Delegate>;
            if (dict == null)
                return;

            Delegate d;
            if (dict.TryGetValue("BehaviorTriggerActivationWatcher", out d))
                triggerWatcher = d as Action<bool, Action<IMyRemoteControl, string, string, IMyEntity, Vector3D>>;
            if (dict.TryGetValue("RegisterCustomAction", out d))
                registerCustomAction = d as Action<bool, string, Action<object[]>>;
            if (dict.TryGetValue("ChatCommand", out d))
                chatCommand = d as Action<string, MatrixD, long, ulong>;
            if (dict.TryGetValue("ProcessStaticEncountersAtLocation", out d))
                processStatics = d as Action<Vector3D>;
            if (dict.TryGetValue("SendBehaviorCommand", out d))
                sendBehaviorCommand = d as Action<List<string>, Vector3D, string, double, long>;
            if (dict.TryGetValue("RegisterSuccessfulSpawnAction", out d))
                registerSpawnAction = d as Action<Action<IMyCubeGrid>, bool>;
            if (dict.TryGetValue("RegisterDespawnWatcher", out d))
                registerDespawnWatcher = d as Func<IMyCubeGrid, Action<IMyCubeGrid, string>, bool>;

            Ready = triggerWatcher != null || registerCustomAction != null;
        }
    }
}
