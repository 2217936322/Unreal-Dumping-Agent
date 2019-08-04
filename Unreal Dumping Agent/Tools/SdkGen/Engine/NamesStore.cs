using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unreal_Dumping_Agent.UtilsHelper;

namespace Unreal_Dumping_Agent.Tools.SdkGen.Engine
{
    public class GNameInfo
    {
        public IntPtr Address { get; internal set; }
        public List<EngineClasses.FNameEntity> Names { get; internal set; }
        public List<IntPtr> Chunks { get; internal set; }
        public int ChunkSize { get; internal set; }
        public bool ReadyToUse { get; internal set; }

        public GNameInfo()
        {
            Address = IntPtr.Zero;
            Names = new List<EngineClasses.FNameEntity>();
            Chunks = new List<IntPtr>();
            ChunkSize = 0x4000;
        }
    }
    public static class NamesStore
    {
        public static GNameInfo GNames { get; } = new GNameInfo();

        public static async Task<bool> Initialize(IntPtr gnamesAddress, bool forceReInit = true)
        {
            if (!Utils.IsValidGNamesAddress(gnamesAddress))
                return false;

            if (!forceReInit && GNames.Address != IntPtr.Zero)
                return true;

            GNames.Names.Clear();
            GNames.Chunks.Clear();
            GNames.Address = gnamesAddress;

            return await ReadGNameArray();
        }
        public static async Task<bool> ReadGNameArray()
        {
            int ptrSize = Utils.GamePointerSize();

            // Dereference Static Pointer, now it's chunks array address
            var chunksAddress = Utils.MemObj.ReadAddress(GNames.Address);

            // Get GNames Chunks
            for (int i = 0; i < 15; i++)
            {
                int offset = ptrSize * i;

                IntPtr addr = Utils.MemObj.ReadAddress(chunksAddress + offset);
                if (!Utils.IsValidRemoteAddress(addr)) break;

                GNames.Chunks.Add(addr);
            }

            // Calc AnsiName offset
            var nameOffset = Utils.CalcNameOffset(Utils.MemObj.ReadAddress(GNames.Chunks[0]), false);

            // Dump Names
            foreach (var namesChunk in GNames.Chunks)
            {
                for (int i = 0; i < GNames.ChunkSize; i++)
                {
                    int offset = ptrSize * i;
                    var fNameAddress = Utils.MemObj.ReadAddress(namesChunk + offset);

                    if (!Utils.IsValidRemoteAddress(fNameAddress))
                    {
                        // Push Empty, if i just skip will case a problems, so just add empty item
                        GNames.Names.Add(new EngineClasses.FNameEntity { Index = GNames.Names.Count, AnsiName = string.Empty });
                        continue;
                    }

                    // Read FName
                    var tmp = new EngineClasses.FNameEntity();
                    if (!await tmp.ReadData(fNameAddress, nameOffset))
                        return false;

                    tmp.Index = GNames.Names.Count;
                    GNames.Names.Add(tmp);
                }
            }

            GNames.ReadyToUse = true;
            return true;
        }
        public static bool IsValid(int index)
        {
            return index >= 0 && index <= GNames.Names.Count && !string.IsNullOrEmpty(GetByIndex(index));
        }
        public static string GetByIndex(int index)
        {
            return index > GNames.Names.Count ? null : GNames.Names[index].AnsiName;
        }
        public static int GetByName(string name)
        {
            var fNameEntity = GNames.Names.FirstOrDefault(n => n.AnsiName == name);
            return fNameEntity?.Index ?? -1;
        }
    }
}
