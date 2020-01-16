using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using JetBrains.Refasmer.PEHandler;
using static JetBrains.Refasmer.PEHandler.PEFile;

namespace PEHandler
{
    /// <summary>
    /// An entry in the ".rsrc" section. Can either be identified by a name or an ID, and can either be a data entry or a directory.
    /// </summary>
    public class RsrcEntry
    {
        /// <summary>
        /// Gets the root entry of the ".rsrc" section this entry is contained in. If this is null, this entry is the root entry.
        /// </summary>
        public RsrcEntry Root { get; internal set; }

        /// <summary>
        /// Gets the parent of this entry.
        /// </summary>
        public RsrcEntry Parent { get; internal set; }

        /// <summary>
        /// Parent at the time of <see cref="cachedTopParent"/> last being cached.
        /// </summary>
        private RsrcEntry cachedParent;

        /// <summary>
        /// Cached result for <see cref="TopParent"/>.
        /// </summary>
        private RsrcEntry cachedTopParent;

        /// <summary>
        /// Gets the top-level parent of this entry.
        /// </summary>
        public RsrcEntry TopParent
        {
            get
            {
                if (cachedTopParent == null || cachedParent != Parent)
                {
                    cachedParent = Parent;
                    cachedTopParent = Parent;
                    while (cachedTopParent.Parent != null && cachedTopParent.Parent.Parent != null)
                        cachedTopParent = cachedTopParent.Parent;
                }
                return cachedTopParent;
            }
        }

        /// <summary>
        /// The entry's name. If this is null, use <see cref="ID"/> instead.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The entry's ID.
        /// </summary>
        public uint ID { get; set; }

        /// <summary>
        /// The entry's path name. If <see cref="Name"/> is null, this is the string representation of <see cref="ID"/>.
        /// </summary>
        public string PathName => Name ?? ID.ToString();

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>a string that represents the current object</returns>
        public override string ToString() => PathName;

        /// <summary>
        /// The entry's data. If this is null, this entry is a directory (see <see cref="Entries"/>).
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        /// The entry's subentry list. If this is null, this entry is a data entry (see <see cref="Data"/>.
        /// </summary>
        public LinkedList<RsrcEntry> Entries { get; set; }

        /// <summary>
        /// The entry's codepage. Specific to data entries.
        /// </summary>
        public uint DataCodepage { get; set; }

        /// <summary>
        /// Reserved field. Specific to data entries.
        /// </summary>
        public uint DataReserved { get; set; }

        /// <summary>
        /// The entry's characteristic flags. Specific to directory entries.
        /// </summary>
        public uint DirCharacteristics { get; set; }

        /// <summary>
        /// The entry's timestamp. Specific to directory entries.
        /// </summary>
        public uint DirTimestamp { get; set; }

        /// <summary>
        /// The entry's major version number. Specific to directory entries.
        /// </summary>
        public ushort DirVersionMajor { get; set; }

        /// <summary>
        /// The entry's minor version number. Specific to directory entries.
        /// </summary>
        public ushort DirVersionMinor { get; set; }

        /// <summary>
        /// Creates a new entry with a parent.
        /// </summary>
        /// <param name="parent">parent entry</param>
        internal RsrcEntry(RsrcEntry parent)
        {
            Root = parent?.Root;
            Parent = parent;
        }

        /// <summary>
        /// Returns a path representation of the entry.
        /// </summary>
        /// <returns>path to entry</returns>
        public string ToPath()
        {
            string path = "";
            RsrcEntry entry = this;
            while (entry.Parent != null && entry.Parent.Parent != null)
            {
                path = $"{entry.PathName}/{path}";
                entry = entry.Parent;
            }
            return path.Substring(0, path.Length - 1);
        }

        /// <summary>
        /// Gets wheter the entry is a directory (<see cref="Entries"/> is non-null).
        /// </summary>
        public bool IsDirectory => Entries != null;

        /// <summary>
        /// If <see cref="IsDirectory"/> returns false, throws an exception.
        /// </summary>
        private void AssertIsDirectory()
        {
            if (!IsDirectory)
                throw new InvalidOperationException("This method can't be invoked using a non-directory entry!");
        }

        /// <summary>
        /// Adds an existing entry as a sub-entry.
        /// </summary>
        /// <param name="e">entry to add</param>
        public void AddSubEntry(RsrcEntry e)
        {
            AssertIsDirectory();
            e.Root = Root;
            e.Parent = this;
            Entries.AddLast(e);
        }

