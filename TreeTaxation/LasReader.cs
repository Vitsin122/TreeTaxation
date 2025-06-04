using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TreeTaxation
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;

    public class LasReader : IDisposable
    {
        private BinaryReader _reader;
        private LasHeader _header;

        public LasHeader Header => _header;

        public class LasHeader
        {
            public string FileSignature;       // 4 bytes: "LASF"
            public ushort FileSourceId;
            public ushort GlobalEncoding;
            public uint ProjectIdGuid1;
            public ushort ProjectIdGuid2;
            public ushort ProjectIdGuid3;
            public byte[] ProjectIdGuid4;      // 8 bytes
            public byte VersionMajor;
            public byte VersionMinor;
            public string SystemIdentifier;    // 32 bytes
            public string GeneratingSoftware;  // 32 bytes
            public ushort FileCreationDay;
            public ushort FileCreationYear;
            public ushort HeaderSize;
            public uint OffsetToPointData;
            public uint NumVariableLengthRecords;
            public byte PointDataFormat;
            public ushort PointDataRecordLength;
            public uint NumPointRecords;
            public uint[] NumPointsByReturn;   // 5 elements
            public double XScale, YScale, ZScale;
            public double XOffset, YOffset, ZOffset;
            public double MaxX, MinX, MaxY, MinY, MaxZ, MinZ;
        }

        public class LasPoint
        {
            public int X, Y, Z;               // Raw unscaled values
            public ushort Intensity;
            public byte ReturnNumber;
            public byte NumberOfReturns;
            public byte Classification;
            public byte ScanAngleRank;
            public byte UserData;
            public ushort PointSourceId;
            public double GPSTime;             // Only if format >= 1
            public ushort Red, Green, Blue;   // Only if format >= 2
        }

        public class RealLasPoint
        {
            public double X, Y, Z;               // Raw unscaled values
            public ushort Intensity;
            public byte ReturnNumber;
            public byte NumberOfReturns;
            public byte Classification;
            public byte ScanAngleRank;
            public byte UserData;
            public ushort PointSourceId;
            public double GPSTime;             // Only if format >= 1
            public ushort Red, Green, Blue;   // Only if format >= 2
        }

        public LasReader(string filePath)
        {
            _reader = new BinaryReader(File.OpenRead(filePath));
            ReadHeader();
        }

        private void ReadHeader()
        {
            _header = new LasHeader
            {
                FileSignature = new string(_reader.ReadChars(4)),
                FileSourceId = _reader.ReadUInt16(),
                GlobalEncoding = _reader.ReadUInt16(),
                ProjectIdGuid1 = _reader.ReadUInt32(),
                ProjectIdGuid2 = _reader.ReadUInt16(),
                ProjectIdGuid3 = _reader.ReadUInt16(),
                ProjectIdGuid4 = _reader.ReadBytes(8),
                VersionMajor = _reader.ReadByte(),
                VersionMinor = _reader.ReadByte(),
                SystemIdentifier = new string(_reader.ReadChars(32)).Trim('\0'),
                GeneratingSoftware = new string(_reader.ReadChars(32)).Trim('\0'),
                FileCreationDay = _reader.ReadUInt16(),
                FileCreationYear = _reader.ReadUInt16(),
                HeaderSize = _reader.ReadUInt16(),
                OffsetToPointData = _reader.ReadUInt32(),
                NumVariableLengthRecords = _reader.ReadUInt32(),
                PointDataFormat = _reader.ReadByte(),
                PointDataRecordLength = _reader.ReadUInt16(),
                NumPointRecords = _reader.ReadUInt32(),
                NumPointsByReturn = new uint[5]
                {
                _reader.ReadUInt32(),
                _reader.ReadUInt32(),
                _reader.ReadUInt32(),
                _reader.ReadUInt32(),
                _reader.ReadUInt32()
                },
                XScale = _reader.ReadDouble(),
                YScale = _reader.ReadDouble(),
                ZScale = _reader.ReadDouble(),
                XOffset = _reader.ReadDouble(),
                YOffset = _reader.ReadDouble(),
                ZOffset = _reader.ReadDouble(),
                MaxX = _reader.ReadDouble(),
                MinX = _reader.ReadDouble(),
                MaxY = _reader.ReadDouble(),
                MinY = _reader.ReadDouble(),
                MaxZ = _reader.ReadDouble(),
                MinZ = _reader.ReadDouble()
            };

            // Skip any remaining header bytes
            _reader.BaseStream.Position = _header.OffsetToPointData;
        }

        public IEnumerable<LasPoint> ReadPoints()
        {
            int pointSize = _header.PointDataRecordLength;
            byte[] pointBuffer = new byte[pointSize];

            for (int i = 0; i < _header.NumPointRecords; i++)
            {
                _reader.Read(pointBuffer, 0, pointSize);
                yield return ParsePoint(pointBuffer);
            }
        }

        private LasPoint ParsePoint(byte[] buffer)
        {
            var point = new LasPoint();
            int offset = 0;

            // Common fields for all point formats
            point.X = BitConverter.ToInt32(buffer, offset); offset += 4;
            point.Y = BitConverter.ToInt32(buffer, offset); offset += 4;
            point.Z = BitConverter.ToInt32(buffer, offset); offset += 4;
            point.Intensity = BitConverter.ToUInt16(buffer, offset); offset += 2;
            point.ReturnNumber = (byte)(buffer[offset] & 0x07); // Bits 0-2
            point.NumberOfReturns = (byte)((buffer[offset] >> 3) & 0x07); // Bits 3-5
            offset++;
            point.Classification = buffer[offset]; offset++;
            point.ScanAngleRank = buffer[offset]; offset++;
            point.UserData = buffer[offset]; offset++;
            point.PointSourceId = BitConverter.ToUInt16(buffer, offset); offset += 2;

            // Format-specific fields
            if (_header.PointDataFormat >= 1)
            {
                point.GPSTime = BitConverter.ToDouble(buffer, offset); offset += 8;
            }

            if (_header.PointDataFormat >= 2)
            {
                point.Red = BitConverter.ToUInt16(buffer, offset); offset += 2;
                point.Green = BitConverter.ToUInt16(buffer, offset); offset += 2;
                point.Blue = BitConverter.ToUInt16(buffer, offset); offset += 2;
            }

            return point;
        }

        public void Dispose()
        {
            _reader?.Dispose();
        }

        // Helper method to convert raw coordinates to real values
        public (double x, double y, double z) GetScaledCoordinates(LasPoint point)
        {
            return (
                point.X * _header.XScale + _header.XOffset,
                point.Y * _header.YScale + _header.YOffset,
                point.Z * _header.ZScale + _header.ZOffset
            );
        }
    }
}
