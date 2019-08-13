using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unreal_Dumping_Agent.UtilsHelper;
using static Unreal_Dumping_Agent.Tools.SdkGen.Engine.UE4.GenericTypes;
using static Unreal_Dumping_Agent.Tools.SdkGen.EngineClasses;
    
namespace Unreal_Dumping_Agent.Tools.SdkGen.Engine
{
    public class GObjectInfo
    {
        /// <summary>
        /// Static address for gobjects,
        /// it's only for saving it !!
        /// <para>Use `ChunksAddress` instead</para>
        /// </summary>
        public IntPtr Address { get; internal set; }
        public IntPtr ChunksAddress { get; internal set; }
        public List<UEObject> Objects { get; internal set; }
        public List<IntPtr> Chunks { get; internal set; }
        public bool IsPointerNextToPointer { get; internal set; }
        public bool IsChunksAddress { get; internal set; }
        public bool ReadyToUse { get; internal set; }

        public GObjectInfo()
        {
            Chunks = new List<IntPtr>();
            Objects = new List<UEObject>();
        }
    }

    public static class ObjectsStore
    {
        public static GObjectInfo GObjects { get; } = new GObjectInfo();
        private const int NumElementsPerChunk = 0x11000;
        private const int MinZeroAddress = 150;

        public static async Task<bool> Initialize(IntPtr gobjectsAddress, bool forceReInit = true)
        {
            if (!NamesStore.GNames.ReadyToUse)
                throw new Exception("Initialize Names before try to initialize objects.");

            if (!Utils.IsValidGObjectsAddress(Utils.MemObj.ReadAddress(gobjectsAddress)))
                return false;

            if (!forceReInit && !GObjects.ChunksAddress.IsNull())
                return true;

            GObjects.Objects.Clear();
            GObjects.Chunks.Clear();
            GObjects.Address = gobjectsAddress;
            GObjects.ChunksAddress = Utils.MemObj.ReadAddress(gobjectsAddress);

            return await FetchData();
        }
        private static async Task<bool> FetchData()
        {
            if (!await GetGObjectInfo())
                return false;

            return await ReadUObjectArray();
        }
        private static Task<bool> GetGObjectInfo()
        {
            int ptrSize = Utils.GamePointerSize();

            // Check if it's first `UObject` or first `Chunk` address
            // And Get Chunk and other GObjects info
            {
                int skipCount = 0;
                for (int uIndex = 0; uIndex <= 20 && skipCount <= 5; uIndex++)
                {
                    IntPtr curAddress = GObjects.ChunksAddress + (uIndex * ptrSize);
                    IntPtr chunk = Utils.MemObj.ReadAddress(curAddress);

                    if (chunk.IsNull())
                    {
                        skipCount++;
                        continue;
                    }

                    if (!Utils.IsValidRemoteAddress(chunk))
                        break;

                    skipCount = 0;
                    GObjects.Chunks.Add(chunk);
                }

                if (skipCount >= 5)
                {
                    GObjects.IsChunksAddress = true;
                }
                else
                {
                    GObjects.IsChunksAddress = false;
                    GObjects.Chunks.Clear();
                    // if game didn't use chunks then we must have one value on this list
                    // not necessary we can add NULL, just we need list to have count 1!
                    GObjects.Chunks.Add(GObjects.ChunksAddress);
                }
            }

            // Get Work Method [Pointer Next Pointer or FUObjectItem(Flags, ClusterIndex, etc)]
            {
                IntPtr firstObj = GObjects.IsChunksAddress
                    ? Utils.MemObj.ReadAddress(GObjects.ChunksAddress)
                    : GObjects.ChunksAddress;
                IntPtr obj1 = Utils.MemObj.ReadAddress(firstObj);
                IntPtr obj2 = Utils.MemObj.ReadAddress(firstObj + ptrSize);

                if (!Utils.IsValidRemoteAddress(obj1))
                    return Task.FromResult(false);

                // Not Valid mean it's not Pointer Next To Pointer, or GObject address is wrong.
                GObjects.IsPointerNextToPointer = Utils.IsValidRemoteAddress(obj2);
            }

            return Task.FromResult(true);
        }
        private static async Task<bool> ReadUObjectArray()
        {
            int ptrSize = Utils.GamePointerSize();

            for (int i = 0; i < GObjects.Chunks.Count; i++)
            {
                int skipCount = 0;
                int offset = i * ptrSize;
                IntPtr chunkAddress = GObjects.IsChunksAddress ? Utils.MemObj.ReadAddress(GObjects.ChunksAddress + offset) : GObjects.ChunksAddress;

                for (int uIndex = 0; GObjects.IsChunksAddress ? uIndex <= NumElementsPerChunk : skipCount <= MinZeroAddress; uIndex++)
                {
                    IntPtr dwUObject;

			        // Get object address
                    if (GObjects.IsPointerNextToPointer)
                    {
                        dwUObject = Utils.MemObj.ReadAddress(chunkAddress + uIndex * ptrSize);
                    }
                    else
                    {
                        var fUObjectItem = new FUObjectItem();
                        var fUObject = chunkAddress + uIndex * fUObjectItem.StructSize();
                        await fUObjectItem.ReadData(fUObject);
                        dwUObject = fUObjectItem.Object;
                    }

                    // Skip null pointer in GObjects array
                    if (dwUObject.IsNull())
                    {
                        skipCount++;
                        continue;
                    }

                    skipCount = 0;

                    // Break if found bad object in GObjects array, GObjects shouldn't have bad object
                    // check is not a static address !
                    if (!ReadUObject(dwUObject, out UEObject curObject))
                        break;

                    GObjects.Objects.Add(curObject);
                }
            }

            GObjects.ReadyToUse = true;
            return true;
        }
        private static bool ReadUObject(IntPtr uObjectAddress, out UEObject retUObj)
        {
            var tmp = new UObject();
            retUObj = new UEObject(tmp);

            return Utils.IsValidRemoteAddress(uObjectAddress) && tmp.ReadData(uObjectAddress).Result;
        }

