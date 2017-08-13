using System;
using System.Xml.Linq;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using ImageMagick;
using trip2map.Models;

namespace trip2map
{
	public class VisualisationEngine
	{
		public class ImageGPS
		{
			public long? Lat;
			public long? Lon;
			public DateTimeOffset? Timestamp;
			public DateTime? LocalTime;
		}

		private List<TrackPoint> _all;
		private List<Models.Action> _actions;
		private List<(string path, ExifProfile exif)> _exifs;

		public void Load(List<TrackPoint> all)
		{
			_all = new List<TrackPoint>(all);
		}

		public void LoadImageExifs(List<string> imagePaths)
		{
			_exifs = new List<(string, ExifProfile)>();

			foreach (string imagePath in imagePaths)
				using (MagickImage image = new MagickImage(imagePath))
				{
					//Retrieve the exif information
					ExifProfile profile = image.GetExifProfile();
					_exifs.Add((imagePath, profile));
				}
		}

		public void ResizeImages(string outPath)
		{
			//check that the out folders exist, or create it
			if (!Directory.Exists(outPath))
			{
				Directory.CreateDirectory(outPath);
			}

			foreach (string imagePath in _exifs.Select(x => x.path))
				using (MagickImage image = new MagickImage(imagePath))
				{
					string outFilePath = Path.Combine(outPath, Path.GetFileName(imagePath));
					ResizeImage(image, outFilePath);
				}
		}

		// /// <summary>
		// /// Exports the json file for the slideshow.
		// /// </summary>
		// /// <param name="fileName">Output file name.</param>
		// public void ExportSlideshow(string fileName)
		// {
			
		// }

		/// <summary>
		/// Exports the json file for the activities.
		/// </summary>
		/// <param name="fileName">Output file name.</param>
		public void Export(string fileName)
		{
			var data = Generate();
			AddSlideShowActions(data);

			string json = JsonConvert.SerializeObject(data, Formatting.Indented);
			File.WriteAllText(fileName, json);
		}

		private void AddSlideShowActions(List<Models.Action> data)
		{
			var imagesGPS = _exifs.Select(x => new { path = x.path, gps = GetExifGPS(x.exif)});
			
			//for photos without GPS timestamp we'll consider timezone offset, same as first activity
			// and construct the Timestamp
			foreach (var image in imagesGPS.Where(x => !x.gps.Timestamp.HasValue))
			{
				image.gps.Timestamp = new DateTimeOffset(image.gps.LocalTime.Value, data.First().Time.Offset);
			}

			imagesGPS = imagesGPS.OrderBy(x => x.gps.Timestamp);

			var imagesBefore = imagesGPS.Where(x => x.gps.Timestamp < data.First().Time).ToList();
			int index = 0;
			var firstAction = data.First();
			foreach (var image in imagesBefore)
			{
				
				Models.Action action = new Models.Action
				{
					Type = "picture",
					Lat = firstAction.Lat,
					Lon = firstAction.Lon,
					Time = image.gps.Timestamp.Value,
					Distance = 0,
					TotalDistance = 0,
					Day = (firstAction.Time.Date - image.gps.Timestamp.Value.Date).Days + 1,
					Image = StripToImageName(image.path)
				};

				//insert the new "picture" actions incrementaly 
				data.Insert(index++, action);
			}
			
			var imagesMiddle = imagesGPS.Where(x => x.gps.Timestamp > firstAction.Time &&
				x.gps.Timestamp < data.Last().Time).ToList();
			foreach (var image in imagesMiddle)
			{
				var previousAction  = data.Last(x => x.Time < image.gps.Timestamp /*&& x.Type != "picture"*/);
				int indexOfAction = data.IndexOf(previousAction);

				Models.Action action = new Models.Action
				{
					Type = "picture",
					// not sure about what is the best coord to put here, 
					// but probably it's still good to keep the previous action coords
					Lat = previousAction.Lat,
					Lon = previousAction.Lon,
					Time = image.gps.Timestamp.Value,
					Distance = previousAction.Distance,
					TotalDistance = previousAction.TotalDistance,
					Day = (image.gps.Timestamp.Value.Date - firstAction.Time.Date).Days + 1,
					Image = StripToImageName(image.path)
				};

				data.Insert(indexOfAction + 1, action);
			}
			
			
			//and al lthe rest images that we drop post all the actions
			var lastAction = data.Last();
			var imagesTail = imagesGPS.Where(x => x.gps.Timestamp > lastAction.Time).ToList();
			foreach (var image in  imagesTail)
			{
				Models.Action action = new Models.Action
				{
					Type = "picture",
					Lat = lastAction.Lat,
					Lon = lastAction.Lon,
					Time = image.gps.Timestamp.Value,
					Distance = lastAction.Distance,
					TotalDistance = lastAction.TotalDistance,
					Day = (image.gps.Timestamp.Value.Date - firstAction.Time.Date).Days + 1,
					Image = StripToImageName(image.path)
				};
				data.Add(action);
			}
		}

		private void ResizeImage(MagickImage image, string path)
		{
			image.Quality = 60;
			
			//RightTop - vertical
			if (image.Orientation == OrientationType.RightTop)
			{
					image.Resize(800, 800);
					image.Rotate(90);
					image.Orientation = OrientationType.TopLeft;
			}
			else
			{
				image.Resize(1400, 1400);
			}

			
			image.Write(path);
			//Path.GetDirectoryName(path) 
		}

