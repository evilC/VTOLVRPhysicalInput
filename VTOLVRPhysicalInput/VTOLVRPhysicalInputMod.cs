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
        private readonly Dictionary<string, float> _vrJoystickValues = new Dictionary<string, float>() {{"PitchAxis", 0}, {"RollAxis", 0}, {"YawAxis", 0}};
        private readonly Dictionary<string, float> _vrThrottleValues = new Dictionary<string, float>() {{"ThrottleAxis", 0}};
        private Vector3 _vrThrottleThumb = new Vector3(0, 0,0);
        private readonly List<string> _polledStickNames = new List<string>();
        private readonly List<Joystick> _polledSticks = new List<Joystick>();
        private readonly Dictionary<string, List<StickMapping>> _stickMappings = new Dictionary<string, List<StickMapping>>();
        private Mappings _mappings;

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

            foreach (var polledStickName in _polledStickNames)
            {
                if (!foundSticks.ContainsKey(polledStickName))
                {
                    ThrowError($"Joystick {polledStickName} not found");
                }
                Log($"Joystick {polledStickName} found");
                foundSticks[polledStickName].Properties.BufferSize = 128;
                foundSticks[polledStickName].Acquire();
                _polledSticks.Add(foundSticks[polledStickName]);
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

            if (VrControlsAvailable())
            {
                _vrJoystick.OnSetStick.Invoke(new Vector3(_vrJoystickValues["PitchAxis"], _vrJoystickValues["YawAxis"], _vrJoystickValues["RollAxis"]));
                _vrThrottle.OnSetThrottle.Invoke(_vrThrottleValues["ThrottleAxis"]);
                _vrThrottle.OnSetThumbstick.Invoke(_vrThrottleThumb);
            }
        }

        public void PollSticks()
        {
            foreach (var polledStick in _polledSticks)
            {
                var data = polledStick.GetBufferedData();
                foreach (var state in data)
                {
                    var mappedStick = _stickMappings[polledStick.Information.ProductName];
                    var ax = state.Offset.ToString();
                    foreach (var stickMapping in mappedStick)
                    {
                        if (stickMapping.InputAxis == ax)
                        {
                            var value = ConvertAxisValue(state.Value, stickMapping.Invert);
                            Log($"Stick: {polledStick.Information.ProductName}, Axis: {ax}, Input Value: {state.Value}, Output Device: {stickMapping.OutputDevice}, Output Axis: {stickMapping.OutputAxis}, Output Value: {value}");
                            if (stickMapping.OutputDevice == "Joystick")
                            {
                                _vrJoystickValues[stickMapping.OutputAxis] = value;
                            }
                            else
                            {
                                _vrThrottleValues[stickMapping.OutputAxis] = value;
                            }
                            break;
                        }
                    }
                    if (state.Offset == JoystickOffset.Buttons6)
                    {
                        Log($"B7: {state.Value}");
                        if (state.Value == 128)
                        {
                            _vrThrottleThumb.y = 1;
                        }
                        else
                        {
                            _vrThrottleThumb.y = 0;
                        }
                        
                    }
                    else if (state.Offset == JoystickOffset.Buttons8)
                    {
                        Log($"B9: {state.Value}");
                        if (state.Value == 128)
                        {
                            _vrThrottleThumb.y = -1;
                        }
                        else
                        {
                            _vrThrottleThumb.y = 0;
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
                _mappings = (Mappings)obj;
                reader.Close();

                // Build Dictionary
                // ToDo: How to do this as part of XML Deserialization?
                foreach (var stick in _mappings.MappingsList)
                {
                    if (!_mappings.Sticks.ContainsKey(stick.StickName))
                    {
                        _mappings.Sticks.Add(stick.StickName, new StickMappings());
                    }
                    foreach (var axisToVectorComponentMapping in stick.AxisToVectorComponentMappings)
                    {
                        _mappings.Sticks[stick.StickName].AxisToVectorComponentMappings.Add(JoystickOffsetFromName(axisToVectorComponentMapping.InputAxis), axisToVectorComponentMapping);
                    }

                    foreach (var axisToFloatMapping in stick.AxisToFloatMappings)
                    {
                        _mappings.Sticks[stick.StickName].AxisToFloatMappings.Add(JoystickOffsetFromName(axisToFloatMapping.InputAxis), axisToFloatMapping);
                    }

                    foreach (var buttonToVectorComponentMapping in stick.ButtonToVectorComponentMappings)
                    {
                        _mappings.Sticks[stick.StickName].ButtonToVectorComponentMappings.Add(JoystickOffsetFromName("Buttons" + (buttonToVectorComponentMapping.InputButton - 1)), buttonToVectorComponentMapping);
                    }
                }

                var debug = "me";
                /*
                var serializer = new XmlSerializer(typeof(List<Setting>), new XmlRootAttribute("Settings"));
                var stringReader = new StringReader(File.ReadAllText(settingsFile));
                var settings = (List<Setting>)serializer.Deserialize(stringReader);
                foreach (var setting in settings)
                {
                    if (!_polledStickNames.Contains(setting.StickName))
                    {
                        _polledStickNames.Add(setting.StickName);
                    }

                    if (!_stickMappings.ContainsKey(setting.StickName))
                    {
                        _stickMappings[setting.StickName] = new List<StickMapping>();
                    }

                    _stickMappings[setting.StickName].Add(new StickMapping
                    {
                        InputAxis = setting.StickAxis, OutputAxis = setting.Name, Invert = setting.Invert, OutputDevice = setting.OutputDevice
                    });
                }
                */
            }
            else
            {
                Log($"{settingsFile} not found");
                throw new Exception($"{settingsFile} not found");
            }
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
