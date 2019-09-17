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
        public List<StickMappings> StickMappings = new List<StickMappings>();
    }

    public class StickMappings
    {
        public string StickName { get; set; }
        [XmlElement("AxisToAxis")]
        public List<AxisToAxisMapping> AxisToAxisMappings = new List<AxisToAxisMapping>();
        [XmlElement("ButtonToButton")]
        public List<ButtonToButtonMapping> ButtonToButtonMappings = new List<ButtonToButtonMapping>();
    }

    public class AxisToAxisMapping
    {
        public string InputAxis { get; set; }
        public bool Invert { get; set; }
        public string OutputDevice { get; set; }
        public string OutputAxis { get; set; }
    }

    public class ButtonToButtonMapping
    {
        public string Name { get; set; }
    }
}
