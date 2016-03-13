using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

using TeensyBatMap.Domain;

namespace TeensyBatMap.Tests
{
	[TestFixture]
    public class PeakTests
    {
		[Test]
		public void CanFindSinglePeak()
		{
			FftAnalyzer analyzer = new FftAnalyzer(1.5, 3);

			uint[] data = { 0, 1, 2, 3, 8, 2, 3, 1 };

			KeyValuePair<int, uint>[] peaks = analyzer.GetPeaks(data).ToArray();

			Assert.AreEqual(1, peaks.Length);
			Assert.AreEqual(4, peaks[0].Key);
			Assert.AreEqual(8, peaks[0].Value);
		}

		[Test]
		public void CanFindSingleMiddlePeak()
		{
			FftAnalyzer analyzer = new FftAnalyzer(1.5, 3);

			uint[] data = { 0, 0, 9, 9, 0, 0 };

			KeyValuePair<int, uint>[] peaks = analyzer.GetPeaks(data).ToArray();

			Assert.AreEqual(1, peaks.Length);
			Assert.AreEqual(2, peaks[0].Key);
		}

		[Test]
		public void CanFindPeakWithNoiseAtEnd()
		{
			FftAnalyzer analyzer = new FftAnalyzer(1.5, 3);

			uint[] data = { 0, 0, 9, 8, 7, 6, 8 };

			KeyValuePair<int, uint>[] peaks = analyzer.GetPeaks(data).ToArray();

			Assert.AreEqual(1, peaks.Length);
			Assert.AreEqual(2, peaks[0].Key);
		}

		[Test]
		public void CanFindMultipleSteepPeaks()
		{
			FftAnalyzer analyzer = new FftAnalyzer(1.5, 3);

			uint[] data = { 0, 9, 8, 0, 0, 9, 8, 0, 0, 9, 9 };

			KeyValuePair<int, uint>[] peaks = analyzer.GetPeaks(data).ToArray();

			Assert.AreEqual(3, peaks.Length);
			Assert.AreEqual(1, peaks[0].Key);
			Assert.AreEqual(5, peaks[1].Key);
			Assert.AreEqual(9, peaks[2].Key);
		}

		[Test]
		public void CanFindPeaksInRealSample()
		{
			FftAnalyzer analyzer = new FftAnalyzer(2.5);

			uint[] data = { 0, 5, 5, 5, 4, 4, 5, 4, 4, 4, 3, 3, 3, 3, 4, 3, 2, 4, 4, 4, 3, 4, 5, 5, 7, 4, 4, 6, 8, 8, 11, 15, 23, 34, 42, 48, 55, 94, 64, 86, 122, 129, 83, 45, 45, 30, 19, 20, 21, 14, 11, 17, 13, 14, 11, 10, 6, 8, 8, 7, 6, 4, 4, 4, 4, 3, 4, 2, 3, 4, 3, 3, 3, 2, 4, 3, 4, 4, 3, 6, 5, 3, 8, 4, 4, 5, 5, 5, 5, 5, 5, 5, 5, 7, 6, 11, 11, 11, 12, 16, 17, 22, 25, 22, 27, 31, 26, 25, 25, 26, 23, 20, 18, 14, 15, 14, 9, 19, 13, 18, 16, 13, 14, 14, 16, 12, 9, 8, 8, 6, 5, 4, 5, 6, 7, 6, 5, 6, 5, 5, 7, 5, 4, 2, 2, 3, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 1, 1, 1, 2, 1, 2, 2, 2, 3, 2, 2, 2, 2, 2, 2, 2, 2, 2, 3, 2, 2, 2, 2, 2, 4, 3, 3, 3, 3, 4, 3, 3, 3, 3, 3, 3, 3, 3, 3, 2, 2, 2, 4, 2, 2, 2, 3, 3, 3, 5, 5, 2, 2, 4, 3, 2, 2, 3, 2, 2, 2, 2, 2, 1, 2, 2, 3, 2, 2, 2, 3, 1, 2, 2, 2, 1, 1, 1, 1, 2, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 3, 3, 2, 2, 2, 3, 3, 3 };

			KeyValuePair<int, uint>[] peaks = analyzer.GetPeaks(data).ToArray();

			Assert.AreEqual(2, peaks.Length);
			Assert.AreEqual(129, peaks[0].Value);
			Assert.AreEqual(31, peaks[1].Value);
		}

		[Test]
		public void CanFindPeaksInRealSample2()
		{
			FftAnalyzer analyzer = new FftAnalyzer(2.5);

			uint[] data = { 0, 4, 4, 4, 3, 3, 3, 4, 2, 2, 2, 3, 3, 3, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 3, 3, 3, 3, 5, 5, 8, 6, 13, 18, 42, 62, 62, 14, 10, 18, 18, 12, 4, 2, 12, 5, 3, 3, 2, 2, 2, 2, 2, 2, 2, 1, 2, 2, 2, 3, 2, 2, 1, 1, 1, 1, 1, 1, 2, 1, 1, 1, 2, 1, 2, 1, 1, 1, 2, 1, 2, 2, 2, 2, 2, 2, 2, 2, 1, 2, 2, 2, 2, 2, 1, 2, 2, 2, 1, 2, 2, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 1, 1, 1, 1, 2, 1, 1, 1, 2, 1, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 0, 0, 1, 1, 0, 1, 1, 1, 0, 0, 1, 1, 1, 0, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

			KeyValuePair<int, uint>[] peaks = analyzer.GetPeaks(data).ToArray();

			Assert.AreEqual(1, peaks.Length);
			Assert.AreEqual(62, peaks[0].Value);
		}

		[Test]
		public void CanFindPeaksWithNoise()
		{
			FftAnalyzer analyzer = new FftAnalyzer(1.5, 3);

			//    * 
			//    ** *
			//  * ****   * 
			//  ******   **
			// *******   **
			// ********  **
			// 123456789012
			//    |      |
			uint[] data = { 0, 2, 4, 3, 6, 5, 4, 5, 1, 0, 0, 4, 3 };

			KeyValuePair<int, uint>[] peaks = analyzer.GetPeaks(data).ToArray();

			Assert.AreEqual(2, peaks.Length);
			Assert.AreEqual(4, peaks[0].Key);
			Assert.AreEqual(6, peaks[0].Value);
			Assert.AreEqual(11, peaks[1].Key);
			Assert.AreEqual(4, peaks[1].Value);
		}
    }
}
