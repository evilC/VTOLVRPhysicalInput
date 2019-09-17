using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using SharpDX.DirectInput;

namespace VTOLVRPhysicalInput
{
    #region Load-Time Objects
    public class Mappings
    {
        [XmlElement("StickMappings")]
        public List<StickMappingList> MappingsList = new List<StickMappingList>();
    }

    // Used at load-time to parse XML
    public class StickMappingList
    {
        public string StickName { get; set; }
        [XmlElement("AxisToVectorComponent")]
        public List<AxisToVectorComponentMapping> AxisToVectorComponentMappings = new List<AxisToVectorComponentMapping>();
        [XmlElement("AxisToFloat")]
        public List<AxisToFloatMapping> AxisToFloatMappings = new List<AxisToFloatMapping>();
        [XmlElement("ButtonToVectorComponent")]
        public List<ButtonToVectorComponent> ButtonToVectorComponentMappings = new List<ButtonToVectorComponent>();
        [XmlElement("ButtonToFloat")]
        public List<ButtonToFloat> ButtonToFloatMappings = new List<ButtonToFloat>();
    }
    #endregion

    #region Run-Time Objects
    public class MappingsDictionary
    {
        public Dictionary<string, StickMappings> Sticks = new Dictionary<string, StickMappings>();
    }

    // Used at run-time to process input
    public class StickMappings
    {
        public string StickName { get; set; }
        public Joystick Stick { get; set; }
        public Dictionary<JoystickOffset, AxisToVectorComponentMapping> AxisToVectorComponentMappings = new Dictionary<JoystickOffset, AxisToVectorComponentMapping>();
        public Dictionary<JoystickOffset, AxisToFloatMapping> AxisToFloatMappings = new Dictionary<JoystickOffset, AxisToFloatMapping>();
        public Dictionary<JoystickOffset, ButtonToVectorComponent> ButtonToVectorComponentMappings = new Dictionary<JoystickOffset, ButtonToVectorComponent>();
        public Dictionary<JoystickOffset, ButtonToFloat> ButtonToFloatMappings = new Dictionary<JoystickOffset, ButtonToFloat>();
    }
    #endregion

    #region Common Objects
    public class AxisToVectorComponentMapping
    {
        public string InputAxis { get; set; }
        public bool Invert { get; set; }
        public string OutputDevice { get; set; } // ToDo: Make enum
        public string OutputComponent { get; set; }
    }

    public class AxisToFloatMapping
    {
        public string InputAxis { get; set; }
        public bool Invert { get; set; }
        public string OutputDevice { get; set; }
    }

    public class ButtonToVectorComponent
    {
        public int InputButton { get; set; }
        public string OutputDevice { get; set; }
        public string OutputComponent { get; set; }
        public float Direction { get; set; }
    }

    public class ButtonToFloat
    {
        public int InputButton { get; set; }
        public string OutputDevice { get; set; }
        public float PressValue { get; set; }
        public float ReleaseValue { get; set; }
    }
    #endregion
}
