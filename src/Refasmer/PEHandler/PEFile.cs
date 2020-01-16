using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PEHandler;

namespace JetBrains.Refasmer.PEHandler
{
    /// <summary>
    /// Trace logging delegate.
    /// </summary>
    /// <param name="msg">message to log</param>
    public delegate void LogTrace(string msg);

    /// <summary>
    /// A generic PE EXE handling class.
    /// </summary>
    public class PEFile
    {
        /// <summary>
        /// Default no-operation trace logging delegate.
        /// </summary>
        public static LogTrace NullTrace { get; private set; } = (string msg) => { };

        /// <summary>
        /// Trace logging delegate.
        /// </summary>
        internal static LogTrace trace = NullTrace;

        /// <summary>
        /// Trace logging delegate.
        /// </summary>
        public static LogTrace Trace { get => trace; set => trace = value ?? NullTrace; }

        /// <summary>
        /// The PE header.
        /// </summary>
        public readonly byte[] earlyHeader;

        /// <summary>
        /// Stream for the PE header.
        /// </summary>
        public readonly MemoryStream earlyHeaderMS;

        /// <summary>
        /// Reader for the PE header.
        /// </summary>
        public readonly BinaryReader earlyHeaderReader;

        /// <summary>
        /// Writer for the PE heaader.
        /// </summary>
        public readonly BinaryWriter earlyHeaderWriter;

        /// <summary>
        /// The list of sections.
        /// </summary>
        public List<Section> Sections { get; private set; }

        /// <summary>
        /// <see cref="PEHandler.RsrcHandler"/> instance for modifying the contents of the ".rsrc" section.
        /// <para>If null, this EXE does not have a ".rsrc" section.</para>
        /// </summary>
        public RsrcHandler RsrcHandler { get; private set; }

        /// <summary>
        /// The total file size of the EXE.
        /// </summary>
        public long FileSize { get; private set; }

        /// <summary>
        /// Creates a new <see cref="PEFile"/>, with data from the supplied <see cref="Stream"/>.
        /// </summary>
        /// <param name="src">stream to read EXE information from</param>
        /// <param name="expectedTex">expected size of headers</param>
        /// <exception cref="IOException">Thrown if the specified stream does not supply valid EXE information.</exception>
        public PEFile(Stream src, uint expectedTex = 0x1000)
        {
            Sections = new List<Section>();
            src.Seek(0, SeekOrigin.Begin);
            earlyHeader = new byte[expectedTex];
            src.Read(earlyHeader, 0, (int)expectedTex);
            earlyHeaderMS = new MemoryStream(earlyHeader);
            earlyHeaderReader = new BinaryReader(earlyHeaderMS, Encoding.UTF8, true);
            earlyHeaderWriter = new BinaryWriter(earlyHeaderMS, Encoding.UTF8, true);
            earlyHeaderMS.Seek(NtHeaders, SeekOrigin.Begin);
            if (earlyHeaderReader.ReadUInt32() != 0x00004550)
                throw new IOException("Not a valid PE file.");
            earlyHeaderMS.Seek(2, SeekOrigin.Current); // short: machine
            uint sectionCount = (uint)(earlyHeaderReader.ReadUInt16() & 0xFFFF);
            earlyHeaderMS.Seek(8, SeekOrigin.Current); // int 1: unknown, int 2: symtab address
            if (earlyHeaderReader.ReadUInt32() != 0)
                throw new IOException(
                    "This file was linked with a symbol table. Since we don't want to accidentally destroy it, you get this error instead.");
            uint optHeadSize = (uint)(earlyHeaderReader.ReadUInt16() & 0xFFFF);
            earlyHeaderMS.Seek(2, SeekOrigin.Current); // short: characteristics
            // -- optional header --
            uint optHeadPoint = (uint)earlyHeaderMS.Position;
            if (optHeadSize < 0x78)
                throw new IOException("Optional header size is under 0x78 (RESOURCE table info)");
            ushort optHeadType = earlyHeaderReader.ReadUInt16();
            if (optHeadType != 0x010B)
                throw new IOException("Unknown optional header type: " + optHeadType.ToString("X"));
            OptionalHeaderInts = new PEOptionalHeaderInts(this);
            // Check that size of headers is what we thought
            if (HeaderSize != expectedTex)
                throw new IOException("Size of headers must be as expected due to linearization fun");
            // Everything verified - load up the image sections
            src.Seek(optHeadPoint + optHeadSize, SeekOrigin.Begin);
            for (uint i = 0; i < sectionCount; i++)
            {
                Section s = new Section();
                s.Read(src);
                Sections.Add(s);
            }
            Test();
            Trace($"File size is 0x{FileSize:X}");
            // Finally, initialize the .rsrc handler
            if (ResourcesIndex > 0)
                RsrcHandler = new RsrcHandler(this);
        }

