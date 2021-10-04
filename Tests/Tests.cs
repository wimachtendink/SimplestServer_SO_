using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimplestServer_SO.Tests
{
	public class Tests
	{

		/// <summary>
		/// returns true if all tests pass
		/// </summary>
		/// <returns></returns>
		public static bool DoAllTests()
		{
			if (!ConfigMessageBitParserTest()) { return false; }

			return true;
		}

		public static bool ConfigMessageBitParserTest()
		{
			var derp = new ConfigMessage();

			for (int i = 1; i < 65535; i++)
			{
				derp.SampleRate = i;
				if (derp.SampleRate != i)
				{
					Console.WriteLine($"FAILED\n\t{derp.SampleRate}:{i}");
					return false;
				}
			}
			Console.WriteLine($"Passed!");
			return true;
		}
	}
}
