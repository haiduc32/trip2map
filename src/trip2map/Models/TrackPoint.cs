using System;

namespace trip2map.Models
{
    public class TrackPoint
    {
        public double Lat {get; set;}
        public double Lon {get; set;}
        public double Elevation {get; set;}
        public DateTimeOffset Time {get; set;}
        public int Cadence {get; set;}

    }
}