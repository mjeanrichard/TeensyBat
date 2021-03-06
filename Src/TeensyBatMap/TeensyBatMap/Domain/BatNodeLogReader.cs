using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Windows.Storage;

namespace TeensyBatMap.Domain
{
	public class BatNodeLogReader
	{
		private readonly FftAnalyzer _fftAnalyzer;

		public BatNodeLogReader(FftAnalyzer fftAnalyzer)
		{
			_fftAnalyzer = fftAnalyzer;
		}

		public async Task<BatNodeLog> Load(IStorageFile file)
		{
			BatNodeLog log = new BatNodeLog();
			log.LogStart = DateTime.UtcNow;
			using (Stream logStream = await file.OpenStreamForReadAsync())
			{
				using (BinaryReader reader = new BinaryReader(logStream))
				{
					ReadData(log, reader);
				}
			}
			log.CallCount = log.Calls.Count;
			return log;
		}

		private void ReadData(BatNodeLog log, BinaryReader reader)
		{
			RecordTypes recordType = GetNextRecordType(reader);
			if (recordType != RecordTypes.Header)
			{
				throw new InvalidOperationException("No Header found in Log File!");
			}
			ReadHeader(log, reader);

			while (reader.BaseStream.Position < reader.BaseStream.Length)
			{
				recordType = GetNextRecordType(reader);
				switch (recordType)
				{
					case RecordTypes.None:
						break;
					case RecordTypes.Call:
						if (log.Verison == 1)
						{
							ReadCallRecordV1(log, reader);
						}
						else
						{
							ReadCallRecordV2(log, reader);
						}
						break;
					case RecordTypes.Info:
						ReadInfoRecord(log, reader);
						break;
					case RecordTypes.Header:
						BatMapperEvents.Log.LogImportHeaderRecordWithinFile(reader.BaseStream.Position);
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}

		private void ReadCallRecordV1(BatNodeLog log, BinaryReader reader)
		{
			BatCall call = new BatCall();

			call.Duration = reader.ReadUInt32();
			call.StartTimeMs = reader.ReadUInt32();
			call.ClippedSamples = reader.ReadUInt16();
			call.MaxPower = reader.ReadUInt16();
			call.MissedSamples = reader.ReadUInt16();

			call.FftData = reader.ReadBytes(512);

			if (call.Duration > 100000)
			{
				call.Enabled = false;
			}
			AnalyzeFftData(call);

			log.Calls.Add(call);
		}

		private void ReadCallRecordV2(BatNodeLog log, BinaryReader reader)
		{
			BatCall call = new BatCall();

			call.Duration = reader.ReadUInt32();
			call.StartTimeMs = reader.ReadUInt32();
			call.ClippedSamples = reader.ReadUInt16();
			call.MissedSamples = reader.ReadUInt16();

			int powerDataLength = reader.ReadUInt16();
			call.PowerData = reader.ReadBytes(powerDataLength);
			call.MaxPower = call.PowerData.Max();

			call.FftData = reader.ReadBytes(512);

			if (call.Duration > 100000)
			{
				call.Enabled = false;
			}
			AnalyzeFftData(call);

			log.Calls.Add(call);
		}

		private void ReadInfoRecord(BatNodeLog log, BinaryReader reader)
		{
			BatInfo info = new BatInfo();

			info.Time = CreateDate(reader.ReadUInt32());
			info.TimeMs = reader.ReadUInt32();
			info.BatteryVoltage = reader.ReadUInt16();
			info.SampleDuration = reader.ReadUInt16();
			log.Infos.Add(info);
		}

		private void AnalyzeFftData(BatCall call)
		{
            //_fftAnalyzer.Recalculate(call);
		}

		private RecordTypes GetNextRecordType(BinaryReader reader)
		{
			if (reader.BaseStream.Position + 2 >= reader.BaseStream.Length)
			{
				return RecordTypes.None;
			}
			byte[] recordMarker = reader.ReadBytes(2);
			RecordTypes recordType = GetRecordType(recordMarker);
			if (recordType == RecordTypes.None)
			{
				BatMapperEvents.Log.LogImportMissingStartRecordMarker(reader.BaseStream.Position);
				//skip until next recognized marker
				while (reader.BaseStream.Position < reader.BaseStream.Length)
				{
					recordMarker[0] = recordMarker[1];
					recordMarker[1] = reader.ReadByte();
					recordType = GetRecordType(recordMarker);
					if (recordType != RecordTypes.None)
					{
						break;
					}
				}
			}
			return recordType;
		}

		private RecordTypes GetRecordType(byte[] marker)
		{
			if (marker.Length != 2)
			{
				return RecordTypes.None;
			}
			if (marker[0] == 255)
			{
				switch (marker[1])
				{
					case 255:
						return RecordTypes.Call;
					case 244:
						return RecordTypes.Info;
					default:
						return RecordTypes.None;
				}
			}
			// TB in ASCII is 84, 66
			if (marker[0] == 84 && marker[1] == 66)
			{
				return RecordTypes.Header;
			}
			return RecordTypes.None;
		}

		private void ReadHeader(BatNodeLog log, BinaryReader reader)
		{
			byte[] marker = reader.ReadBytes(2);
			if (marker[0] != 76) //  76 -> L
			{
				throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "Ungültiger Marker ({0}).", marker[0]));
			}
			log.Verison = marker[1];
			log.NodeId = reader.ReadByte();
			uint seconds = reader.ReadUInt32();
			DateTime startTime = CreateDate(seconds);
			log.LogStart = startTime;
		}

		private DateTime CreateDate(uint time)
		{
			return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Local).AddSeconds(time);
		}
	}
}