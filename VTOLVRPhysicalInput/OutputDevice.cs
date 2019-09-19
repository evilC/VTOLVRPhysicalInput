using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VTOLVRPhysicalInput
{
    public class OutputDevice
    {
        // Latest state of each axis
        //private readonly Dictionary<string, float> _axisStates = new Dictionary<string, float>();
        private readonly Dictionary<
            /* Set Name */ string,
            /* State of axes in sets */ Dictionary<
                /* Axis Name */ string,
                /* Axis State */ float>> _axisSetStates
                    = new Dictionary<string, Dictionary<string, float>>();

        // Has a given set changed since last update?
        private readonly Dictionary<string, bool> _setChanged 
            = new Dictionary<string, bool>();

        // Which Set each axis is in
        private readonly Dictionary<string, string> _axisToSetMappings
            = new Dictionary<string, string>();

        // Functions to call on SendUpdates for each set
        private readonly Dictionary</* Set Name */string, Action<Dictionary<string, float>>> _axisSetDelegates 
            = new Dictionary<string, Action<Dictionary<string, float>>>();

        public OutputDevice AddAxisSet(string setName, List<string> axisNames)
        {
            _axisSetStates.Add(setName, new Dictionary<string, float>());
            _setChanged.Add(setName, false);
            foreach (var axisName in axisNames)
            {
                _axisSetStates[setName].Add(axisName, 0);
                _axisToSetMappings.Add(axisName, setName);
            }

            return this;
        }

        public OutputDevice AddAxisSetDelegate(string setName, Action<Dictionary<string, float>> setDelegate)
        {
            _axisSetDelegates.Add(setName, setDelegate);
            return this;
        }

        public void SetAxis(string axisName, float value)
        {
            var setName = _axisToSetMappings[axisName];
            var setStates = _axisSetStates[setName];
            setStates[axisName] = value;
            _setChanged[setName] = true;
        }

        public void SendUpdates()
        {
            foreach (var setDelegate in _axisSetDelegates)
            {
                var setName = setDelegate.Key;
                if (!_setChanged[setName]) continue;
                var setStates = _axisSetStates[setName];
                setDelegate.Value(setStates);
                _setChanged[setName] = false;
            }
        }

        
    }
}
