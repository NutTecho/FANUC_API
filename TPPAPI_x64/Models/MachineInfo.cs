using System;
using System.Collections.Generic;
using System.Linq;
// using System.Web;

namespace TPPAPI.Models
{
    public class MachineInfo
    {
        public string? CNC_TYPE {get; set;}
        public string? MC_TYPE {get; set;}
        public string? MT_TYPE { get; set; }
        public string? MT_DETAIL { get; set; }
        public int MAX_AXIS {get; set;}
        public int AXIS_USE {get; set;}
        public string? SERIES {get; set;}
        public string? MODULE_ID {get; set;}
        public string? SOFT_ID {get; set;}
        public string? SOFT_SERIES {get; set;}
        public string? SOFT_VERSION {get; set;}
    }
}