        /// <summary>
        /// Validates the PE data.
        /// </summary>
        /// <param name="justOrderAndOverlap">wheter to just check for overlapping sections or to perform a full validation</param>
        /// <returns>file size (or -1 if justOrderAndOverlap is set</returns>
        private int Test(bool justOrderAndOverlap = false)
        {
            // You may be wondering: "Why so many passes?"
            // The answer: It simplifies the code.

            // -- Test virtual integrity
            Sections.Sort((x, y) => x.CompareTo(y));
            // Sets the minimum RVA we can use. This works due to the virtual sorting stuff.
            uint rvaHeaderFloor = 0;
            foreach (Section s in Sections)
            {
                if (s.VirtualAddress < rvaHeaderFloor)
                    throw new IOException("Section RVA Overlap, " + s);
                rvaHeaderFloor = s.VirtualAddress + s.VirtualSize;
            }
            if (justOrderAndOverlap)
                return -1;
            // -- Allocate file addresses
            List<AllocationSpan> map = new List<AllocationSpan>
            {
                // Disallow collision with the primary header
                new AllocationSpan(0, (uint)earlyHeader.Length)
            };
            for (int i = 0; i < 2; i++)
            {
                foreach (Section s in Sections)
                {
                    if (s.MetaLinearize != (i == 0))
                        continue;
                    bool ok = false;
                    if (s.MetaLinearize)
                    {
                        ok = CheckAllocation(map, new AllocationSpan(s.VirtualAddress, (uint)s.RawData.Length));
                        s.FileAddress = s.VirtualAddress;
                    }
                    if (!ok)
                    {
                        uint position = 0;
                        while (!CheckAllocation(map, new AllocationSpan(position, (uint)s.RawData.Length)))
                            position += FileAlignment;
                        s.FileAddress = position;
                    }
                }
            }
            // -- Set Section Count / Rewrite section headers
            // 4: signature
            // 2: field: number of sections
            earlyHeaderMS.Seek(NtHeaders + 4 + 2, SeekOrigin.Begin);
            earlyHeaderWriter.Write((ushort)Sections.Count);
            earlyHeaderMS.Seek(SectionHeaders, SeekOrigin.Begin);
            foreach (Section s in Sections)
                s.WriteHead(earlyHeaderMS);
            // -- Image size is based on virtual size, not phys.
            uint imageSize = ImageSize;
            OptionalHeaderInts[0x38] = AlignForward(imageSize, SectionAlignment);
            // -- File size based on the allocation map
            uint fileSize = 0;
            foreach (AllocationSpan allocSpan in map)
            {
                uint v = allocSpan.start + allocSpan.length;
                if (v > fileSize)
                    fileSize = v;
            }
            return (int)(FileSize = AlignForward(fileSize, FileAlignment));
        }

        /// <summary>
        /// Gets the image size.
        /// </summary>
        public uint ImageSize
        {
            get
            {
                uint imageSize = 0;
                foreach (Section s in Sections)
                {
                    uint v = s.VirtualAddress + s.VirtualSize;
                    if (v > imageSize)
                        imageSize = v;
                }
                return imageSize;
            }
        }

        /// <summary>
        /// Checks if an <see cref="AllocationSpan"/> collides with any other spans in a list. If there are no collisions, the new span is added to the list.
        /// </summary>
        /// <param name="map">span list</param>
        /// <param name="newSpan">new span to check</param>
        /// <returns>true if there were no collisions, false otherwise</returns>
        private bool CheckAllocation(List<AllocationSpan> map, AllocationSpan newSpan)
        {
            foreach (AllocationSpan allocSpan in map)
                if (allocSpan.Collides(newSpan))
                    return false;
            map.Add(newSpan);
            return true;
        }

