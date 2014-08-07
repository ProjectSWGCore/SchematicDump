using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Text;

namespace SchematicDump
{
	static class Program
	{
		static void Main()
		{
			Console.Title = "SchematicDump";
			int progress = 0;

			String[] allfiles = System.IO.Directory.GetFiles("./", "*.iff", System.IO.SearchOption.AllDirectories);
			
			foreach (string file in allfiles)
			{
				if (progress % 100 == 0) Console.WriteLine("Progress: {0}/{1}", progress, allfiles.Length);

				FileStream stream = File.OpenRead(file);
				using (stream)
				{
					StreamWriter fileStream = new StreamWriter(file.Replace(".iff", ".txt"));
					int index = 1;
					foreach (string slot in findData(stream, "SISSPCNT"))
					{
						fileStream.WriteLine("Slot {0}: {1}", index, slot.Replace(Convert.ToChar(0x00), ' ').Replace(Convert.ToChar(0x01), ' '));
						index++;
					}
					index = 1;
					foreach (string attribute in findData(stream, "DSSAPCNT"))
					{
						fileStream.WriteLine("Attribute {0}: {1}", index, attribute.Replace(Convert.ToChar(0x00), ' ').Replace(Convert.ToChar(0x01), ' '));
						index++;
					}
					fileStream.Write("Crafted Template: {0}", findCraftedTemplate(stream, file));
					fileStream.Close();
				}
				progress++;
			}

			Console.WriteLine("Finished generating {0} files.", allfiles.Length);
			Console.Read();
		}

		static List<String> findData(Stream dataStream, string type)
		{
			// "SISSPCNT"
			// "DSSAPCNT"

			int offset1 = 0x07;
			int offset2 = 0x0f;

			List<String> results = new List<string>();

			byte[] nameMatch = Encoding.ASCII.GetBytes(type);
			byte[] temp = new byte[nameMatch.Length];

			for (int i = 0; i < dataStream.Length; i++)
			{
				dataStream.Position = i;
				dataStream.Read(temp, 0, nameMatch.Length);

				if (temp.SequenceEqual(nameMatch))
				{
					dataStream.Position += offset2;

					byte[] resultData = new byte[dataStream.ReadByte() - offset1 - 1];

					dataStream.Position += offset1;
					dataStream.Read(resultData, 0, resultData.Length);

					results.Add(Encoding.UTF8.GetString(resultData, 0, resultData.Length));
				}
			}
			return results;
		}
		static string findCraftedTemplate(Stream dataStream, string file)
		{
			byte[] nameMatch = Encoding.ASCII.GetBytes("crafted");
			byte[] temp = new byte[nameMatch.Length];

			for (int i = 0; i < dataStream.Length; i++)
			{
				dataStream.Position = i;
				dataStream.Read(temp, 0, nameMatch.Length);
				if (temp.SequenceEqual(nameMatch))
				{
					dataStream.Position -= (nameMatch.Length + 1);

					byte[] resultData = new byte[dataStream.ReadByte()];
					dataStream.Position += 0x17;

					dataStream.Read(resultData, 0, resultData.Length);

					if (resultData.Length - 0x18 <= 0)
					{	
						Console.WriteLine("Error: Crafted Template Length less than 0 for file {0} when removing trail-space", file);
						return Encoding.UTF8.GetString(resultData, 0, resultData.Length);
					}
					return Encoding.UTF8.GetString(resultData, 0, resultData.Length - 0x18);
				}
			}
			return "Unknown";
		}
	}
}