        /// <summary>
        /// Creates a new sub-entry.
        /// </summary>
        /// <returns>new entry</returns>
        private RsrcEntry AddSubEntry()
        {
            AssertIsDirectory();
            RsrcEntry e = new RsrcEntry(this);
            Entries.AddLast(e);
            return e;
        }

        /// <summary>
        /// Creates a new sub-entry with the specified name.
        /// </summary>
        /// <param name="name">entry name</param>
        /// <returns>new entry</returns>
        public RsrcEntry AddSubEntry(string name)
        {
            RsrcEntry e = AddSubEntry();
            e.Name = name ?? throw new ArgumentNullException("name");
            return e;
        }

        /// <summary>
        /// Creates a new sub-entry with the specified ID.
        /// </summary>
        /// <param name="id">entry ID</param>
        /// <returns>new entry</returns>
        public RsrcEntry AddSubEntry(uint id)
        {
            RsrcEntry e = AddSubEntry();
            e.ID = id;
            return e;
        }

        /// <summary>
        /// Gets a sub-entry with the specified name. If no entry with the specified name is found, returns null.
        /// </summary>
        /// <param name="name">name to search</param>
        /// <returns>entry with specified name, or null if not found</returns>
        public RsrcEntry GetSubEntry(string name)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            AssertIsDirectory();
            foreach (RsrcEntry entry in Entries)
            {
                if (name.Equals(entry.Name))
                    return entry;
            }
            return null;
        }

        /// <summary>
        /// Checks if this entry has a sub-entry with the specified name.
        /// </summary>
        /// <param name="name">name to check</param>
        /// <returns>true if has entry, false otherwise</returns>
        public bool HasSubEntry(string name)
        {
            return GetSubEntry(name) != null;
        }

        /// <summary>
        /// Gets a sub-entry with the specified ID. If no entry with the specified ID is found, returns null.
        /// </summary>
        /// <param name="id">ID to search</param>
        /// <returns>entry with specified ID, or null if not found</returns>
        public RsrcEntry GetSubEntry(uint id)
        {
            AssertIsDirectory();
            foreach (RsrcEntry entry in Entries)
            {
                if (entry.Name != null)
                    continue;
                if (id == entry.ID)
                    return entry;
            }
            return null;
        }

