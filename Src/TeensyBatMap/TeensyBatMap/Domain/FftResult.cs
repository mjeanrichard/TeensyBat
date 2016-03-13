namespace TeensyBatMap.Domain
{
	public class FftResult
	{
		public uint DcOffset { get; set; }
		public uint[] FftData { get; set; }
		public int[] Peaks { get; set; }
	}
}