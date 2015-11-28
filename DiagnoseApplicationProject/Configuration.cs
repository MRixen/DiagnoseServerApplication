using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RBC
{
    public enum feederBehavior
    {
        runNever,
        runAlways,
        runNoParts,
        runLastPart
    }

    public class Configuration
    {
        public Boolean debuggingActive { get; set; }
        public feederBehavior feederbehavior { get; set; }
    }
}
