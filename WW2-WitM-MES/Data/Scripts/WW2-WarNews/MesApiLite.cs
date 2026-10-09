using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace WW2WitM.WarNewsSystem
{
    // Minimal client for the MES mod API: only the two endpoints War News uses.
    // MES sends its API dictionary to channel 1521905890 on the server in BeforeStart
    // (MES API/LocalAPI.cs SendApiToMods, Core/MES_SessionCore.cs); keys from LocalAPI.GetApiDictionary.
    public class MesApiLite
    {
        private const long MesModId = 1521905890;

        public bool Ready;
        public bool HasWatcher { get { return triggerWatcher != null; } }
        public bool HasCustomActions { get { return registerCustomAction != null; } }
        private Action<bool, Action<IMyRemoteControl, string, string, IMyEntity, Vector3D>> triggerWatcher;
        private Action<bool, string, Action<object[]>> registerCustomAction;

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

            Ready = triggerWatcher != null || registerCustomAction != null;
        }
    }
}
