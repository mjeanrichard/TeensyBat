using Microsoft.Diagnostics.Tracing;

namespace TeensyBatMap
{
	public sealed class BatMapperEvents : EventSource
	{
		public static BatMapperEvents Log = new BatMapperEvents();

		[Event(1000, Message = "Missing Record Start Marker at position {0}. Skipping unit next marker.", Level = EventLevel.Warning)]
		public void LogImportMissingStartRecordMarker(long position)
		{
			WriteEvent(1000, position);
		}
	}
}