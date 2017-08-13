using System;
using System.Collections.Generic;

namespace trip2map.Models
{
    public class Activity
    {
        public Activity()
        {
            Points  = new List<TrackPoint>();
        }

        public DateTimeOffset StartTime {get; set;}
        public List<TrackPoint> Points {get; set; }
    }
}