        public static UEObject GetByIndex(int index) => GObjects.Objects[index];
        public static Task<UEObject> GetByAddress(IntPtr address)
        {
            return Task.Run(() =>
            {
                var ueObject = GObjects.Objects.FirstOrDefault(obj => obj.Object.ObjAddress.ToInt64() == address.ToInt64());

                if (ueObject == null)
                    throw new KeyNotFoundException("Try to get wrong ObjectAddress. !! maybe it's EngineStructs problem.!!");

                return ueObject;
            });
        }
        public static UEObject GetByAddress(IntPtr address, out bool success)
        {
            try
            {
                var obj = GetByAddress(address);
                success = true;
                return obj.Result;
            }
            catch (KeyNotFoundException)
            {
                success = false;
            }

            return new UEObject();
        }
        public static int GetIndexByAddress(IntPtr address)
        {
            return GObjects.Objects.FindIndex(o => o.GetAddress() == address);
        }
        public static Task<UEClass> FindClass(string name)
        {
            return Task.Run(() =>
            {
                var ret = GObjects.Objects.FirstOrDefault(o => o.GetFullName().Result == name);
                return ret == null ? new UEClass() : ret.Cast<UEClass>();
            });
        }

        private static readonly Dictionary<string, int> _countCache = new Dictionary<string, int>();
        public static int CountObjects<T>(string name) where T : UEObject, new()
        {
            if (_countCache.ContainsKey(name))
                return _countCache[name];

            lock (Utils.MainLocker)
            {
                int count = GObjects.Objects
                    .Count(obj => obj.IsA<T>().Result && obj.GetName().Result == name);

                _countCache[name] = count;
            }

            return _countCache[name];
        }
    }
}