        /// <summary>
        /// Aligns a virtual address forward, meaning the virtual address will be increased in order to align it.
        /// </summary>
        /// <param name="virtualAddrRelative">address to align</param>
        /// <param name="fileAlignment">file alignment factor</param>
        /// <returns>aligned address</returns>
        public static uint AlignForward(uint virtualAddrRelative, uint fileAlignment)
        {
            uint mod = virtualAddrRelative % fileAlignment;
            if (mod != 0)
                virtualAddrRelative += fileAlignment - mod;
            return virtualAddrRelative;
        }

        /// <summary>
        /// Writes a valid EXE file from the data specified in this instance.
        /// </summary>
        /// <returns>EXE file buffer</returns>
        public byte[] Write()
        {
            // rewrite .rsrc
            RsrcHandler?.Write();
            byte[] data = new byte[Test()];
            Trace($"Data size is 0x{data.Length:X}");
            MemoryStream d = new MemoryStream(data);
            d.Write(earlyHeader, 0, earlyHeader.Length);
            foreach (Section s in Sections)
            {
                d.Seek(s.FileAddress, SeekOrigin.Begin);
                d.Write(s.RawData, 0, s.RawData.Length);
            }
            d.Dispose();
            return data;
        }

        /// <summary>
        /// Gets the position of the PE signature, which is the start of the PE data.
        /// </summary>
        public uint NtHeaders
        {
            get
            {
                earlyHeaderMS.Seek(0x3C, SeekOrigin.Begin);
                return earlyHeaderReader.ReadUInt32();
            }
        }

        /// <summary>
        /// Gets the position of the start of the section headers.
        /// </summary>
        public uint SectionHeaders
        {
            get
            {
                uint nt = NtHeaders;
                // 4: Signature
                // 0x10: Field : Size of Optional Header
                earlyHeaderMS.Seek(nt + 4 + 0x10, SeekOrigin.Begin);
                return (uint)(nt + 4 + 0x14 + (earlyHeaderReader.ReadUInt16() & 0xFFFF));
            }
        }

        /// <summary>
        /// Handles getting and setting integer fields in the optional header.
        /// </summary>
        public class PEOptionalHeaderInts
        {
            private readonly PEFile file;

            internal PEOptionalHeaderInts(PEFile file) => this.file = file;

            private void SetOptHeaderIntPos(uint ofs)
            {
                // 0x18: Signature + IMAGE_FILE_HEADER
                file.earlyHeaderMS.Seek(file.NtHeaders + 0x18 + ofs, SeekOrigin.Begin);
            }

            /// <summary>
            /// Gets or sets a field in the optional header.
            /// </summary>
            /// <param name="ofs">offset of field</param>
            /// <returns>value of field</returns>
            public uint this[uint ofs]
            {
                get
                {
                    SetOptHeaderIntPos(ofs);
                    return file.earlyHeaderReader.ReadUInt32();
                }
                set
                {
                    SetOptHeaderIntPos(ofs);
                    file.earlyHeaderWriter.Write(ofs);
                }
            }
        }

        /// <summary>
        /// <see cref="PEOptionalHeaderInts"/> instance for getting or setting integer fields in the optional header.
        /// </summary>
        public PEOptionalHeaderInts OptionalHeaderInts { get; private set; }

        /// <summary>
        /// Image base.
        /// </summary>
        public uint ImageBase => OptionalHeaderInts[0x1C];

        /// <summary>
        /// Section alignment.
        /// </summary>
        public uint SectionAlignment => OptionalHeaderInts[0x20];

        /// <summary>
        /// File alignment.
        /// </summary>
        public uint FileAlignment => OptionalHeaderInts[0x24];

        /// <summary>
        /// Header size.
        /// </summary>
        public uint HeaderSize => OptionalHeaderInts[0x3C];

