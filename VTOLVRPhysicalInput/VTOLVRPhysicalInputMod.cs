﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using SharpDX.DirectInput;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VTOLVRPhysicalInput
{
    [Info("VTOLVR Physical Input", "Allows using of physical input devices in VTOL VR", "https://github.com/evilC/VTOLVRPhysicalInput", "0.0.2")]
    public class VtolVrPhysicalInput : VTOLMOD
    {
        public static DirectInput DiInstance = new DirectInput();
        private VRJoystick _vrJoystick;
        private VRThrottle _vrThrottle;
        private bool _waitingForVrJoystick;
        private bool _pollingEnabled;

        private Dictionary<string, bool> _deviceMapped = new Dictionary<string, bool>(){{"Stick", false}, {"Throttle", false}};

        private readonly Dictionary<string, float> _vrJoystickValues = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase) {{"X", 0}, {"Y", 0}, {"Z", 0}};
        private readonly Dictionary<string, float> _vrJoystickThumb = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase) { { "X", 0 }, { "Y", 0 }, { "Z", 0 } };
        private readonly Dictionary<string, bool> _vrJoystickButtonStates = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase) {{"Trigger", false}, {"Menu", false}};
        private Dictionary<string, bool> _vrJoystickPreviousButtonStates = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase) {{"Trigger", false}, {"Menu", false}};
        private float _vrJoystickTriggerValue;

        private float _vrThrottleValue = 0;
        private readonly Dictionary<string, float> _vrThrottleThumb = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase) { { "X", 0 }, { "Y", 0 }, { "Z", 0 } };
        private readonly Dictionary<string, bool> _vrThrottleButtonStates = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase) { { "Trigger", false }, { "Menu", false } };
        private Dictionary<string, bool> _vrThrottlePreviousButtonStates = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase) { { "Trigger", false }, { "Menu", false } };
        private float _vrThrottleTriggerValue;

        private readonly MappingsDictionary _stickMappings = new MappingsDictionary();

        private OutputDevice _outputStick;
        private OutputDevice _outputThrottle;
        private Dictionary<string, OutputDevice> _outputDevices;

        private static Dictionary<int, Vector3> _povAngleToVector3 = new Dictionary<int, Vector3>
        {
            { -1, new Vector3(0, 0, 0) },       //Center 
            { 0, new Vector3(0, 1, 0) },        // Up
            { 4500, new Vector3(1, 1, 0) },     // Up Right
            { 9000, new Vector3(1, 0, 0) },     // Right
            { 13500, new Vector3(1, -1, 0) },   // Down Right
            { 18000, new Vector3(0, -1, 0) },   // Down
            { 22500, new Vector3(-1, -1, 0) },  // Down Left
            { 27000, new Vector3(-1, 0, 0) },   // Left
            { 31500, new Vector3(-1, 1, 0) }    // Up Left
        };

        /// <summary>
        /// When running in game, called at start
        /// </summary>
        public void Start()
        {
            DontDestroyOnLoad(this.gameObject); // Required, else mod stops working when you leave opening scene and enter ready room
            InitSticks();
            InitUpdates();
        }

        public void InitSticks(bool standaloneTesting = false)
        {
            ProcessSettingsFile(standaloneTesting);
            Log("DBGVIEWCLEAR");
            Log("VTOL VR Mod loaded");
            var diDeviceInstances = DiInstance.GetDevices();

            var foundSticks = new Dictionary<string, Joystick>();

            foreach (var device in diDeviceInstances)
            {
                if (!IsStickType(device)) continue;
                var foundStick = new Joystick(DiInstance, device.InstanceGuid);
                if (foundSticks.ContainsKey(foundStick.Information.ProductName)) continue; // ToDo: Handle duplicate stick names?
                foundSticks.Add(foundStick.Information.ProductName, foundStick);
            }

            foreach (var polledStick in _stickMappings.Sticks)
            {
                if (!foundSticks.ContainsKey(polledStick.Key))
                {
                    ThrowError($"Joystick {polledStick.Value.Stick} not found");
                }
                Log($"Joystick {polledStick.Key} found");
                polledStick.Value.Stick = foundSticks[polledStick.Key];
                polledStick.Value.Stick.Properties.BufferSize = 128;
                polledStick.Value.Stick.Acquire();
            }

            foreach (var device in _deviceMapped)
            {
                Log($"Output Device {device.Key} mapped = {device.Value}");
            }
        }

        public void InitUpdates(bool standaloneTesting = false)
        {
            // Stick Output
            _outputStick = new OutputDevice();
            _outputStick
                .AddAxisSet("StickXyz", new List<string> {"Roll", "Pitch", "Yaw"})
                .AddAxisSet("Touchpad", new List<string> {"X", "Y", "Z"})
                .AddButton("Menu")
                .AddButton("Trigger");

            if (standaloneTesting)
            {
                _outputStick
                    .AddAxisSetDelegate("StickXyz", TestingUpdateStickXyz)
                    .AddAxisSetDelegate("Touchpad", TestingUpdateStickTouchpad)
                    .AddButtonDelegate("Menu", TestingUpdateStickMenuButton)
                    .AddButtonDelegate("Trigger", TestingUpdateStickTriggerButton);
            }
            else
            {
                _outputStick
                    .AddAxisSetDelegate("StickXyz", UpdateStickXyz)
                    .AddAxisSetDelegate("Touchpad", UpdateStickTouchpad)
                    .AddButtonDelegate("Menu", UpdateStickMenuButton)
                    .AddButtonDelegate("Trigger", UpdateStickTriggerButton);
            }

            // Throttle Output
            _outputThrottle = new OutputDevice();
            _outputThrottle
                .AddAxisSet("Throttle", new List<string> {"Throttle"})
                .AddAxisSet("Touchpad", new List<string> {"X", "Y", "Z"})
                .AddAxisSet("Trigger", new List<string> { "Trigger" })
                .AddButton("Menu")
                .AddButton("Trigger");

            if (standaloneTesting)
            {
                _outputThrottle
                    .AddAxisSetDelegate("Throttle", TestingUpdateThrottlePower)
                    .AddAxisSetDelegate("Touchpad", TestingUpdateThrottleTouchpad)
                    .AddAxisSetDelegate("Trigger", TestingUpdateThrottleTriggerAxis);
            }
            else
            {
                _outputThrottle
                    .AddAxisSetDelegate("Throttle", UpdateThrottlePower)
                    .AddAxisSetDelegate("Touchpad", UpdateThrottleTouchpad)
                    .AddAxisSetDelegate("Trigger", UpdateThrottleTriggerAxis);
            }

            _outputDevices = new Dictionary<string, OutputDevice> {{"Stick", _outputStick}, { "Throttle", _outputThrottle} };
        }

        #region Delegates
        #region Stick Delegates
        private void UpdateStickXyz(Dictionary<string, float> values)
        {
            _vrJoystick.OnSetStick.Invoke(new Vector3(values["Pitch"], values["Yaw"], values["Roll"]));
        }

        private void TestingUpdateStickXyz(Dictionary<string, float> values)
        {
            Log($"Update StickXyz: {values.ToDebugString()}");
        }

        private void UpdateStickTouchpad(Dictionary<string, float> values)
        {
            _vrJoystick.OnSetThumbstick.Invoke(new Vector3(values["X"], values["Y"], values["Z"]));
        }

        private void TestingUpdateStickTouchpad(Dictionary<string, float> values)
        {
            Log($"Update StickTouchpad: {values.ToDebugString()}");
        }

        private void UpdateStickMenuButton(bool value)
        {
            if (value) _vrJoystick.OnMenuButtonDown.Invoke();
            else _vrJoystick.OnMenuButtonUp.Invoke();
        }

        private void TestingUpdateStickMenuButton(bool value)
        {
            Log($"Update StickMenuButton: {value}");
        }

        private void UpdateStickTriggerButton(bool value)
        {
            if (value) _vrJoystick.OnTriggerDown.Invoke();
            else _vrJoystick.OnTriggerUp.Invoke();
        }

        private void TestingUpdateStickTriggerButton(bool value)
        {
            Log($"Update StickTriggerButton: {value}");
        }

        #endregion

        #region Throttle Delegates
        private void UpdateThrottlePower(Dictionary<string, float> values)
        {
            _vrThrottle.OnSetThrottle.Invoke(values["Throttle"]);
        }

        private void TestingUpdateThrottlePower(Dictionary<string, float> values)
        {
            Log($"Update ThrottlePower: {values.ToDebugString()}");
        }

        private void UpdateThrottleTouchpad(Dictionary<string, float> values)
        {
            _vrThrottle.OnSetThumbstick.Invoke(new Vector3(values["X"], values["Y"], values["Z"]));
        }

        private void TestingUpdateThrottleTouchpad(Dictionary<string, float> values)
        {
            Log($"Update ThrottleTouchpad: {values.ToDebugString()}");
        }

        private void UpdateThrottleTriggerAxis(Dictionary<string, float> values)
        {
            _vrThrottle.OnTriggerAxis.Invoke(values["Trigger"]);
        }

        private void TestingUpdateThrottleTriggerAxis(Dictionary<string, float> values)
        {
            Log($"Update ThrottleTriggerAxis: {values.ToDebugString()}");
        }

        #endregion
        #endregion

        /// <summary>
        /// Called by Unity each frame
        /// </summary>
        public void Update()
        {
            if (_pollingEnabled)
            {
                if (!VrControlsAvailable() || !InCockpit())
                {
                    _pollingEnabled = false;
                    _waitingForVrJoystick = false;
                    _vrJoystick = null;
                    _vrThrottle = null;
                    Log("Left cockpit");
                }
            }
            else
            {
                if (!VrControlsAvailable() && InCockpit() && !_waitingForVrJoystick)
                {
                    // Entered cockpit
                    Log("Entered cockpit");
                    _waitingForVrJoystick = true;
                    StartCoroutine(FindScripts());
                    return;
                }
            }

            // Take state from physical stick and apply to VRJoystick
            PollSticks();

            if (!VrControlsAvailable()) return;

            SendUpdates();
            /*
            if (_deviceMapped["Stick"])
            {
                _vrJoystick.OnSetStick.Invoke(new Vector3(_vrJoystickValues["X"], _vrJoystickValues["Y"], _vrJoystickValues["Z"]));
                _vrJoystick.OnTriggerAxis.Invoke(_vrJoystickTriggerValue);
                _vrJoystick.OnSetThumbstick.Invoke(new Vector3(_vrJoystickThumb["X"], _vrJoystickThumb["Y"], _vrJoystickThumb["Z"]));
                if (_vrJoystickButtonStates["Trigger"] && !_vrJoystickPreviousButtonStates["Trigger"])
                {
                    _vrJoystick.OnTriggerDown.Invoke();
                }
                else if (!_vrJoystickButtonStates["Trigger"] && _vrJoystickPreviousButtonStates["Trigger"])
                {
                    _vrJoystick.OnTriggerUp.Invoke();
                }

                if (_vrJoystickButtonStates["Menu"] && !_vrJoystickPreviousButtonStates["Menu"])
                {
                    _vrJoystick.OnMenuButtonDown.Invoke();
                }
                else if (!_vrJoystickButtonStates["Menu"] && _vrJoystickPreviousButtonStates["Menu"])
                {
                    _vrJoystick.OnMenuButtonUp.Invoke();
                }

                _vrJoystickPreviousButtonStates = new Dictionary<string, bool>(_vrJoystickButtonStates);
            }

            if (_deviceMapped["Throttle"])
            {
                _vrThrottle.OnSetThrottle.Invoke(_vrThrottleValue);
                _vrThrottle.OnSetThumbstick.Invoke(new Vector3(_vrThrottleThumb["X"], _vrThrottleThumb["Y"], _vrThrottleThumb["Z"]));
                _vrThrottle.OnTriggerAxis.Invoke(_vrThrottleTriggerValue);
                if (_vrThrottleButtonStates["Trigger"] && !_vrThrottlePreviousButtonStates["Trigger"])
                {
                    _vrThrottle.OnTriggerDown.Invoke();
                }
                else if (!_vrThrottleButtonStates["Trigger"] && _vrThrottlePreviousButtonStates["Trigger"])
                {
                    _vrThrottle.OnTriggerUp.Invoke();
                }
                if (_vrThrottleButtonStates["Menu"] && !_vrThrottlePreviousButtonStates["Menu"])
                {
                    _vrThrottle.OnMenuButtonDown.Invoke();
                }
                else if (!_vrThrottleButtonStates["Menu"] && _vrThrottlePreviousButtonStates["Menu"])
                {
                    _vrThrottle.OnMenuButtonUp.Invoke();
                }

                _vrThrottlePreviousButtonStates = new Dictionary<string, bool>(_vrThrottleButtonStates);
            }
            */
        }

        /// <summary>
        /// When playing game, called by Update when we are in the cockpit
        /// When testing via console app, called directly
        /// </summary>
        public void SendUpdates()
        {
            if (_deviceMapped["Stick"])
            {
                _outputStick.SendUpdates();
            }

            if (_deviceMapped["Throttle"])
            {
                _outputThrottle.SendUpdates();
            }
        }

        /// <summary>
        /// When playing game, called by Update when we are in the cockpit
        /// When testing via console app, called directly
        /// </summary>
        public void PollSticks()
        {
            foreach (var mappedStick in _stickMappings.Sticks.Values)
            {
                var data = mappedStick.Stick.GetBufferedData();
                foreach (var state in data)
                {
                    var ov = (int)state.Offset;
                    if (ov <= 28)
                    {
                        // Axes
                        if (mappedStick.AxisToVectorComponentMappings.TryGetValue(state.Offset, out var vectorComponentMapping))
                        {
                            var device = _outputDevices[vectorComponentMapping.OutputDevice];
                            device.SetAxis(
                                vectorComponentMapping.OutputComponent,
                                vectorComponentMapping.OutputSet,
                                ConvertAxisValue(state.Value, vectorComponentMapping.Invert, vectorComponentMapping.MappingRange));
                        }
                    }
                    else if (ov <= 44)
                    {
                        // POV Hats
                        if (mappedStick.PovToTouchpadMappings.TryGetValue(state.Offset, out var touchpadMapping))
                        {
                            //Log(($"PovToTouchpad: POV={state.Offset}, Value={state.Value}, OutputDevice={touchpadMapping.OutputDevice}"));
                            var vec = _povAngleToVector3[state.Value];
                            var device = _outputDevices[touchpadMapping.OutputDevice];
                            device.SetAxis("X", touchpadMapping.OutputSet, vec.x);
                            device.SetAxis("Y", touchpadMapping.OutputSet, vec.y);
                        }
                    }
                    else if (ov <= 175)
                    {
                        // Buttons
                        if (mappedStick.ButtonToButtonMappings.TryGetValue(state.Offset, out var buttonToButtonMapping))
                        {
                            //Log(($"ButtonToButton: Button={state.Offset}, Value={state.Value}, OutputDevice={buttonToButtonMapping.OutputDevice}, OutputButton={buttonToButtonMapping.OutputButton}"));
                            var device = _outputDevices[buttonToButtonMapping.OutputDevice];
                            device.SetButton(buttonToButtonMapping.OutputButton, state.Value == 128);
                        }

                        if (mappedStick.ButtonToVectorComponentMappings.TryGetValue(state.Offset, out var buttonToVectorMapping))
                        {
                            //Log(($"ButtonToVector: Button={state.Offset}, Value={state.Value}, OutputDevice={buttonToVectorMapping.OutputDevice}, Component={buttonToVectorMapping.OutputComponent}"));
                            var device = _outputDevices[buttonToVectorMapping.OutputDevice];
                            device.SetAxis(
                                buttonToVectorMapping.OutputComponent, 
                                buttonToVectorMapping.OutputSet,
                                state.Value == 128 ? buttonToVectorMapping.PressValue : buttonToVectorMapping.ReleaseValue);
                        }

                    }
                }
            }
        }

        /*
        public void PollSticks()
        {
            foreach (var mappedStick in _stickMappings.Sticks.Values)
            {
                var data = mappedStick.Stick.GetBufferedData();
                foreach (var state in data)
                {
                    var ov = (int)state.Offset;
                    if (ov <= 28)
                    {
                        // Axes
                        if (mappedStick.AxisToVectorComponentMappings.TryGetValue(state.Offset, out var vectorComponentMapping))
                        {
                            //Log(($"AxisToVector: Axis={state.Offset}, Value={state.Value}, OutputDevice={vectorComponentMapping.OutputDevice}, Component={vectorComponentMapping.OutputComponent}"));
                            if (vectorComponentMapping.OutputDevice == "Stick")
                            {
                                _vrJoystickValues[vectorComponentMapping.OutputComponent] = ConvertAxisValue(state.Value, vectorComponentMapping.Invert);
                            }
                        }

                        if (mappedStick.AxisToFloatMappings.TryGetValue(state.Offset, out var floatMapping))
                        {
                            if (floatMapping.OutputDevice == "Throttle")
                            {
                                var outputValue = ConvertAxisValue(state.Value, floatMapping.Invert, floatMapping.MappingRange);
                                _vrThrottleValue = outputValue;
                                //Log(($"AxisToFloat: Axis={state.Offset}, Value={state.Value}, OutputDevice={floatMapping.OutputDevice}, OutputValue: {outputValue}"));
                            }
                            
                        }
                    }
                    else if (ov <= 44)
                    {
                        // POV Hats
                        if (mappedStick.PovToTouchpadMappings.TryGetValue(state.Offset, out var touchpadMapping))
                        {
                            //Log(($"PovToTouchpad: POV={state.Offset}, Value={state.Value}, OutputDevice={touchpadMapping.OutputDevice}"));
                            var output = touchpadMapping.OutputDevice == "Stick" ? _vrJoystickThumb : _vrThrottleThumb;
                            output["X"] = 0;
                            output["Y"] = 0;

                            switch (state.Value)
                            {
                                case 0:
                                    // Up
                                    output["Y"] = 1;
                                    break;
                                case 4500:
                                    // Up Right
                                    output["Y"] = 1;
                                    output["X"] = 1;
                                    break;
                                case 9000:
                                    // Right
                                    output["X"] = 1;
                                    break;
                                case 13500:
                                    // Down Right
                                    output["X"] = 1;
                                    output["Y"] = -1;
                                    break;
                                case 18000:
                                    // Down
                                    output["Y"] = -1;
                                    break;
                                case 22500:
                                    //Down Left
                                    output["Y"] = -1;
                                    output["X"] = -1;
                                    break;
                                case 27000:
                                    // Left
                                    output["X"] = -1;
                                    break;
                                case 31500:
                                    // Up Left
                                    output["Y"] = 1;
                                    output["X"] = -1;
                                    break;
                            }
                        }
                    }
                    else if (ov <= 175)
                    {
                        // Buttons
                        if (mappedStick.ButtonToButtonMappings.TryGetValue(state.Offset, out var buttonToButtonMapping))
                        {
                            //Log(($"ButtonToButton: Button={state.Offset}, Value={state.Value}, OutputDevice={buttonToButtonMapping.OutputDevice}, OutputButton={buttonToButtonMapping.OutputButton}"));
                            Dictionary<string, bool> output;
                            output = buttonToButtonMapping.OutputDevice == "Stick" ? _vrJoystickButtonStates : _vrThrottleButtonStates;
                            output[buttonToButtonMapping.OutputButton] = state.Value == 128;
                        }

                        if (mappedStick.ButtonToVectorComponentMappings.TryGetValue(state.Offset, out var buttonToVectorMapping))
                        {
                            //Log(($"ButtonToVector: Button={state.Offset}, Value={state.Value}, OutputDevice={buttonToVectorMapping.OutputDevice}, Component={buttonToVectorMapping.OutputComponent}"));
                            if (buttonToVectorMapping.OutputDevice == "Throttle")
                            {
                                _vrThrottleThumb[buttonToVectorMapping.OutputComponent] = state.Value == 128 ? buttonToVectorMapping.PressValue : 0;
                            }
                        }

                        if (mappedStick.ButtonToFloatMappings.TryGetValue(state.Offset, out var buttonToFloatMapping))
                        {
                            //Log(($"ButtonToFloat: Button={state.Offset}, Value={state.Value}, OutputDevice={buttonToFloatMapping.OutputDevice}"));
                            if (buttonToFloatMapping.OutputDevice == "Stick")
                            {
                                _vrJoystickTriggerValue = state.Value == 128 ? buttonToFloatMapping.PressValue : buttonToFloatMapping.ReleaseValue;
                            }
                            else if (buttonToFloatMapping.OutputDevice == "Throttle")
                            {
                                _vrThrottleTriggerValue = state.Value == 128 ? buttonToFloatMapping.PressValue : buttonToFloatMapping.ReleaseValue;
                            }
                        }
                    }
                }
            }
        }
        */

        private IEnumerator FindScripts()
        {
            while (_vrJoystick == null)
            {
                //Searches the whole game scene for the script, there should only be one so its fine
                _vrJoystick = FindObjectOfType<VRJoystick>();
                _vrThrottle = FindObjectOfType<VRThrottle>();
                if (VrControlsAvailable()) continue;
                Log("Waiting for VRJoystick...");
                yield return new WaitForSeconds(1);
            }
            _waitingForVrJoystick = false;
            _pollingEnabled = true;
            Log("Got VRJoystick");
        }

        private static float ConvertAxisValue(int value, bool invert, string mappingRange = "Full")
        {
            float retVal;
            if (value == 65535) retVal = 1;
            else retVal = (((float)value / 32767) - 1);
            if (invert) retVal *= -1;
            if (mappingRange == "High")
            {
                retVal /= 2;
                retVal += 0.5f;
            }
            else if (mappingRange == "Low")
            {
                retVal /= 2;
                retVal -= 0.5f;
            }
            return retVal;
        }

        private static bool InCockpit()
        {
            return SceneManager.GetActiveScene().buildIndex == 7 || SceneManager.GetActiveScene().buildIndex == 11;
        }

        private bool VrControlsAvailable()
        {
            return _vrJoystick != null && _vrThrottle != null;
        }

        public static bool IsStickType(DeviceInstance deviceInstance)
        {
            return deviceInstance.Type == SharpDX.DirectInput.DeviceType.Joystick
                   || deviceInstance.Type == SharpDX.DirectInput.DeviceType.Gamepad
                   || deviceInstance.Type == SharpDX.DirectInput.DeviceType.FirstPerson
                   || deviceInstance.Type == SharpDX.DirectInput.DeviceType.Flight
                   || deviceInstance.Type == SharpDX.DirectInput.DeviceType.Driving
                   || deviceInstance.Type == SharpDX.DirectInput.DeviceType.Supplemental;
        }

        public void ProcessSettingsFile(bool standaloneTesting = false)
        {
            string settingsFile;
            if (standaloneTesting)
            {
                settingsFile = Path.Combine(Directory.GetCurrentDirectory(), @"VTOLVRPhysicalInputSettings.xml");
            }
            else
            {
                settingsFile = Path.Combine(Directory.GetCurrentDirectory(), @"VTOLVR_ModLoader\Mods\VTOLVRPhysicalInputSettings.xml");
            }
            
            if (File.Exists(settingsFile))
            {
                var deserializer = new XmlSerializer(typeof(Mappings));
                TextReader reader = new StreamReader(settingsFile);
                var obj = deserializer.Deserialize(reader);
                var stickMappings = (Mappings)obj;
                reader.Close();

                // Build Dictionary
                // ToDo: How to do this as part of XML Deserialization?
                foreach (var stick in stickMappings.MappingsList)
                {
                    if (!_stickMappings.Sticks.ContainsKey(stick.StickName))
                    {
                        _stickMappings.Sticks.Add(stick.StickName, new StickMappings(){StickName = stick.StickName});
                    }

                    var mapping = _stickMappings.Sticks[stick.StickName];

                    foreach (var axisToVectorComponentMapping in stick.AxisToVectorComponentMappings)
                    {
                        mapping.AxisToVectorComponentMappings.Add(JoystickOffsetFromName(axisToVectorComponentMapping.InputAxis), axisToVectorComponentMapping);
                        _deviceMapped[axisToVectorComponentMapping.OutputDevice] = true;
                    }

                    //foreach (var axisToFloatMapping in stick.AxisToFloatMappings)
                    //{
                    //    mapping.AxisToFloatMappings.Add(JoystickOffsetFromName(axisToFloatMapping.InputAxis), axisToFloatMapping);
                    //    _deviceMapped[axisToFloatMapping.OutputDevice] = true;
                    //}

                    foreach (var buttonToVectorComponentMapping in stick.ButtonToVectorComponentMappings)
                    {
                        mapping.ButtonToVectorComponentMappings.Add(JoystickOffsetFromName(ButtonNameFromIndex(buttonToVectorComponentMapping.InputButton)), buttonToVectorComponentMapping);
                        _deviceMapped[buttonToVectorComponentMapping.OutputDevice] = true;
                    }

                    foreach (var buttonToButtonMapping in stick.ButtonToButtonMappings)
                    {
                        mapping.ButtonToButtonMappings.Add(JoystickOffsetFromName(ButtonNameFromIndex(buttonToButtonMapping.InputButton)), buttonToButtonMapping);
                        _deviceMapped[buttonToButtonMapping.OutputDevice] = true;
                    }

                    //foreach (var buttonToFloatMapping in stick.ButtonToFloatMappings)
                    //{
                    //    mapping.ButtonToFloatMappings.Add(JoystickOffsetFromName(ButtonNameFromIndex(buttonToFloatMapping.InputButton)), buttonToFloatMapping);
                    //    _deviceMapped[buttonToFloatMapping.OutputDevice] = true;
                    //}

                    foreach (var povToTouchpadMapping in stick.PovToTouchpadMappings)
                    {
                        mapping.PovToTouchpadMappings.Add(JoystickOffsetFromName(PovNameFromIndex(povToTouchpadMapping.InputPov)), povToTouchpadMapping);
                        _deviceMapped[povToTouchpadMapping.OutputDevice] = true;
                    }
                }
            }
            else
            {
                Log($"{settingsFile} not found");
                throw new Exception($"{settingsFile} not found");
            }
        }

        private string ButtonNameFromIndex(int index)
        {
            return "Buttons" + (index - 1);
        }

        private string PovNameFromIndex(int index)
        {
            return "PointOfViewControllers" + (index - 1);
        }

        private JoystickOffset JoystickOffsetFromName(string n)
        {
            return (JoystickOffset)Enum.Parse(typeof(JoystickOffset), n);
        }

        private void ThrowError(string text)
        {
            Log(text);
            throw new Exception(text);
        }

        private void Log(string text)
        {
            System.Diagnostics.Debug.WriteLine($"PhysicalStickMod| {text}");
        }
    }
}
