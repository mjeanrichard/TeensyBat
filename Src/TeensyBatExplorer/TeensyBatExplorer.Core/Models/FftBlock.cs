namespace TeensyBatExplorer.Core.Models
{
    public class FftBlock
    {
        public int Id { get; set; }
        public int Index { get; set; }
        public int Loudness { get; set; }
        public int SampleNr { get; set; }
        public byte[] Data { get; set; }

        public BatCall Call{ get; set; }
    }
}