        /// <summary>
        /// Creates a <see cref="MemoryStream"/> using the specified RVA point.
        /// <para>The returned stream will be based on the raw data of the section that contains the specified RVA point, and will be positioned at that RVA point.</para>
        /// <para>If no section contains the specified RVA point, null will be returned instead.</para>
        /// </summary>
        /// <param name="rva">RVA point</param>
        /// <returns>stream at that RVA point, or null if no section contains that RVA point</returns>
        public MemoryStream SetupRVAPoint(uint rva)
        {
            foreach (Section s in Sections)
            {
                if (rva >= s.VirtualAddress)
                {
                    uint rel = rva - s.VirtualAddress;
                    if (rel < Math.Max((uint)s.RawData.Length, s.VirtualSize))
                    {
                        MemoryStream ms = new MemoryStream(s.RawData);
                        ms.Seek(rel, SeekOrigin.Begin);
                        return ms;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Gets the index of the ".rsrc" section in the section list.
        /// <para>If this EXE does not have a ".rsrc" section, -1 is returned instead.</para>
        /// </summary>
        public int ResourcesIndex
        {
            get
            {
                int idx = 0;
                uint rsrcRVA = OptionalHeaderInts[0x70];
                foreach (Section s in Sections)
                {
                    if (s.VirtualAddress == rsrcRVA)
                        return idx;
                    idx++;
                }
                return -1;
            }
        }

        /// <summary>
        /// Gets the index of the section with the specified tag in the section list.
        /// <para>If this EXE does not have a section with such tag, -1 is returned instead.</para>
        /// <para>In the (unlikely) event of multiple sections with the same tag, the index of the tag with the lowest RVA is returned.</para>
        /// </summary>
        /// <returns>index of section with specified tag, or -1 if the EXE does not have such section</returns>
        public int GetSectionIndexByTag(String tag)
        {
            int idx = 0;
            foreach (Section s in Sections)
            {
                if (s.Tag.Equals(tag))
                    return idx;
                idx++;
            }
            return -1;
        }

        /// <summary>
        /// Allocates a new section.
        /// </summary>
        /// <param name="newS">section to allocate</param>
        /// <param name="resortSections">wheter to resort the section list after adding the section</param>
        public void Malloc(Section newS, bool resortSections = true)
        {
            Section rsrcSection = null;
            int rsI = ResourcesIndex;
            if (rsI != -1)
            {
                rsrcSection = Sections.ElementAt(rsI);
                Sections.RemoveAt(rsI);
            }
            MallocInterior(newS, (uint)earlyHeader.Length, SectionAlignment);
            Sections.Add(newS);
            if (rsrcSection != null)
            {
                // rsrcSection has to be the latest section in the file
                uint oldResourcesRVA = rsrcSection.VirtualAddress;
                MallocInterior(rsrcSection, ImageSize, SectionAlignment);
                uint newResourcesRVA = rsrcSection.VirtualAddress;
                rsrcSection.ShiftResourceContents((int)(newResourcesRVA - oldResourcesRVA));
                Sections.Add(rsrcSection);
                OptionalHeaderInts[0x70] = newResourcesRVA;
                OptionalHeaderInts[0x74] = rsrcSection.VirtualSize;
            }
            if (resortSections)
                Sections.Sort((x, y) => x.CompareTo(y));
        }

        /// <summary>
        /// Moves a section by a specific amount, without colliding with other sections.
        /// </summary>
        /// <param name="rsrcSection">section to move</param>
        /// <param name="i">amount to move the section by</param>
        /// <param name="sectionAlignment">section alignment factor</param>
        private void MallocInterior(Section rsrcSection, uint i, uint sectionAlignment)
        {
            while (true)
            {
                i = AlignForward(i, sectionAlignment);
                // Is this OK?
                AllocationSpan allocSpan = new AllocationSpan(i, AlignForward(rsrcSection.VirtualSize, sectionAlignment));
                bool hit = false;
                foreach (Section s in Sections)
                {
                    if (new AllocationSpan(s.VirtualAddress, AlignForward(s.VirtualSize, sectionAlignment))
                            .Collides(allocSpan))
                    {
                        hit = true;
                        break;
                    }
                }
                if (!hit)
                {
                    rsrcSection.VirtualAddress = i;
                    return;
                }
                i += sectionAlignment;
            }
        }

        /// <summary>
        /// Creates a filler section.
        /// </summary>
        /// <param name="num">ID of filler section</param>
        /// <param name="addr">RVA of filler section</param>
        /// <param name="size">size of filler section</param>
        /// <returns></returns>
        public static Section CreateFillerSection(uint num, uint addr, uint size)
        {
            Section filler = new Section
            {
                Tag = ".flr" + num.ToString("X4"),
                VirtualAddress = addr,
                VirtualSize = size,
                Characteristics = SectionCharacteristics.CNT_UNINITIALIZED_DATA | SectionCharacteristics.MEM_READ | SectionCharacteristics.MEM_WRITE
            };
            return filler;
        }

        /// <summary>
        /// Checks if a section is a filler section.
        /// </summary>
        /// <param name="s">section to check</param>
        /// <returns>true if section is filler, false otherwise</returns>
        public static bool SectionIsFiller(Section s)
        {
            string sTag = s.Tag;
            if (sTag.Length == 8 && sTag.StartsWith(".flr"))
            {
                if (!uint.TryParse(sTag.Substring(3), out uint z))
                    return false;
                return s.Characteristics.HasFlag(SectionCharacteristics.CNT_UNINITIALIZED_DATA);
            }
            return false;
        }

        /// <summary>
        /// Removes all filler sections.
        /// </summary>
        public void RemoveFillerSections()
        {
            List<Section> secsToRem = new List<Section>();
            foreach (Section s in Sections)
                if (SectionIsFiller(s))
                    secsToRem.Add(s);
            Sections = Sections.Except(secsToRem).ToList();
        }

        /// <summary>
        /// Fills gaps in the virtual layout with filler sections.
        /// <para>This operation is neccesary for compatability with Windows 10, since it apparently dislikes gaps in the virtual layout and will refuse to run EXEs with them.</para>
        /// </summary>
        public void FillVirtualLayoutGaps()
        {
            // remove previous filler sections first
            RemoveFillerSections();
            // sort sections
            Sections.Sort((x, y) => x.CompareTo(y));
            uint flrNum = 0;
            uint lastAddr = 0;
            List<Section> secsToMalloc = new List<Section>();
            // if a section's RVA does not equal lastAddr, that means there's a gap in the virtual layout
            // for some reason Windows 10 and apparently *only* Windows 10 hates virtual layout gaps, and will refuse to run EXEs with them
            // so we plug the gaps up using uninitialized filler sections (.flrXXXX)
            foreach (Section s in Sections)
            {
                if (lastAddr != 0)
                    if (s.VirtualAddress != lastAddr)
                        // create new filler section
                        secsToMalloc.Add(CreateFillerSection(flrNum++, lastAddr, s.VirtualAddress - lastAddr));
                lastAddr = AlignForward(s.VirtualAddress + s.VirtualSize, SectionAlignment);
            }
            // malloc the new filler sections
            foreach (Section s in secsToMalloc)
                Malloc(s, false);
            // finally, resort the sections list
            Sections.Sort((x, y) => x.CompareTo(y));
        }

        /// <summary>
        /// PE section characteristic flags.
        /// </summary>
        [Flags]
        public enum SectionCharacteristics : uint
        {
            /// <summary>
            /// Reserved for future use.
            /// </summary>
            RESERVED_000 = 0x00000000,

            /// <summary>
            /// Reserved for future use.
            /// </summary>
            RESERVED_001 = 0x00000001,

            /// <summary>
            /// Reserved for future use.
            /// </summary>
            RESERVED_002 = 0x00000002,

            /// <summary>
            /// Reserved for future use.
            /// </summary>
            RESERVED_004 = 0x00000004,

            /// <summary>
            /// The section should not be padded to the next boundary. This flag is obsolete and is replaced by IMAGE_SCN_ALIGN_1BYTES. This is valid only for object files.
            /// </summary>
            TYPE_NO_PAD = 0x00000008,

            /// <summary>
            /// Reserved for future use.
            /// </summary>
            RESERVED_010 = 0x00000010,

            /// <summary>
            /// The section contains executable code.
            /// </summary>
            CNT_CODE = 0x00000020,

            /// <summary>
            /// The section contains initialized data.
            /// </summary>
            CNT_INITIALIZED_DATA = 0x00000040,

            /// <summary>
            /// The section contains uninitialized data.
            /// </summary>
            CNT_UNINITIALIZED_DATA = 0x00000080,

            /// <summary>
            /// Reserved for future use.
            /// </summary>
            LNK_OTHER = 0x00000100,

            /// <summary>
            /// The section contains comments or other information. The .drectve section has this type. This is valid for object files only.
            /// </summary>
            LNK_INFO = 0x00000200,

            /// <summary>
            /// Reserved for future use.
            /// </summary>
            RESERVED_400 = 0x00000400,

            /// <summary>
            /// The section will not become part of the image. This is valid only for object files.
            /// </summary>
            LNK_REMOVE = 0x00000800,

            /// <summary>
            /// The section contains COMDAT data. For more information, see COMDAT Sections (Object Only). This is valid only for object files.
            /// </summary>
            LNK_COMDAT = 0x00001000,

            /// <summary>
            /// The section contains data referenced through the global pointer (GP).
            /// </summary>
            GPREL = 0x00008000,

            /// <summary>
            /// Reserved for future use.
            /// </summary>
            MEM_PURGEABLE = 0x00020000,

            /// <summary>
            /// Reserved for future use.
            /// </summary>
            MEM_16BIT = 0x00020000,

            /// <summary>
            /// Reserved for future use.
            /// </summary>
            MEM_LOCKED = 0x00040000,

            /// <summary>
            /// Reserved for future use.
            /// </summary>
            MEM_PRELOAD = 0x00080000,

            /// <summary>
            /// Align data on a 1-byte boundary. Valid only for object files.
            /// </summary>
            ALIGN_1BYTES = 0x00100000,

            /// <summary>
            /// Align data on a 2-byte boundary. Valid only for object files.
            /// </summary>
            ALIGN_2BYTES = 0x00200000,

            /// <summary>
            /// Align data on a 4-byte boundary. Valid only for object files.
            /// </summary>
            ALIGN_4BYTES = 0x00300000,

            /// <summary>
            /// Align data on an 8-byte boundary. Valid only for object files.
            /// </summary>
            ALIGN_8BYTES = 0x00400000,

            /// <summary>
            /// Align data on a 16-byte boundary. Valid only for object files.
            /// </summary>
            ALIGN_16BYTES = 0x00500000,

            /// <summary>
            /// Align data on a 32-byte boundary. Valid only for object files.
            /// </summary>
            ALIGN_32BYTES = 0x00600000,

            /// <summary>
            /// Align data on a 64-byte boundary. Valid only for object files.
            /// </summary>
            ALIGN_64BYTES = 0x00700000,

            /// <summary>
            /// Align data on a 128-byte boundary. Valid only for object files.
            /// </summary>
            ALIGN_128BYTES = 0x00800000,

            /// <summary>
            /// Align data on a 256-byte boundary. Valid only for object files.
            /// </summary>
            ALIGN_256BYTES = 0x00900000,

            /// <summary>
            /// Align data on a 512-byte boundary. Valid only for object files.
            /// </summary>
            ALIGN_512BYTES = 0x00A00000,

            /// <summary>
            /// Align data on a 1024-byte boundary. Valid only for object files.
            /// </summary>
            ALIGN_1024BYTES = 0x00B00000,

            /// <summary>
            /// Align data on a 2048-byte boundary. Valid only for object files.
            /// </summary>
            ALIGN_2048BYTES = 0x00C00000,

            /// <summary>
            /// Align data on a 4096-byte boundary. Valid only for object files.
            /// </summary>
            ALIGN_4096BYTES = 0x00D00000,

            /// <summary>
            /// Align data on an 8192-byte boundary. Valid only for object files.
            /// </summary>
            ALIGN_8192BYTES = 0x00E00000,

            /// <summary>
            /// The section contains extended relocations.
            /// </summary>
            LNK_NRELOC_OVFL = 0x01000000,

            /// <summary>
            /// The section can be discarded as needed.
            /// </summary>
            MEM_DISCARDABLE = 0x02000000,

            /// <summary>
            /// The section cannot be cached.
            /// </summary>
            MEM_NOT_CACHED = 0x04000000,

            /// <summary>
            /// The section is not pageable.
            /// </summary>
            MEM_NOT_PAGED = 0x08000000,

            /// <summary>
            /// The section can be shared in memory.
            /// </summary>
            MEM_SHARED = 0x10000000,

            /// <summary>
            /// The section can be executed as code.
            /// </summary>
            MEM_EXECUTE = 0x20000000,

            /// <summary>
            /// The section can be read.
            /// </summary>
            MEM_READ = 0x40000000,

            /// <summary>
            /// The section can be written to.
            /// </summary>
            MEM_WRITE = 0x80000000,
        }

        /// <summary>
        /// A PE section.
        /// </summary>
        public class Section : IComparable<Section>, IEquatable<Section>
        {
            /// <summary>
            /// Default characteristic flags.
            /// </summary>
            public static readonly SectionCharacteristics defaultCharacteristics = SectionCharacteristics.CNT_INITIALIZED_DATA | SectionCharacteristics.MEM_EXECUTE | SectionCharacteristics.MEM_READ | SectionCharacteristics.MEM_WRITE;

            /// <summary>
            /// 0-length byte array.
            /// </summary>
            private static readonly byte[] blankRawData = new byte[0];

            /// <summary>
            /// Linearization flag. If true, the virtual address should be equal to the file address.
            /// </summary>
            public bool MetaLinearize { get; set; }

            /// <summary>
            /// The section tag, serialized for writing the section head (<see cref="WriteHead(Stream)"/>).
            /// </summary>
            private readonly byte[] tagData = new byte[8];

            /// <summary>
            /// Gets and sets the section's tag.
            /// </summary>
            public string Tag
            {
                get => Encoding.Default.GetString(tagData);
                set
                {
                    if (value == null)
                        throw new ArgumentNullException(nameof(value));
                    byte[] data = Encoding.Default.GetBytes(value);
                    Array.Copy(data, tagData, 8);
                }
            }

            /// <summary>
            /// Gets and sets the section's virtual size.
            /// </summary>
            public uint VirtualSize { get; set; }

            /// <summary>
            /// Gets and sets the section's virtual address.
            /// </summary>
            public uint VirtualAddress { get; set; }

            /// <summary>
            /// Gets the section's file address.
            /// </summary>
            public uint FileAddress { get; internal set; }

            /// <summary>
            /// The section's raw data.
            /// </summary>
            private byte[] rawData;

            /// <summary>
            /// Gets and sets the section's data.
            /// </summary>
            public byte[] RawData { get => rawData; set => rawData = value ?? blankRawData; }

            /// <summary>
            /// Gets and sets the section's characteristic flags.
            /// </summary>
            public SectionCharacteristics Characteristics { get; set; }

            /// <summary>
            /// Creates a new section with no data and default characteristic flags.
            /// </summary>
            public Section()
            {
                RawData = blankRawData;
                Characteristics = defaultCharacteristics;
            }

            /// <summary>
            /// Reads a section from a stream.
            /// </summary>
            /// <param name="src">stream to read from</param>
            public void Read(Stream src)
            {
                using (BinaryReader r = new BinaryReader(src, Encoding.UTF8, true))
                {
                    r.Read(tagData, 0, 8);
                    VirtualSize = r.ReadUInt32();
                    VirtualAddress = r.ReadUInt32();
                    RawData = new byte[r.ReadUInt32()];
                    FileAddress = r.ReadUInt32();
                    MetaLinearize = FileAddress == VirtualAddress;
                    long saved = src.Position;
                    src.Seek(FileAddress, SeekOrigin.Begin);
                    src.Read(RawData, 0, RawData.Length);
                    src.Seek(saved, SeekOrigin.Begin);
                    src.Seek(8, SeekOrigin.Current); // int 1: unknown, int 2: unknown
                    if (r.ReadUInt16() != 0)
                        throw new IOException("Relocations not allowed");
                    if (r.ReadUInt16() != 0)
                        throw new IOException("Line numbers not allowed");
                    Characteristics = (SectionCharacteristics)r.ReadUInt32();
                }
            }

            /// <summary>
            /// Writes the section header to a stream.
            /// </summary>
            /// <param name="dst">stream to write to</param>
            public void WriteHead(Stream dst)
            {
                using (BinaryWriter w = new BinaryWriter(dst, Encoding.UTF8, true))
                {
                    w.Write(tagData);
                    w.Write(VirtualSize);
                    w.Write(VirtualAddress);
                    w.Write((uint)RawData.Length);
                    w.Write(FileAddress);
                    w.Write((uint)0);
                    w.Write((uint)0);
                    w.Write((ushort)0);
                    w.Write((ushort)0);
                    w.Write((uint)Characteristics);
                }
            }

            /// <summary>
            /// Returns the hash code for this instance.
            /// </summary>
            /// <returns>hash code</returns>
            public override int GetHashCode()
            {
                var hashCode = 1956869319;
                hashCode = hashCode * -1521134295 + MetaLinearize.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Tag);
                hashCode = hashCode * -1521134295 + VirtualSize.GetHashCode();
                hashCode = hashCode * -1521134295 + VirtualAddress.GetHashCode();
                hashCode = hashCode * -1521134295 + FileAddress.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<byte[]>.Default.GetHashCode(RawData);
                hashCode = hashCode * -1521134295 + Characteristics.GetHashCode();
                return hashCode;
            }

            /// <summary>
            /// Determines whether the specified object is equal to the current object.
            /// </summary>
            /// <param name="other">the object to compare with the current object</param>
            /// <returns>true if the specified object is equal to the current object, false otherwise</returns>
            public bool Equals(Section other) => GetHashCode() == other.GetHashCode();

            /// <summary>
            /// Compares the current instance with another object of the same type.
            /// </summary>
            /// <param name="other">an object to compare with this instance</param>
            /// <returns>a value that indicates the relative order of the objects being compared</returns>
            public int CompareTo(Section other)
            {
                if (VirtualAddress < other.VirtualAddress)
                    return -1;
                if (VirtualAddress == other.VirtualAddress)
                    return 0; // Impossible if they are different, but...
                return 1;
            }

            private void ShiftDirTable(MemoryStream ms, int amt, uint pointer)
            {
                BinaryReader r = new BinaryReader(ms, Encoding.UTF8, true);
                BinaryWriter w = new BinaryWriter(ms, Encoding.UTF8, true);
                ms.Seek(pointer + 12, SeekOrigin.Begin);
                // get the # of rsrc subdirs indexed by name
                uint nEntry = r.ReadUInt16();
                // get the # of rsrc subdirs indexed by id
                nEntry += r.ReadUInt16();
                // read and shift entries
                uint pos = pointer + 16;
                for (uint i = 0; i < nEntry; i++)
                    RsrcShift(ms, r, w, amt, pos + i * 8);
                r.Dispose();
                w.Dispose();
            }

            private void RsrcShift(MemoryStream ms, BinaryReader r, BinaryWriter w, int amt, uint pointer)
            {
                ms.Seek(pointer + 4, SeekOrigin.Begin);
                uint rva = r.ReadUInt32();
                if ((rva & 0x80000000) != 0) // if hi bit 1 points to another directory table
                    ShiftDirTable(ms, amt, rva & 0x7FFFFFFF);
                else
                {
                    ms.Seek(rva, SeekOrigin.Begin);
                    uint oldVal = r.ReadUInt32(); ;
                    ms.Seek(rva, SeekOrigin.Begin);
                    w.Write((uint)(oldVal + amt));
                }
            }

            /// <summary>
            /// Shifts the resource directory table by a specific amount.
            /// </summary>
            /// <param name="amt">amount to shift by</param>
            internal void ShiftResourceContents(int amt)
            {
                MemoryStream ms;
                ShiftDirTable(ms = new MemoryStream(RawData), amt, 0);
                ms.Close();
            }

            /// <summary>
            /// Returns a string that represents the current object.
            /// </summary>
            /// <returns>a string that represents the current object</returns>
            public override string ToString() => $"{Tag} : RVA {VirtualAddress:X} : VS {VirtualSize:X} : RDS {RawData.Length} : CH {Characteristics}";
        }

        /// <summary>
        /// A section that starts at a specified position and spans a specified length.
        /// </summary>
        private class AllocationSpan
        {
            /// <summary>
            /// The start position.
            /// </summary>
            public uint start;

            /// <summary>
            /// The length of the span.
            /// </summary>
            public uint length;

            /// <summary>
            /// Creates a new span.
            /// </summary>
            /// <param name="fa">start position</param>
            /// <param name="size">length</param>
            public AllocationSpan(uint fa, uint size)
            {
                start = fa;
                length = size;
            }

            /// <summary>
            /// Checks if this span collides with another span, or vice versa.
            /// </summary>
            /// <param name="other">span to check with</param>
            /// <returns>true if spans collide, false otherwise</returns>
            public bool Collides(AllocationSpan other)
            {
                return Within(other.start) || Within(other.start + other.length - 1) || other.Within(start)
                        || other.Within(start + length - 1);
            }

            /// <summary>
            /// Checks if a point is within this span.
            /// </summary>
            /// <param name="target">point to check</param>
            /// <returns>true if span contains point, false otherwise</returns>
            private bool Within(uint target)
            {
                return (target >= start) && ((start + length) > target);
            }
        }
    }
}