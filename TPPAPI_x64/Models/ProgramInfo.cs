using System;
using System.Collections.Generic;
using System.Linq;
// using System.Web;

namespace TPPAPI.Models
{
    public class ProgramInfo
    {
        public string? MAIN_PROG {get; set;}

        public string? CURRENT_PROG  {get; set;}

        public string? SEQNUM {get; set;}

        public short? REG_PROG {get; set;}

        public short? UNREG_PROG { get; set; }

        public double USED_MEM {get; set;}

        public double UNUSED_MEM {get; set;}
        
    }
}