        /// <summary>
        /// Checks if this entry has a sub-entry with the specified ID.
        /// </summary>
        /// <param name="id">ID to check</param>
        /// <returns>true if has entry, false otherwise</returns>
        public bool HasSubEntry(uint id)
        {
            return GetSubEntry(id) != null;
        }
    }

    /// <summary>
    /// Handles modifying the structure and contents of the ".rsrc" section.
    /// </summary>
    public class RsrcHandler
    {
        /// <summary>
        /// Gets the root entry.
        /// </summary>
        public RsrcEntry Root { get; private set; }

        /// <summary>
        /// Gets an entry from a path representation. If a part of the path doesn't exist or is invalid, null is returned.
        /// </summary>
        /// <param name="path">path to follow</param>
        /// <returns>entry, or null if part of path doesn't exist or is invalid</returns>
        public RsrcEntry GetEntryFromPath(String path)
        {
            path = path.Trim();
            if (path.Length == 0)
                return Root;
            RsrcEntry entry = Root;
            string[] pathParts = path.Split('/');
            for (int i = 0; i < pathParts.Length; i++)
            {
                string pathPart = pathParts[i];
                if (entry.HasSubEntry(pathPart))
                    entry = entry.GetSubEntry(pathPart);
                else
                {
                    bool ok = uint.TryParse(pathPart, out uint id);
                    if (ok)
                        entry = entry.GetSubEntry(id);
                    else
                        return null;
                }
                if (i != pathParts.Length - 1 && !entry.IsDirectory)
                    return null;
            }
            return entry;
        }

        /// <summary>
        /// The PE EXE the ".rsrc" section comes from.
        /// </summary>
        private PEFile srcFile;

        /// <summary>
        /// The ".rsrc" section this instance is handling.
        /// </summary>
        private Section rsrcSec;

        /// <summary>
        /// Creates a new ".rsrc" section handler, handling the ".rsrc" section from the specified <see cref="PEFile"/>.
        /// </summary>
        /// <param name="srcFile"><see cref="PEFile"/> to handle</param>
        /// <exception cref="ArgumentException">Thrown if the specified <see cref="PEFile"/> does not contain a ".rsrc" section.</exception>
        internal RsrcHandler(PEFile srcFile)
        {
            this.srcFile = srcFile;
            int rsI = srcFile.ResourcesIndex;
            if (rsI < 0)
                throw new ArgumentException("srcFile does not have a .rsrc section!");
            rsrcSec = srcFile.Sections.ElementAt(rsI);
            rsrcSec.ShiftResourceContents((int)-rsrcSec.VirtualAddress);
            Root = new RsrcEntry(null)
            {
                Root = null,
                Entries = new LinkedList<RsrcEntry>()
            };
            MemoryStream src = new MemoryStream(rsrcSec.RawData);
            ReadDirectory(src, Root);
            src.Dispose();
            rsrcSec.ShiftResourceContents((int)rsrcSec.VirtualAddress);
        }

        /// <summary>
        /// Writes the ".rsrc" section, and reallocates it to the PE EXE.
        /// </summary>
        public void Write()
        {
            srcFile.Sections.Remove(rsrcSec);
            // 0th romp to calculate section sizes
            SectionSizes sectionSizes = CalculateSectionSizes(Root);
            Trace("Calculated .rsrc section sizes are:");
            Trace($"Directory tables: 0x{sectionSizes.directorySize:X}");
            Trace($"Data entries: 0x{sectionSizes.dataEntrySize:X}");
            Trace($"String definitions: 0x{sectionSizes.stringSize:X}");
            Trace($"Resource data: 0x{sectionSizes.dataSize:X}");
            Trace($"TOTAL: 0x{sectionSizes.TotalSize:X}");
            Trace("-- END --");
            byte[] dstBuf = new byte[sectionSizes.TotalSize];
            MemoryStream dst = new MemoryStream(dstBuf);
            ReferenceMemory refMem = new ReferenceMemory();
            // write directories, leave references blank
            WriteDirectory(dst, Root, refMem);
            // write references
            WriteReferences(dst, sectionSizes, refMem);
            rsrcSec.RawData = dstBuf;
            rsrcSec.VirtualSize = (uint)dstBuf.Length;
            srcFile.Malloc(rsrcSec);
            // update offsets
            uint rsrcSecRVA = rsrcSec.VirtualAddress;
            rsrcSec.ShiftResourceContents((int)rsrcSecRVA);
            srcFile.OptionalHeaderInts[0x70] = rsrcSecRVA;
        }

        /// <summary>
        /// Reads a directory.
        /// </summary>
        /// <param name="src">stream to read from</param>
        /// <param name="root">root entry</param>
        private void ReadDirectory(MemoryStream src, RsrcEntry root)
        {
            using (BinaryReader r = new BinaryReader(src, Encoding.UTF8, true))
            {
                long posStorage = 0;
                root.DirCharacteristics = r.ReadUInt32();
                root.DirTimestamp = r.ReadUInt32();
                root.DirVersionMajor = r.ReadUInt16();
                root.DirVersionMinor = r.ReadUInt16();
                uint entries = r.ReadUInt16();
                entries += r.ReadUInt16();
                for (uint i = 0; i < entries; i++)
                {
                    RsrcEntry entry = new RsrcEntry(root);
                    uint nameOffset = r.ReadUInt32();
                    if ((nameOffset & 0x80000000) == 0)
                        // id
                        entry.ID = nameOffset;
                    else
                    {
                        // name
                        posStorage = src.Position;
                        nameOffset &= 0x7FFFFFFF;
                        src.Seek(nameOffset, SeekOrigin.Begin);
                        ushort nameLen = r.ReadUInt16();
                        char[] nameBuf = new char[nameLen];
                        for (int j = 0; j < nameLen; j++)
                            nameBuf[j] = (char)r.ReadUInt16();
                        entry.Name = new string(nameBuf);
                        src.Seek(posStorage, SeekOrigin.Begin);
                    }
                    ReadEntryData(src, entry);
                    root.Entries.AddLast(entry);
                }
            }
        }

        /// <summary>
        /// Reads an entry's data.
        /// </summary>
        /// <param name="src">stream to read from</param>
        /// <param name="entry">entry to apply data to</param>
        private void ReadEntryData(MemoryStream src, RsrcEntry entry)
        {
            using (BinaryReader r = new BinaryReader(src, Encoding.UTF8, true))
            {
                uint dataOffset = r.ReadUInt32();
                long posStorage = src.Position;
                if ((dataOffset & 0x80000000) == 0)
                {
                    // data
                    src.Seek(dataOffset, SeekOrigin.Begin);
                    uint dataPos = r.ReadUInt32();
                    uint dataSize = r.ReadUInt32();
                    entry.DataCodepage = r.ReadUInt32();
                    entry.DataReserved = r.ReadUInt32();
                    // read the data
                    src.Seek(dataPos, SeekOrigin.Begin);
                    byte[] entryData = new byte[dataSize];
                    src.Read(entryData, 0, (int)dataSize);
                    entry.Data = entryData;
                }
                else
                {
                    // subdirectory
                    dataOffset &= 0x7FFFFFFF;
                    entry.Entries = new LinkedList<RsrcEntry>();
                    src.Seek(dataOffset, SeekOrigin.Begin);
                    ReadDirectory(src, entry);
                }
                src.Seek(posStorage, SeekOrigin.Begin);
            }
        }

        /// <summary>
        /// Stores calculated section sizes.
        /// </summary>
        private struct SectionSizes
        {
            /// <summary>
            /// Size of the directory tables.
            /// </summary>
            public uint directorySize;

            /// <summary>
            /// Size of the data entries.
            /// </summary>
            public uint dataEntrySize;

            /// <summary>
            /// Size of the string definitions.
            /// </summary>
            public uint stringSize;

            /// <summary>
            /// Size of the resource data.
            /// </summary>
            public uint dataSize;

            /// <summary>
            /// Total size of the ".rsrc" section.
            /// </summary>
            public uint TotalSize => directorySize + dataEntrySize + stringSize + dataSize;

            /// <summary>
            /// Combines two <see cref="SectionSizes"/> instances together.
            /// </summary>
            /// <param name="s1">first instance</param>
            /// <param name="s2">second instance</param>
            /// <returns>combined instance</returns>
            public static SectionSizes operator +(SectionSizes s1, SectionSizes s2)
            {
                SectionSizes sizes;
                sizes.directorySize = s1.directorySize + s2.directorySize;
                sizes.dataEntrySize = s1.dataEntrySize + s2.dataEntrySize;
                sizes.stringSize = s1.stringSize + s2.stringSize;
                sizes.dataSize = s1.dataSize + s2.dataSize;
                return sizes;
            }
        }

        /// <summary>
        /// Calculates section sizes.
        /// </summary>
        /// <param name="root">root entry</param>
        /// <param name="allocStr">list of already-allocated strings</param>
        /// <returns>calculated section sizes</returns>
        private SectionSizes CalculateSectionSizes(RsrcEntry root, List<string> allocStr = null)
        {
            if (allocStr == null)
                allocStr = new List<string>();
            SectionSizes sizes;
            sizes.directorySize = 0x10;
            sizes.dataEntrySize = 0;
            sizes.stringSize = 0;
            sizes.dataSize = 0;
            foreach (RsrcEntry entry in root.Entries)
            {
                sizes.directorySize += 8;
                if (entry.Name != null && !allocStr.Contains(entry.Name))
                {
                    allocStr.Add(entry.Name);
                    sizes.stringSize += 2 + (uint)entry.Name.Length * 2;
                }
                if (entry.Data != null)
                {
                    sizes.dataEntrySize += 0x10;
                    sizes.dataSize += (uint)entry.Data.Length;
                }
                else if (entry.IsDirectory)
                    sizes += CalculateSectionSizes(entry, allocStr);
                else
                    throw new Exception("Entry has no data nor any subentries: " + entry.ToPath());
            }
            return sizes;
        }

        /// <summary>
        /// Stores offsets and references.
        /// </summary>
        private class ReferenceMemory
        {
            /// <summary>
            /// Offsets to directories.
            /// </summary>
            public Dictionary<RsrcEntry, uint> directoryOffsets;

            /// <summary>
            /// References to directories.
            /// </summary>
            public Dictionary<RsrcEntry, List<uint>> directoryReferences;

            /// <summary>
            /// References to data entries.
            /// </summary>
            public Dictionary<RsrcEntry, List<uint>> dataEntryReferences;

            /// <summary>
            /// References to strings.
            /// </summary>
            public Dictionary<string, List<uint>> stringReferences;

            /// <summary>
            /// Initializes a <see cref="ReferenceMemory"/> instance.
            /// </summary>
            public ReferenceMemory()
            {
                directoryOffsets = new Dictionary<RsrcEntry, uint>();
                directoryReferences = new Dictionary<RsrcEntry, List<uint>>();
                dataEntryReferences = new Dictionary<RsrcEntry, List<uint>>();
                stringReferences = new Dictionary<string, List<uint>>();
            }

            /// <summary>
            /// Adds a directory offset.
            /// </summary>
            /// <param name="entry">directory</param>
            /// <param name="offPos">offset</param>
            public void AddDirectoryOffset(RsrcEntry entry, uint offPos)
            {
                directoryOffsets.Add(entry, offPos);
            }

            /// <summary>
            /// Adds a reference to a reference map.
            /// </summary>
            /// <typeparam name="TKey">The type of the map key.</typeparam>
            /// <param name="refMap">reference map</param>
            /// <param name="key">key</param>
            /// <param name="refPos">reference position</param>
            private void AddReference<TKey>(Dictionary<TKey, List<uint>> refMap, TKey key, uint refPos)
            {
                bool succ = refMap.TryGetValue(key, out List<uint> refList);
                if (!succ)
                {
                    refList = new List<uint>();
                    refMap.Add(key, refList);
                }
                refList.Add(refPos);
            }

            /// <summary>
            /// Adds a directory reference.
            /// </summary>
            /// <param name="entry">directory</param>
            /// <param name="refPos">reference position</param>
            public void AddDirectoryReference(RsrcEntry entry, uint refPos)
            {
                AddReference(directoryReferences, entry, refPos);
            }

            /// <summary>
            /// Adds a data entry reference.
            /// </summary>
            /// <param name="entry">data entry</param>
            /// <param name="refPos">reference position</param>
            public void AddDataEntryReference(RsrcEntry entry, uint refPos)
            {
                AddReference(dataEntryReferences, entry, refPos);
            }

            /// <summary>
            /// Adds a directory reference.
            /// </summary>
            /// <param name="str">string</param>
            /// <param name="refPos">reference position</param>
            public void AddStringReference(string str, uint refPos)
            {
                AddReference(stringReferences, str, refPos);
            }
        }

        /// <summary>
        /// Writes a directory.
        /// </summary>
        /// <param name="dst">stream to write to</param>
        /// <param name="root">root entry</param>
        /// <param name="refMem">offset and reference storage</param>
        private void WriteDirectory(MemoryStream dst, RsrcEntry root, ReferenceMemory refMem)
        {
            using (BinaryWriter w = new BinaryWriter(dst, Encoding.UTF8, true))
            {
                refMem.AddDirectoryOffset(root, (uint)dst.Position);
                // write unimportant fields
                w.Write(root.DirCharacteristics);
                w.Write(root.DirTimestamp);
                w.Write(root.DirVersionMajor);
                w.Write(root.DirVersionMinor);
                // first romp to count name/ID entries
                LinkedList<RsrcEntry> nameEntries = new LinkedList<RsrcEntry>(), idEntries = new LinkedList<RsrcEntry>();
                ushort nameEntryCount = 0, idEntryCount = 0;
                foreach (RsrcEntry entry in root.Entries)
                {
                    if (entry.Name == null)
                    {
                        idEntryCount++;
                        idEntries.AddLast(entry);
                    }
                    else
                    {
                        nameEntryCount++;
                        nameEntries.AddLast(entry);
                    }
                }
                // write em out
                w.Write(nameEntryCount);
                w.Write(idEntryCount);
                // second romp to actually write it
                // make a subdir list to write *after* writing the entire directory
                LinkedList<RsrcEntry> subdirs = new LinkedList<RsrcEntry>();
                WriteDirectoryEntries(w, nameEntries, subdirs, refMem);
                WriteDirectoryEntries(w, idEntries, subdirs, refMem);
                // now write the subdirectories
                foreach (RsrcEntry entry in subdirs)
                    WriteDirectory(dst, entry, refMem);
            }
        }

        /// <summary>
        /// Writes a directory's entries.
        /// </summary>
        /// <param name="w">stream writer</param>
        /// <param name="entries">entry list</param>
        /// <param name="subdirs">subdirectory list</param>
        /// <param name="refMem">offset and reference storage</param>
        private void WriteDirectoryEntries(BinaryWriter w, LinkedList<RsrcEntry> entries, LinkedList<RsrcEntry> subdirs, ReferenceMemory refMem)
        {
            foreach (RsrcEntry entry in entries)
            {
                if (entry.Name == null)
                    w.Write(entry.ID);
                else
                {
                    refMem.AddStringReference(entry.Name, (uint)w.BaseStream.Position);
                    w.Write(0x80000000);
                }
                if (entry.IsDirectory)
                {
                    refMem.AddDirectoryReference(entry, (uint)w.BaseStream.Position);
                    w.Write(0x80000000);
                    subdirs.AddLast(entry);
                }
                else if (entry.Data != null)
                {
                    refMem.AddDataEntryReference(entry, (uint)w.BaseStream.Position);
                    w.Write((uint)0);
                }
                else
                    throw new Exception("Entry has no data nor any subentries: " + entry.ToPath());
            }
        }

        /// <summary>
        /// Writes references.
        /// </summary>
        /// <param name="dst">stream to write to</param>
        /// <param name="sectionSizes">section sizes</param>
        /// <param name="refMem">offset and reference storage</param>
        private void WriteReferences(MemoryStream dst, SectionSizes sectionSizes, ReferenceMemory refMem)
        {
            using (BinaryWriter w = new BinaryWriter(dst, Encoding.UTF8, true))
            {
                // write subdirectory references
                foreach (KeyValuePair<RsrcEntry, List<uint>> entry in refMem.directoryReferences)
                {
                    bool ok = refMem.directoryOffsets.TryGetValue(entry.Key, out uint off);
                    if (!ok)
                        throw new Exception("Directory is missing offset: " + entry.Key.ToPath());
                    off |= 0x80000000;
                    foreach (uint refLoc in entry.Value)
                    {
                        dst.Seek(refLoc, SeekOrigin.Begin);
                        w.Write(off);
                    }
                }
                // write actual data, remember offsets
                Dictionary<RsrcEntry, uint> dataOffsets = new Dictionary<RsrcEntry, uint>();
                dst.Seek(sectionSizes.directorySize + sectionSizes.dataEntrySize + sectionSizes.stringSize, SeekOrigin.Begin);
                Trace($"dst.Length = 0x{dst.Length:X}");
                foreach (KeyValuePair<RsrcEntry, List<uint>> entry in refMem.dataEntryReferences)
                {
                    Trace($"Writing data for entry {entry.Key.ToPath()} at 0x{dst.Position:X} with size 0x{entry.Key.Data.Length:X}, ends at 0x{(dst.Position + entry.Key.Data.Length):X}");
                    dataOffsets.Add(entry.Key, (uint)dst.Position);
                    byte[] data = entry.Key.Data;
                    w.Write(data);
                }
                // write data entries and their references
                dst.Seek(sectionSizes.directorySize, SeekOrigin.Begin);
                foreach (KeyValuePair<RsrcEntry, List<uint>> entry in refMem.dataEntryReferences)
                {
                    uint off = (uint)dst.Position;
                    foreach (uint refLoc in entry.Value)
                    {
                        dst.Seek(refLoc, SeekOrigin.Begin);
                        w.Write(off);
                    }
                    dst.Seek(off, SeekOrigin.Begin);
                    RsrcEntry rsrc = entry.Key;
                    bool ok = dataOffsets.TryGetValue(rsrc, out uint dataPos);
                    if (!ok)
                        throw new Exception("Data is missing offset: " + rsrc.ToPath());
                    Trace($"Writing data entry for entry {entry.Key.ToPath()} at 0x{dst.Position:X}; position is 0x{dataPos:X}, size is 0x{rsrc.Data.Length:X}");
                    w.Write(dataPos);
                    w.Write((uint)rsrc.Data.Length);
                    w.Write(rsrc.DataCodepage);
                    w.Write(rsrc.DataReserved);
                }
                // write strings (directory names) and their references
                dst.Seek(sectionSizes.directorySize + sectionSizes.dataEntrySize, SeekOrigin.Begin);
                foreach (KeyValuePair<string, List<uint>> entry in refMem.stringReferences)
                {
                    uint pos = (uint)dst.Position;
                    uint off = pos | 0x80000000;
                    foreach (uint refLoc in entry.Value)
                    {
                        dst.Seek(refLoc, SeekOrigin.Begin);
                        w.Write(off);
                    }
                    dst.Seek(pos, SeekOrigin.Begin);
                    string str = entry.Key;
                    ushort strLen = (ushort)str.Length;
                    w.Write(strLen);
                    for (int i = 0; i < strLen; i++)
                        w.Write((ushort)str[i]);
                }
            }
        }
    }
}