using System;
using OpenTabletDriver.Configurations.Parsers.Summagraphics;
using Xunit;

namespace OpenTabletDriver.Tests.ConfigurationTest
{
    public class SummaSketchMMParserTests
    {
        private static byte[] BuildPacket(int x, int y, bool inProximity, int flagBits,
                                          bool xPositive = true, bool yPositive = true,
                                          bool tabletId = false)
        {
            if (x < 0 || x > 0x3FFF) throw new ArgumentOutOfRangeException(nameof(x));
            if (y < 0 || y > 0x3FFF) throw new ArgumentOutOfRangeException(nameof(y));
            if (flagBits < 0 || flagBits > 7) throw new ArgumentOutOfRangeException(nameof(flagBits));

            byte b0 = 0x80;
            if (!inProximity) b0 |= 0x40;
            if (tabletId)     b0 |= 0x20;
            if (xPositive)    b0 |= 0x10;
            if (yPositive)    b0 |= 0x08;
            b0 |= (byte)(flagBits & 0x07);

            byte b1 = (byte)(x & 0x7F);
            byte b2 = (byte)((x >> 7) & 0x7F);
            byte b3 = (byte)(y & 0x7F);
            byte b4 = (byte)((y >> 7) & 0x7F);

            return new byte[] { b0, b1, b2, b3, b4 };
        }

        [Fact]
        public void IsFirstByte_PhasingBitSet_ReturnsTrue()
        {
            Assert.True(SummaSketchMMParser.IsFirstByte(0x80));
            Assert.True(SummaSketchMMParser.IsFirstByte(0xFF));
        }

        [Fact]
        public void IsFirstByte_PhasingBitClear_ReturnsFalse()
        {
            Assert.False(SummaSketchMMParser.IsFirstByte(0x00));
            Assert.False(SummaSketchMMParser.IsFirstByte(0x7F));
        }

        [Fact]
        public void IsValidFraming_CorrectPattern_ReturnsTrue()
        {
            var packet = BuildPacket(100, 200, true, 0);
            Assert.True(SummaSketchMMParser.IsValidFraming(packet));
        }

