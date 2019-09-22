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
        private readonly Dictionary<string, bool> _axisSetChanged 
            = new Dictionary<string, bool>();

        // Functions to call on SendUpdates for each set of Axes
        private readonly Dictionary</* Set Name */string, Action<Dictionary<string, float>>> _axisSetDelegates 
            = new Dictionary<string, Action<Dictionary<string, float>>>();

        private readonly Dictionary<string, bool> _buttonStates
            = new Dictionary<string, bool>();

        private readonly Dictionary<string, bool> _buttonChanged
            = new Dictionary<string, bool>();

        // Functions to call on SendUpdates for each Button
        private readonly Dictionary<string, Action<bool>> _buttonDelegates
            = new Dictionary<string, Action<bool>>();

        public OutputDevice AddAxisSet(string setName, List<string> axisNames)
        {
            _axisSetStates.Add(setName, new Dictionary<string, float>());
            //_axisSetChanged.Add(setName, false);
            _axisSetChanged.Add(setName, true); // Enable Axis updates every frame for now
            foreach (var axisName in axisNames)
            {
                _axisSetStates[setName].Add(axisName, 0);
            }

            return this;
        }

        public OutputDevice AddAxisSetDelegate(string setName, Action<Dictionary<string, float>> setDelegate)
        {
            _axisSetDelegates.Add(setName, setDelegate);
            return this;
        }

        public OutputDevice AddButton(string name)
        {
            _buttonStates.Add(name, false);
            _buttonChanged.Add(name, false);
            return this;
        }

        public OutputDevice AddButtonDelegate(string name, Action<bool> buttonDelegate)
        {
            _buttonDelegates.Add(name, buttonDelegate);
            return this;
        }

        public void SetAxis(string axisName, string setName, float value)
        {
            var setStates = _axisSetStates[setName];
            setStates[axisName] = value;
            //_axisSetChanged[setName] = true;
        }

        public void SetButton(string buttonName, bool value)
        {
            _buttonStates[buttonName] = value;
            _buttonChanged[buttonName] = true;
        }

        public void SendUpdates()
        {
            foreach (var setDelegate in _axisSetDelegates)
            {
                var setName = setDelegate.Key;
                if (!_axisSetChanged[setName]) continue;
                var setStates = _axisSetStates[setName];
                setDelegate.Value(setStates);
                //_axisSetChanged[setName] = false;
            }

            foreach (var buttonDelegate in _buttonDelegates)
            {
                if (!_buttonChanged[buttonDelegate.Key]) continue;
                buttonDelegate.Value(_buttonStates[buttonDelegate.Key]);
                _buttonChanged[buttonDelegate.Key] = false;
            }
        }

        
    }
}
