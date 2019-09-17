using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace VTOLVRPhysicalInput
{
    public class Mappings
    {
        [XmlElement("AxisToAxis")]
        public List<AxisToAxisMapping> AxisToAxisMappings = new List<AxisToAxisMapping>();
        [XmlElement("ButtonToButton")]
        public List<ButtonToButtonMapping> ButtonToButtonMappings = new List<ButtonToButtonMapping>();
    }

    public class AxisToAxisMapping
    {
        public string Name { get; set; }
        //public string HouseNo { get; set; }
        //public string StreetName { get; set; }
        //public string City { get; set; }
    }

    public class ButtonToButtonMapping
    {
        public string Name { get; set; }
    }
    //[XmlRoot("AxisToAxis")]
    //public class AxisToAxisMapping
    //{
    //    [XmlElement("Name")]
    //    public string Name { get; set; }
    //}

    //[XmlRoot("ButtonToButton")]
    //public class ButtonToButtonMapping
    //{
    //    [XmlElement("Name")]
    //    public string Name { get; set; }
    //}
}
