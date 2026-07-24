using System;
using System.Collections.Generic;
using System.Linq;
// using System.Web;

namespace TPPAPI.Models
{
    public class StatusModel
    {
       public short AUTO {get ;set;}
       public string? AUTO_DETAIL {get ;set;}
       public short RUN {get; set;}
       public string? RUN_DETAIL {get; set;}
       public short MSTB {get; set;}
       public string? MSTB_DETAIL {get; set;}
       public short EMER {get; set;}
       public string? EMER_DETAIL {get; set;}
       public short ALARM {get; set;}
       public string? ALARM_DETAIL {get; set;}
       public short TM_MODE {get; set;}
       public string? TM_DETAIL {get; set;}
       public short EDIT {get; set;}
       public string? EDIT_DETAIL {get; set;}
       public short MOTION {get; set;}
       public string? MOTION_DETAIL {get; set;}

       public int ACT_FEED_RATE { get; set; }

       public int ACT_SPD_SPEED { get; set; }

       public int[]? ABSOLUTE_POS { get; set; }
       public int[]? RELATIVE_POS { get; set; }
       public int[]? MACHINE_POS { get; set; }
       public int[]? DISTANCE_POS { get; set; }



    }
}