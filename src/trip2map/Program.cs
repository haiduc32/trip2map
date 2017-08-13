using System;
using System.Xml.Linq;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.CommandLineUtils;

namespace trip2map
{
	class Program
	{
		static void Main(string[] args)
		{
			// THIS IS WOKR IN PROGRESS
			// CommandLineApplication cmd = new CommandLineApplication(throwOnUnexpectedArg: false);
			// cmd.Name = "trip2map";
			// cmd.Description = "<TBD>";

			// var gpxOption = cmd.Option("-gpx",
			// 	"GPX file or folder containing GPX files", CommandOptionType.SingleValue);
			
			// var imgOption = cmd.Option("-img",
			// 	"Folder containing the images (JPG only)", CommandOptionType.SingleValue);
			
			// var nameOption = cmd.Option("-o",
			// 	"Output folder name", CommandOptionType.SingleValue);

			if (args.Count() != 3)
			{
				Console.WriteLine("You should execute the program with the following parameters:" +
					"\ttrip2map <gpx> <images> <out-name>" +
					"\t<gpx> - the name of the gpx file or folder containing all gpx files" +
					"\t<images> - the folder with all the images" +
					"\t<out-name> - the name for the new generated folder");
			}

			string gpxOption = args[0];
			if (!Directory.Exists(gpxOption))
			{
				if (!File.Exists(gpxOption))
				{
					Console.Error.WriteLine("Could not find the GPX directory or file.");
				}
			}

			string imagesOption = args[1];
			if (!Directory.Exists(imagesOption))
			{
				Console.Error.WriteLine("Could not find the images directory.");
			}

			string outOption = args[2];

			
			var gpxFiles = ListGpxFiles(gpxOption);
			GPXParser parser = new GPXParser();

			Console.WriteLine("Hello World!");

			var data = parser.LoadFiles(gpxFiles);
			VisualisationEngine ve = new VisualisationEngine();
			ve.Load(data.SelectMany(x => x.Points).ToList());
			ve.LoadImageExifs(Directory.GetFiles(imagesOption).ToList());
			
			var outDir = Directory.CreateDirectory(outOption);

			
			string activityFile = outOption.Split('\\').Last() + ".json";
			//write index.html file
			WriteIndexPage(outDir.ToString(), activityFile);

			ve.Export(Path.Combine(outDir.ToString(), activityFile));
			ve.ResizeImages(Path.Combine(outDir.ToString(), "images\\resized"));

			

			Console.WriteLine("Done.");
		}

		private static void WriteIndexPage(string path, string activityFile)
		{
			var assembly = typeof(trip2map.Program).Assembly;
			var allResources = assembly.GetManifestResourceNames();
			Stream resource = assembly.GetManifestResourceStream("trip2map.index_template.html");
			StreamReader sr = new StreamReader(resource);
			string pageContents = sr.ReadToEnd();
			pageContents = pageContents.Replace("<!-- activity file -->", activityFile);
			File.WriteAllText(Path.Combine(path, "index.html"), pageContents);
		}

		private static List<string> ListGpxFiles(string path)
		{
			if (File.Exists(path))
			{
				return new List<string> { path };
			}
			else
			{
				return Directory.GetFiles(path, "*.gpx").ToList();
			}
		}
	}
}
