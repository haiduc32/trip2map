using System;
using System.Collections.Generic;
using trip2map.Models;

namespace trip2map
{
    public static class Measurements
    {
        public static double CalculateDistance(List<TrackPoint> points)
        {
            double distance = 0;
            for (int i = 0; i < points.Count - 1; i++)
            {
                double d = Distance(points[i].Lat, points[i].Lon, points[i + 1].Lat, points[i + 1].Lon);
                // if (double.IsNaN(d))
                // {
                //     distance += 0;
                //     Distance(points[i].Lat, points[i].Lon, points[i + 1].Lat, points[i + 1].Lon);
                // }
                // else
                {
                    distance += d;
                }
            }

            return distance;
        }

        public static double Distance(double lat1, double lon1, double lat2, double lon2) {
            double theta = lon1 - lon2;
            double dist = Math.Sin(deg2rad(lat1)) * Math.Sin(deg2rad(lat2)) + Math.Cos(deg2rad(lat1)) * Math.Cos(deg2rad(lat2)) * Math.Cos(deg2rad(theta));
            if (dist > 1.0) dist = 1.0;
            dist = Math.Acos(dist);
            dist = rad2deg(dist);
            dist = dist * 60 * 1.1515;
                dist = dist * 1.609344;

            return dist;
        }

        private static double rad2deg(double rad) {
            return (rad / Math.PI * 180.0);
        }

        private static double deg2rad(double deg) {
            return (deg * Math.PI / 180.0);
        }
    }
}