using System;
using System.Collections.Generic;
using System.Linq;
using TeensyBatMap.Database;

namespace TeensyBatMap.Domain
{
    [Table("BatNodeLog")]
    public class BatNodeLog
    {
        private List<BatCall> _calls;
        private int _id;

        public BatNodeLog()
        {
            _calls = new List<BatCall>();
        }

        public double Longitude { get; set; }
        public double Latitude { get; set; }

        [NotNull]
        public DateTime LogStart { get; set; }

        [PrimaryKey]
        [Unique]
        [AutoIncrement]
        public int Id
        {
            get { return _id; }
            set
            {
                _id = value;
                foreach (BatCall call in Calls)
                {
                    call.BatNodeLogId = value;
                }
            }
        }

        [NotNull]
        public string Name { get; set; }

        public string Description { get; set; }

        public IEnumerable<BatCall> Calls
        {
            get { return _calls; }
        }

        [Ignore]
        public BatCall FirstCall { get; set; }

        [Ignore]
        public BatCall LastCall { get; set; }

        public int CallCount { get; set; }

        public void SetCalls(IEnumerable<BatCall> calls)
        {
            _calls = calls.OrderBy(c => c.StartTimeMs).ToList();
            FirstCall = _calls.First();
            LastCall = _calls[_calls.Count - 1];
            CallCount = _calls.Count;
        }
    }
}