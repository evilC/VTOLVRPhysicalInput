using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace VTOLVRPhysicalInput
{
    public class Mappings
    {
        [XmlElement("StickMappings")]
        public List<StickMappingList> MappingsList = new List<StickMappingList>();

        [XmlIgnore]
        public Dictionary<string, StickMappings> Sticks = new Dictionary<string, StickMappings>();
    }

    public class StickMappingList
    {
        public string StickName { get; set; }
        [XmlElement("AxisToVectorComponent")]
        public List<AxisToVectorComponentMapping> AxisToVectorComponentMappings = new List<AxisToVectorComponentMapping>();
        [XmlElement("AxisToFloat")]
        public List<AxisToFloatMapping> AxisToFloatMappings = new List<AxisToFloatMapping>();
        [XmlElement("ButtonToVectorComponent")]
        public List<ButtonToVectorComponent> ButtonToVectorComponentMappings = new List<ButtonToVectorComponent>();
    }

    public class StickMappings
    {
        public Dictionary<string, AxisToVectorComponentMapping> AxisToVectorComponentMappings = new Dictionary<string, AxisToVectorComponentMapping>();
        public Dictionary<string, AxisToFloatMapping> AxisToFloatMappings = new Dictionary<string, AxisToFloatMapping>();
        public Dictionary<string, ButtonToVectorComponent> ButtonToVectorComponentMappings = new Dictionary<string, ButtonToVectorComponent>();
    }

    public class AxisToVectorComponentMapping
    {
        public string InputAxis { get; set; }
        public bool Invert { get; set; }
        public string OutputDevice { get; set; }
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
}
