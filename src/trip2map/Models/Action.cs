using System;

namespace trip2map.Models
{
    public class Action
    {
        public string Type {get; set;}
        public double Lat {get; set; }
        public double Lon {get; set;}
        public DateTimeOffset Time {get; set;}
        public double Distance {get; set;}
        public double TotalDistance {get; set;}
        public string Param {get; set;}
        public int Day {get; set;}
        public string Image { get; set; }
    }
}