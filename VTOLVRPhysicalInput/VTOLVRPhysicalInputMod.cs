using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Xml;
using SharpDX.DirectInput;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VTOLVRPhysicalInput
{
    [Info("VTOLVR Physical Input", "Allows using of physical input devices in VTOL VR", "", "0.0.1")]
    public class VtolVrPhysicalInput : VTOLMOD
    {
        public static DirectInput DiInstance = new DirectInput();
        private Joystick _joystick;
        private VRJoystick _vj;
        private bool _waitingForVrJoystick;
        private bool _pollingEnabled;
        private float _roll, _pitch;
        private string _joystickName;

        public void Start()
        {
            ProcessSettingsFile();

            DontDestroyOnLoad(this.gameObject); // Required, else mod stops working when you leave opening scene and enter ready room
            Log("DBGVIEWCLEAR");
            Log("PhysicalInputMod| VTOL VR Mod loaded");
            var diDeviceInstances = DiInstance.GetDevices();

            foreach (var device in diDeviceInstances)
            {
                var joystick = new Joystick(DiInstance, device.InstanceGuid);
                if (!IsStickType(device)) continue;
                if (joystick.Information.ProductName != _joystickName) continue;;
                _joystick = joystick;
                break;
            }

            if (_joystick == null)
            {
                Log($"PhysicalInputMod| Joystick {_joystickName} not found");
                throw new Exception($"Joystick  {_joystickName} not found");
            }
            Log($"PhysicalInputMod| Joystick {_joystickName} found");
            _joystick.Properties.BufferSize = 128;
            _joystick.Acquire();
            
        }

        public void Update()
        {
            if (_pollingEnabled)
            {
                if (_vj == null || !InCockpit())
                {
                    _pollingEnabled = false;
                    _waitingForVrJoystick = false;
                    _vj = null;
                    Log("PhysicalInputMod| Left cockpit");
                }
            }
            else
            {
                if (_vj == null && InCockpit() && !_waitingForVrJoystick)
                {
                    // Entered cockpit
                    Log("PhysicalInputMod| Entered cockpit");
                    _waitingForVrJoystick = true;
                    StartCoroutine(FindScripts());
                    return;
                }
            }

            var data = _joystick.GetBufferedData();
            foreach (var state in data)
            {
                switch (state.Offset)
                {
                    case JoystickOffset.X:
                        _roll = (((float)state.Value / 32767) - 1) * -1;
                        break;
                    case JoystickOffset.Y:
                        _pitch = (((float)state.Value / 32767) - 1) * -1;
                        break;
                    default:
                        continue;
                }
            }
            // Take state from physical stick and apply to VRJoystick
            if (_vj != null)
            {
                _vj.OnSetStick.Invoke(new Vector3(_pitch, 0f, _roll));
            }
        }

        private IEnumerator FindScripts()
        {
            while (_vj == null)
            {
                //Searches the whole game scene for the script, there should only be one so its fine
                _vj = FindObjectOfType<VRJoystick>();
                if (_vj != null) continue;
                Log("PhysicalInputMod| Waiting for VRJoystick...");
                yield return new WaitForSeconds(1);
            }
            _waitingForVrJoystick = false;
            _pollingEnabled = true;
            Log("PhysicalInputMod| Got VRJoystick");
        }

        private static bool InCockpit()
        {
            return SceneManager.GetActiveScene().buildIndex == 7 || SceneManager.GetActiveScene().buildIndex == 11;
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

        public void ProcessSettingsFile()
        {
            var settingsFile = Path.Combine(Directory.GetCurrentDirectory(), @"VTOLVR_ModLoader\Mods\VTOLVRPhysicalInputSettings.xml");
            if (File.Exists(settingsFile))
            {
                var doc = new XmlDocument();
                doc.Load(settingsFile);
                _joystickName = doc.SelectSingleNode("/Settings/Setting[Name = \"JoystickName\"]")
                    ?.SelectSingleNode("Value")
                    ?.InnerText;
            }
            else
            {
                Log($"{settingsFile} not found");
                throw new Exception($"{settingsFile} not found");
            }
        }

        private void Log(string text)
        {
            System.Diagnostics.Debug.WriteLine($"PhysicalStickMod| {text}");
        }
    }
}
