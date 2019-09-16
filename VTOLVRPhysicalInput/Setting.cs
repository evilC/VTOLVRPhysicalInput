using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VTOLVRPhysicalInput
{
    public class Setting
    {
        public string Name { get; set; }
        public string StickName { get; set; }
        public string StickAxis { get; set; }
        public string OutputDevice { get; set; }
        public bool Invert { get; set; }
    }
}
