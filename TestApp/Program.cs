using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var foo = new VTOLVRPhysicalInput.VtolVrPhysicalInput();
            foo.InitSticks(true);
            while (true)
            {
                foo.PollSticks();
                Thread.Sleep(200);
            }
        }
    }
}