		private ImageGPS GetExifGPS(ExifProfile profile)
		{
			ImageGPS gps = new ImageGPS();

			string exifLatRef = profile.GetValue(ExifTag.GPSLatitudeRef)?.ToString();
			string exifLat = profile.GetValue(ExifTag.GPSLatitude)?.ToString();

			string exifLonRef = profile.GetValue(ExifTag.GPSLongitudeRef)?.ToString();
			string exifLon = profile.GetValue(ExifTag.GPSLongitude)?.ToString();

			string exifTime = profile.GetValue(ExifTag.GPSTimestamp)?.ToString().Trim();
			string exifDate = profile.GetValue(ExifTag.GPSDateStamp)?.ToString().Trim();

			if (exifLatRef != null && exifLat != null && exifLonRef != null && exifLon != null &&
				exifTime != null && exifDate != null)
			{
				// GPSLatitudeRef(Ascii): N
				// GPSLatitude(Rational): 43 31 19
				// GPSLongitudeRef(Ascii): E
				// GPSLongitude(Rational): 6 53 21

				// N - latitude positive
				// E - longituted positive
				long latSign = exifLatRef == "N" ? 1 : -1;
				string[] latSplit = exifLat.Split(' ');
				gps.Lat = latSign * (long.Parse(latSplit[0]) + long.Parse(latSplit[1]) / 60 + long.Parse(latSplit[2]) / 3600);

				long lonSign = exifLonRef == "E" ? 1 : -1;
				string[] lonSplit = exifLon.Split(' ');
				gps.Lat = lonSign * (long.Parse(latSplit[0]) + long.Parse(latSplit[1]) / 60 + long.Parse(latSplit[2]) / 3600);
			
				//TODO: load the timestamp
				gps.Timestamp = DateTimeOffset.ParseExact(exifDate + " " + exifTime+"Z", "yyyy:MM:dd H m sK", null);
			}

			string exifDTOriginal = profile.GetValue(ExifTag.DateTimeOriginal)?.Value?.ToString();
			
			if (exifDTOriginal == null)
			{
				string exifDT = profile.GetValue(ExifTag.DateTime)?.Value?.ToString();
				gps.LocalTime = DateTime.Parse(exifDT);
			}
			else
			{
				gps.LocalTime = DateTime.ParseExact(exifDTOriginal, "yyyy:MM:dd HH:mm:ss", null);
			}

			return gps;
		}

		private List<Models.Action> Generate()
		{
			List<Models.Action> actionList = new List<Models.Action>();
			
			var groupByDay = GroupByDays();
			DateTimeOffset day1 = groupByDay.First().Key;
			double totalDistance = 0.0;
			foreach (var group in groupByDay)
			{
				//double totalDistance = CalculateDistance(group.ToList());
				double segmentSize = 2.0;

				var pointsInGroup = group.OrderBy(x => x.Time).ToList();
				Models.Action action = new Models.Action
				{
					Type = "cycle",
					Lat = pointsInGroup.First().Lat,
					Lon = pointsInGroup.First().Lon,
					Time = pointsInGroup.First().Time,
					Distance = 0,
					TotalDistance = totalDistance,
					Day = (pointsInGroup.First().Time.DateTime - day1.DateTime).Days + 1
				};
				actionList.Add(action);
				
				double distance = 0.0;
				double nextMarker = segmentSize;
				for (int i = 1; i < pointsInGroup.Count - 1; i++)
				{
					double d = Measurements.Distance(pointsInGroup[i - 1].Lat, pointsInGroup[i - 1].Lon, pointsInGroup[i].Lat, pointsInGroup[i].Lon);
					if (d > 100) continue;
					distance += d;
					totalDistance += d;
					if (distance > nextMarker)
					{
						//add new action, increment nextMarker
						do
						{
							nextMarker += segmentSize;
						} while (nextMarker < distance);

						action = new Models.Action
						{
							Type = "cycle",
							Lat = pointsInGroup[i].Lat,
							Lon = pointsInGroup[i].Lon,
							Time = pointsInGroup[i].Time,
							Distance = distance,
							TotalDistance = totalDistance,
							Day = (pointsInGroup[i].Time.DateTime - day1.DateTime).Days + 1
						};
						actionList.Add(action);
					}
					//add the distance to segmentSize increments, then add a new Action
				}

				action = new Models.Action
				{
					Type = "camp",
					Lat = pointsInGroup.Last().Lat,
					Lon = pointsInGroup.Last().Lon,
					Time = pointsInGroup.Last().Time,
					Distance = distance,
					TotalDistance = totalDistance,
					Day = (pointsInGroup.Last().Time.DateTime - day1.DateTime).Days + 1
				};
				actionList.Add(action);
			}

			return _actions = actionList;
		}

		private string StripToImageName(string path)
		{
			return path.Substring(path.LastIndexOf('\\') + 1);
		}

		private IEnumerable<IGrouping<DateTime, TrackPoint>> GroupByDays() => _all.GroupBy(x => x.Time.Date);
	}
}