        [Fact]
        public void IsValidFraming_FirstByteMissingPhasing_ReturnsFalse()
        {
            var packet = BuildPacket(100, 200, true, 0);
            packet[0] &= 0x7F;
            Assert.False(SummaSketchMMParser.IsValidFraming(packet));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        public void IsValidFraming_DataByteHasPhasingBit_ReturnsFalse(int byteIndex)
        {
            var packet = BuildPacket(100, 200, true, 0);
            packet[byteIndex] |= 0x80;
            Assert.False(SummaSketchMMParser.IsValidFraming(packet));
        }

        [Fact]
        public void IsValidFraming_ShortBuffer_ReturnsFalse()
        {
            Assert.False(SummaSketchMMParser.IsValidFraming(new byte[] { 0x80, 0x00, 0x00, 0x00 }));
        }

        [Fact]
        public void Parse_Origin_ReturnsZero()
        {
            var packet = BuildPacket(0, 0, true, 0);
            var report = SummaSketchMMParser.Parse(packet);
            Assert.Equal(0, report.X);
            Assert.Equal(0, report.Y);
        }

        [Fact]
        public void Parse_MaxCoordinates_RoundTrip()
        {
            var packet = BuildPacket(0x3FFF, 0x3FFF, true, 0);
            var report = SummaSketchMMParser.Parse(packet);
            Assert.Equal(0x3FFF, report.X);
            Assert.Equal(0x3FFF, report.Y);
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(127, 127)]
        [InlineData(128, 128)]
        [InlineData(1000, 2000)]
        [InlineData(12000, 12000)]
        [InlineData(0x2000, 0x1000)]
        public void Parse_ArbitraryCoordinates_RoundTrip(int x, int y)
        {
            var packet = BuildPacket(x, y, true, 0);
            var report = SummaSketchMMParser.Parse(packet);
            Assert.Equal(x, report.X);
            Assert.Equal(y, report.Y);
        }

        [Fact]
        public void Parse_XAndYAreIndependent()
        {
            var packet = BuildPacket(0x3FFF, 0, true, 0);
            var report = SummaSketchMMParser.Parse(packet);
            Assert.Equal(0x3FFF, report.X);
            Assert.Equal(0, report.Y);

            packet = BuildPacket(0, 0x3FFF, true, 0);
            report = SummaSketchMMParser.Parse(packet);
            Assert.Equal(0, report.X);
            Assert.Equal(0x3FFF, report.Y);
        }

        [Fact]
        public void Parse_InProximity_IsTrue()
        {
            var report = SummaSketchMMParser.Parse(BuildPacket(0, 0, inProximity: true, 0));
            Assert.True(report.InProximity);
        }

        [Fact]
        public void Parse_OutOfProximity_IsFalse()
        {
            var report = SummaSketchMMParser.Parse(BuildPacket(0, 0, inProximity: false, 0));
            Assert.False(report.InProximity);
        }

        [Fact]
        public void Parse_SignBits_DefaultPositive()
        {
            var report = SummaSketchMMParser.Parse(BuildPacket(100, 200, true, 0));
            Assert.True(report.XPositive);
            Assert.True(report.YPositive);
        }

        [Fact]
        public void Parse_NegativeSignBits_AreReported()
        {
            var packet = BuildPacket(100, 200, true, 0, xPositive: false, yPositive: false);
            var report = SummaSketchMMParser.Parse(packet);
            Assert.False(report.XPositive);
            Assert.False(report.YPositive);
        }

        [Fact]
        public void Parse_TabletId_IsReported()
        {
            var withId    = SummaSketchMMParser.Parse(BuildPacket(0, 0, true, 0, tabletId: true));
            var withoutId = SummaSketchMMParser.Parse(BuildPacket(0, 0, true, 0, tabletId: false));
            Assert.True(withId.TabletId);
            Assert.False(withoutId.TabletId);
        }

        [Theory]
        [InlineData(0b000, 0b0000)]
        [InlineData(0b001, 0b0001)]
        [InlineData(0b010, 0b0010)]
        [InlineData(0b011, 0b0100)]
        [InlineData(0b100, 0b1000)]
        [InlineData(0b101, 0b1001)]
        [InlineData(0b110, 0b1010)]
        [InlineData(0b111, 0b1011)]
        public void DecodeButtons_MatchesAppendixH(int flagBits, int expected)
        {
            Assert.Equal(expected, SummaSketchMMParser.DecodeButtons(flagBits));
        }

        [Fact]
        public void Parse_StylusTipButton_IsButton1()
        {
            var report = SummaSketchMMParser.Parse(BuildPacket(0, 0, true, 0b001));
            Assert.Equal(0b0001, report.Buttons);
        }

        [Fact]
        public void Parse_StylusBarrelButton_IsButton2()
        {
            var report = SummaSketchMMParser.Parse(BuildPacket(0, 0, true, 0b010));
            Assert.Equal(0b0010, report.Buttons);
        }

        [Fact]
        public void Parse_RawFlagBitsArePreserved()
        {
            var report = SummaSketchMMParser.Parse(BuildPacket(0, 0, true, 0b101));
            Assert.Equal(0b101, report.RawFlagBits);
        }

        [Fact]
        public void Parse_ShortBuffer_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
                SummaSketchMMParser.Parse(new byte[] { 0x80, 0x00, 0x00, 0x00 }));
        }

        [Fact]
        public void Parse_BadFraming_Throws()
        {
            var packet = BuildPacket(100, 200, true, 0);
            packet[2] |= 0x80;
            Assert.Throws<ArgumentException>(() => SummaSketchMMParser.Parse(packet));
        }

        [Fact]
        public void TryParse_BadFraming_ReturnsFalse()
        {
            var packet = BuildPacket(100, 200, true, 0);
            packet[2] |= 0x80;
            Assert.False(SummaSketchMMParser.TryParse(packet, out _));
        }

        [Fact]
        public void TryParse_GoodPacket_ReturnsTrueWithCorrectData()
        {
            var packet = BuildPacket(1234, 5678, true, 0b001);
            Assert.True(SummaSketchMMParser.TryParse(packet, out var report));
            Assert.Equal(1234, report.X);
            Assert.Equal(5678, report.Y);
            Assert.True(report.InProximity);
            Assert.Equal(0b0001, report.Buttons);
        }

        [Fact]
        public void Parse_HandCraftedGoldenPacket()
        {
            // PH=1 PR=0 T=0 Sx=1 Sy=1 Fc=0 Fb=0 Fa=1 -> 0x99
            // X = (2<<7)|1 = 257, Y = (4<<7)|2 = 514
            byte[] packet = { 0x99, 0x01, 0x02, 0x02, 0x04 };

            var report = SummaSketchMMParser.Parse(packet);

            Assert.Equal(257, report.X);
            Assert.Equal(514, report.Y);
            Assert.True(report.InProximity);
            Assert.True(report.XPositive);
            Assert.True(report.YPositive);
            Assert.False(report.TabletId);
            Assert.Equal(0b0001, report.Buttons);
            Assert.Equal(0b001, report.RawFlagBits);
        }
    }
}
