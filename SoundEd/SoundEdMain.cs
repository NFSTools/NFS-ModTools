using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace SoundEd
{
    public partial class SoundEdMain : Form
    {
        public SoundEdMain()
        {
            InitializeComponent();

            openFileDialog.Filter = "Sound Files (*.abk *.gin)|*.abk;*.gin";
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                if (openFileDialog.FileName.EndsWith(".abk"))
                {
                    ReadAbkFile(openFileDialog.FileName);
                }
            }
        }

        private void ReadAbkFile(string path)
        {
            var num1 = 0;
            var num2 = 0;
            var numArray1 = new[] { 0, 240, 460, 392 };
            var numArray2 = new[] { 0, 0, -208, -220 };
            var dictionary = new Dictionary<int, string>
            {
                {11, "SplitB"},
                {128, "Split"},
                {130, "Channels"},
                {131, "Compression"},
                {132, "SampleRate"},
                {133, "NumSamples"},
                {134, "LoopOffset"},
                {135, "LoopLength"},
                {136, "DataStart1"},
                {137, "DataStart2"},
                {146, "BytesPerSample"},
                {160, "SplitCompression"}
            };

            using (var inStream = File.OpenRead(path))
            using (var binaryReader = new BinaryReader(inStream))
            {
                inStream.Seek(32L, SeekOrigin.Begin);
                inStream.Seek(binaryReader.ReadInt32(), SeekOrigin.Begin);
                var position = inStream.Position;
                inStream.Seek(6L, SeekOrigin.Current);
                int numSounds = binaryReader.ReadInt16();
                Console.WriteLine(path + " Sounds: " + numSounds);
                inStream.Seek(12L, SeekOrigin.Current);
                var soundOffsetTable = new long[numSounds];
                for (var index = 0; index < numSounds; ++index)
                    soundOffsetTable[index] = inStream.Position + binaryReader.ReadInt32();
                for (var soundIdx = 0; soundIdx < numSounds; ++soundIdx)
                {
                    var channels = 1;
                    var sampleRate = 22050;
                    var split = 0;
                    Console.WriteLine("---------- Sound " + soundIdx + " ------------");
                    inStream.Seek(soundOffsetTable[soundIdx], SeekOrigin.Begin);
                    if (binaryReader.ReadInt32() == 0x5450) // "PT  "
                    {
                        var totalSamples = 0;
                        var dataStartPoints = new long[2];
                        while (true)
                        {
                            byte chunkId;
                            do
                            {
                                chunkId = binaryReader.ReadByte();
                                if (chunkId == byte.MaxValue) break;
                            }
                            while (chunkId == 252 || chunkId == 253 || chunkId == 254);

                            if (chunkId == byte.MaxValue)
                            {
                                break;
                            }

                            var chunkSize = binaryReader.ReadByte();
                            var chunkValue = 0;
                            for (var j = 0; j < chunkSize; ++j)
                                chunkValue = (chunkValue << 8) + binaryReader.ReadByte();
                            var chunkName = "";
                            if (dictionary.ContainsKey(chunkId))
                                chunkName = " (" + dictionary[chunkId] + ")";
                            if (chunkId == 128)
                                split = chunkValue;
                            if (chunkId == 130)
                                channels = chunkValue;
                            if (chunkId == 132)
                                sampleRate = chunkValue;
                            if (chunkId == 133)
                                totalSamples = chunkValue;
                            if (chunkId == 136)
                                dataStartPoints[0] = chunkValue;
                            if (chunkId == 137)
                                dataStartPoints[1] = chunkValue;
                            Console.WriteLine($"Chunk: {chunkId:X2} - value: {chunkValue}{chunkName}");
                            //Console.WriteLine("Code: " + num4.ToString("X2") + " value: " + num7 + str);
                        }

                        if (dataStartPoints[0] != 0L)
                        {
                            if (channels > 1 && split != 2)
                            {
                                throw new Exception($"Unsupported channel count/split mode: {channels}/{split}");
                            }

                            using (var outStream = File.OpenWrite($"{path}_sound{soundIdx:00}.wav"))
                            using (var bw = new BinaryWriter(outStream))
                            {
                                WriteWavHeader(bw, channels, sampleRate, totalSamples * channels * 2);
                                var channelsData = new short[channels, totalSamples + 28];
                                var channelDataIdxMap = new int[2];
                                for (var channelIndex = 0; channelIndex < channels; ++channelIndex)
                                {
                                    var samplesRead = 0;
                                    inStream.Seek(position + dataStartPoints[channelIndex], SeekOrigin.Begin);
                                    while (samplesRead < totalSamples)
                                    {
                                        var num7 = 28;
                                        if (samplesRead + 28 > totalSamples)
                                            num7 = totalSamples - samplesRead;
                                        var audCode = binaryReader.ReadByte();
                                        if (audCode == 238)
                                        {
                                            num1 = (short)(binaryReader.ReadByte() << 8 | binaryReader.ReadByte());
                                            num2 = (short)(binaryReader.ReadByte() << 8 | binaryReader.ReadByte());
                                            for (var index3 = 0; index3 < num7; ++index3)
                                                channelsData[channelIndex, channelDataIdxMap[channelIndex]++] = (short)(binaryReader.ReadByte() << 8 | binaryReader.ReadByte());
                                        }
                                        else
                                        {
                                            var index3 = audCode >> 4 & 15;
                                            var num9 = audCode & 15;
                                            for (var index4 = 0; index4 < 14; ++index4)
                                            {
                                                var num10 = binaryReader.ReadByte();
                                                var num11 = (num10 & 0xF0) >> 4;
                                                if (num11 > 7)
                                                    num11 -= 16;
                                                var num12 = num1 * numArray1[index3] + num2 * numArray2[index3] + (num11 << 20 - num9) + 128 >> 8;
                                                if (num12 > short.MaxValue)
                                                    num12 = short.MaxValue;
                                                else if (num12 < short.MinValue)
                                                    num12 = short.MinValue;
                                                channelsData[channelIndex, channelDataIdxMap[channelIndex]++] = (short)num12;
                                                var num13 = num1;
                                                var num14 = num12;
                                                var num15 = num10 & 15;
                                                if (num15 > 7)
                                                    num15 -= 16;
                                                var num16 = num14 * numArray1[index3] + num13 * numArray2[index3] + (num15 << 20 - num9) + 128 >> 8;
                                                if (num16 > short.MaxValue)
                                                    num16 = short.MaxValue;
                                                else if (num16 < short.MinValue)
                                                    num16 = short.MinValue;
                                                channelsData[channelIndex, channelDataIdxMap[channelIndex]++] = (short)num16;
                                                num2 = num14;
                                                num1 = num16;
                                            }
                                        }
                                        samplesRead += num7;
                                    }
                                    if (totalSamples != samplesRead)
                                        Console.WriteLine("Warning: actual samples read " + samplesRead + "!=total samples count " + totalSamples);
                                }
                                for (var sampleIdx = 0; sampleIdx < totalSamples; ++sampleIdx)
                                {
                                    for (var channelIdx = 0; channelIdx < channels; ++channelIdx)
                                        bw.Write(channelsData[channelIdx, sampleIdx]);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void WriteWavHeader(BinaryWriter bw, int channels, int sampleRate, int totalBytes)
        {
            bw.Write('R');
            bw.Write('I');
            bw.Write('F');
            bw.Write('F');
            bw.Write(totalBytes + 36);
            bw.Write('W');
            bw.Write('A');
            bw.Write('V');
            bw.Write('E');
            bw.Write('f');
            bw.Write('m');
            bw.Write('t');
            bw.Write(' ');
            bw.Write(16);
            bw.Write((short)1);
            bw.Write((short)channels);
            bw.Write(sampleRate);
            bw.Write(sampleRate * channels * 2);
            bw.Write((short)(channels * 2));
            bw.Write((short)16);
            bw.Write('d');
            bw.Write('a');
            bw.Write('t');
            bw.Write('a');
            bw.Write(totalBytes);
        }
    }
}
