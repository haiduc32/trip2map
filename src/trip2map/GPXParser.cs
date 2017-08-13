using System;
using System.Xml.Linq;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using trip2map.Models;

namespace trip2map
{
    public class GPXParser
    {
        public List<Activity> LoadFiles(List<string> fileNames)
        {
            //string[] fileNames = Directory.GetFiles(".\\activity\\");
            double totalDistance = 0;
            double totalElevationGain = 0;

            List<Activity> activities = new List<Activity>();

            foreach (string name in fileNames)
            {
                Console.WriteLine(name);
                Activity activity = LoadFile(name);
                totalDistance += Measurements.CalculateDistance(activity.Points);
                totalElevationGain += CalculateElevationGain(activity.Points);
                activities.Add(activity);
            }

            activities = activities.OrderBy(x=>x.StartTime).ToList();

            double discrepancy = 0.0;
            double elevationGainDiscrepancy = 0.0;
            for (int i = 0; i < activities.Count - 1; i++)
            {
                //check current activity with next activity last point - first point, for discrepances
                TrackPoint current = activities[i].Points.Last();
                TrackPoint next = activities[i+1].Points.First();

                var pointPair = new List<TrackPoint> { current, next};
                double d = Measurements.CalculateDistance(pointPair);
                double e = CalculateElevationGain(pointPair);

                //skip the bus trip
                if (d >100) continue;
                if (d < 1) continue;

                discrepancy += d;
                elevationGainDiscrepancy += e;

                // add extra points at 1km intervals
                int extraPoints = (int)Math.Round(discrepancy);
                double latInc = (next.Lat - current.Lat) / (extraPoints + 1);
                double lonInc = (next.Lon - current.Lon) / (extraPoints + 1);
                //consider an average of 6 minutes per km
                double minInc = 6.0;
                for (int j = 0; j < extraPoints+1; j++)
                {
                    //add the new activity
                    TrackPoint n = new TrackPoint
                    {
                        Lat = current.Lat + latInc * j,
                        Lon = current.Lon + lonInc * j,
                        Elevation = next.Elevation,
                        Time = next.Time > current.Time.AddMinutes(minInc * j) ? current.Time.AddMinutes(minInc * j) : next.Time
                    };
                    activities[i].Points.Add(n);
                }
            }

            Console.WriteLine($"Untracked distance: {discrepancy}");
            Console.WriteLine($"Untracked elevation gain: {elevationGainDiscrepancy}");
            totalDistance += discrepancy;
            Console.WriteLine($"Total distance: {totalDistance}");
            Console.WriteLine($"Total elevation gain: {totalElevationGain}");

            return activities;
        }

        public Activity LoadFile(string filePath)
        {
            Activity activity = new Activity();
            XDocument doc = XDocument.Load(filePath);
            var ns = doc.Root.GetDefaultNamespace();

            var metadata = doc.Root.Element(XName.Get("metadata", ns.NamespaceName));
            activity.StartTime = DateTimeOffset.Parse(metadata.Element(XName.Get("time", ns.NamespaceName)).Value);
            
            var trackPoints = doc.Root.Element(XName.Get("trk", ns.NamespaceName))
                .Element(XName.Get("trkseg", ns.NamespaceName));

            foreach (var pointElement in trackPoints.Elements())
            {
                TrackPoint point= new TrackPoint();
                var attr = pointElement.Attributes().ToList();
                point.Lat = double.Parse(pointElement.Attribute("lat").Value);
                point.Lon = double.Parse(pointElement.Attribute("lon").Value);
                point.Elevation = double.Parse(pointElement.Element(XName.Get("ele", ns.NamespaceName)).Value);
                point.Time = DateTimeOffset.Parse(pointElement.Element(XName.Get("time", ns.NamespaceName)).Value);

                activity.Points.Add(point);
            }

            double activityDistance = Measurements.CalculateDistance(activity.Points);
            Console.WriteLine(activityDistance);

            return activity;
        }

        public double CalculateElevationGain(List<TrackPoint> points)
        {
            double elevationgain = 0;
            for (int i = 0; i < points.Count -1; i++)
            {
                double gain = points[i+1].Elevation - points[i].Elevation;
                if (gain > 0.0) elevationgain += gain;
            }

            return elevationgain;
        }
    }
}