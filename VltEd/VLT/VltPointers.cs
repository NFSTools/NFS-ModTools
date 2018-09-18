using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace VltEd.VLT
{
    public class VltPointers : VltBase
    {
        private List<VltPointerBlock> _allBlocks;
        private Hashtable _vltBlocks;
        private List<VltPointerBlock> _rawBlocks;

        public VltPointerBlock this[int offset]
        {
            get => _vltBlocks[offset] as VltPointerBlock;
            set => _vltBlocks[offset] = value;
        }

        public void ResolveRawPointers(Stream stream)
        {
            var bw = new BinaryWriter(stream);
            foreach (var bk in _rawBlocks)
            {
                bw.BaseStream.Seek(bk.OffsetSource, SeekOrigin.Begin);
                bw.Write(bk.OffsetDest);
            }

            bw.Dispose();
        }

        public override void Read(BinaryReader br)
        {
            _allBlocks = new List<VltPointerBlock>();
            _vltBlocks = new Hashtable();
            _rawBlocks = new List<VltPointerBlock>();
            var loadVlt = false;
            var loadRaw = false;
            while (true)
            {
                var bk = new VltPointerBlock();
                bk.Read(br);
                if (bk.Type != VltPointerBlock.BlockType.Load)
                {
                    Debug.Write($"{br.BaseStream.Position - 0xC:x}\t");
                    Debug.WriteLine(bk.ToString());
                }
                _allBlocks.Add(bk);
                if (bk.Type == VltPointerBlock.BlockType.Switch && (bk.Identifier == 0 || bk.Identifier == 1))
                {
                    if (bk.Identifier == 1)
                    {
                        loadVlt = false;
                        loadRaw = true;
                    }
                    else if (bk.Identifier == 0)
                    {
                        loadVlt = true;
                        loadRaw = false;
                    }
                }
                else if (bk.Type == VltPointerBlock.BlockType.RuntimeLink)
                {
                    // Linked at runtime.
                    if (loadVlt)
                        _vltBlocks[bk.OffsetSource] = bk;
                    if (loadRaw)
                        _rawBlocks.Add(bk);
                }
                else if (bk.Type == VltPointerBlock.BlockType.Load && bk.Identifier == 1)
                {
                    if (loadVlt)
                        _vltBlocks[bk.OffsetSource] = bk;
                    if (loadRaw)
                        _rawBlocks.Add(bk);
                }
                else if (bk.Type == VltPointerBlock.BlockType.Done)
                {
                    break;
                }
                else
                {
                    throw new Exception("Unknown ptr type.");
                }
            }
        }

        public override void Write(BinaryWriter bw)
        {
            foreach (var bk in _allBlocks)
                bk.Write(bw);
        }
    }
}
