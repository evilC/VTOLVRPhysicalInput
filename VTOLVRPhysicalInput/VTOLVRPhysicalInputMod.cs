using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;
using Oculus.Platform.Models;
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

        private readonly Dictionary<string, float> _vrJoystickValues = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase) {{"X", 0}, {"Y", 0}, {"Z", 0}};
        private float _vrJoystickTriggerValue;

        private float _vrThrottleValue = 0;
        private readonly Dictionary<string, float> _vrThrottleThumb = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase) { { "X", 0 }, { "Y", 0 }, { "Z", 0 } };
        private float _vrThrottleTriggerValue;
        private readonly MappingsDictionary _stickMappings = new MappingsDictionary();

        public void Start()
        {
            DontDestroyOnLoad(this.gameObject); // Required, else mod stops working when you leave opening scene and enter ready room
            InitSticks();
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

        }

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

            _vrJoystick.OnSetStick.Invoke(new Vector3(_vrJoystickValues["X"], _vrJoystickValues["Y"], _vrJoystickValues["Z"]));
            _vrJoystick.OnTriggerAxis.Invoke(_vrJoystickTriggerValue);
            
            _vrThrottle.OnSetThrottle.Invoke(_vrThrottleValue);
            _vrThrottle.OnSetThumbstick.Invoke(new Vector3(_vrThrottleThumb["X"], _vrThrottleThumb["Y"], _vrThrottleThumb["Z"]));
            _vrThrottle.OnTriggerAxis.Invoke(_vrThrottleTriggerValue);
        }

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
                            Log(($"AxisToVector: Axis={state.Offset}, Value={state.Value}, OutputDevice={vectorComponentMapping.OutputDevice}, Component={vectorComponentMapping.OutputComponent}"));
                            if (vectorComponentMapping.OutputDevice == "Stick")
                            {
                                _vrJoystickValues[vectorComponentMapping.OutputComponent] = ConvertAxisValue(state.Value, vectorComponentMapping.Invert);
                            }
                        }

                        if (mappedStick.AxisToFloatMappings.TryGetValue(state.Offset, out var floatMapping))
                        {
                            Log(($"AxisToFloat: Axis={state.Offset}, Value={state.Value}, OutputDevice={floatMapping.OutputDevice}"));
                            if (floatMapping.OutputDevice == "Throttle")
                            {
                                _vrThrottleValue = ConvertAxisValue(state.Value, floatMapping.Invert);
                            }
                        }
                    }
                    else if (ov <= 44)
                    {
                        // Hats
                    }
                    else if (ov <= 175)
                    {
                        // Buttons
                        if (mappedStick.ButtonToVectorComponentMappings.TryGetValue(state.Offset, out var buttonMapping))
                        {
                            Log(($"ButtonToVector: Axis={state.Offset}, Value={state.Value}, OutputDevice={buttonMapping.OutputDevice}, Component={buttonMapping.OutputComponent}"));
                            if (buttonMapping.OutputDevice == "Throttle")
                            {
                                _vrThrottleThumb[buttonMapping.OutputComponent] = state.Value == 128 ? buttonMapping.Direction : 0;
                            }
                        }

                        if (mappedStick.ButtonToFloatMappings.TryGetValue(state.Offset, out var buttonToFloatMapping))
                        {
                            Log(($"ButtonToFloat: Axis={state.Offset}, Value={state.Value}, OutputDevice={buttonToFloatMapping.OutputDevice}"));
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

        private static float ConvertAxisValue(int value, bool invert)
        {
            float retVal;
            if (value == 65535) retVal = 1;
            else retVal = (((float)value / 32767) - 1);
            if (invert) retVal *= -1;
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
                    }

                    foreach (var axisToFloatMapping in stick.AxisToFloatMappings)
                    {
                        mapping.AxisToFloatMappings.Add(JoystickOffsetFromName(axisToFloatMapping.InputAxis), axisToFloatMapping);
                    }

                    foreach (var buttonToVectorComponentMapping in stick.ButtonToVectorComponentMappings)
                    {
                        mapping.ButtonToVectorComponentMappings.Add(JoystickOffsetFromName(ButtonNameFromIndex(buttonToVectorComponentMapping.InputButton)), buttonToVectorComponentMapping);
                    }

                    foreach (var buttonToFloatMapping in stick.ButtonToFloatMappings)
                    {
                        mapping.ButtonToFloatMappings.Add(JoystickOffsetFromName(ButtonNameFromIndex(buttonToFloatMapping.InputButton)), buttonToFloatMapping);
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
