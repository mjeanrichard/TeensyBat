using System;
using System.Collections.Generic;

namespace TeensyBatMap.Domain
{
	public class BatNodeLog
	{
		private readonly List<BatCall> _calls;

		public BatNodeLog()
		{
			_calls = new List<BatCall>();
		}

		public double Longitude { get; set; }
		public double Latitude { get; set; }

		public DateTime LogStart { get; set; }

		public int Id { get; set; }

		public string Name { get; set; }

		public string Description { get; set; }

		public List<BatCall> Calls
		{
			get { return _calls; }
		}

		public BatCall FirstCall { get; set; }

		public BatCall LastCall { get; set; }

		public int CallCount { get; set; }
		public int NodeId { get; set; }
	}
}