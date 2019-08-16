using Discord.Rest;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Unreal_Dumping_Agent.Tools.SdkGen;
using Unreal_Dumping_Agent.Tools.SdkGen.Engine;
using Unreal_Dumping_Agent.Tools.SdkGen.Engine.UE4;
using Unreal_Dumping_Agent.UtilsHelper;

namespace Unreal_Dumping_Agent.Tools
{

    public class SdkGenerator
    {
        #region Useful types
        public enum GeneratorState
        {
            Bad,
            Good,
            BadGObject,
            BadGName,
            BadSdkLang
        };
        public class GenRetInfo
        {
            public GeneratorState State;
            public DateTime TookTime;
        };
        #endregion

        private readonly IntPtr _gobjects, _gnames;
        private RestUserMessage _sdkMessage;

        public SdkGenerator(IntPtr gobjects, IntPtr gnames)
        {
            _gobjects = gobjects;
            _gnames = gnames;
        }

        /// <summary>
        /// Start Dumping Packages On Target Process<para/>
        /// As Programming Lang Code.
        /// </summary>
        /// <param name="requestInfo">Information About User</param>
        public async Task<GenRetInfo> Start(AgentRequestInfo requestInfo)
        {
            var ret = new GenRetInfo();

            // ToDo: Add some user message here like count, and some things like that
            //if (requestInfo.Context != null)
            //    _sdkMessage = await requestInfo.Context.Channel.SendMessageAsync();

            // Check Address
            if (!Utils.IsValidGNamesAddress(_gnames))
                return new GenRetInfo { State = GeneratorState.BadGName};
            if (!Utils.IsTUobjectArray(_gobjects))
                return new GenRetInfo { State = GeneratorState.BadGObject };

            // Dump GNames
            var gnamesT = NamesStore.Initialize(_gnames);
            if (!await gnamesT)
                return new GenRetInfo { State = GeneratorState.BadGName };

            // Dump GObjects
            var gobjectsT = ObjectsStore.Initialize(_gobjects);
            if (!await gobjectsT)
                return new GenRetInfo { State = GeneratorState.BadGObject };

            // Init Generator Settings
            if (!Generator.Initialize())
                return new GenRetInfo { State = GeneratorState.Bad };

            // Init Generator info
            //MODULEENTRY32 mod = { };
            //Utils::MemoryObj->GetModuleInfo(startInfo.GameModule, mod);

            Directory.CreateDirectory(Path.Combine(Program.SdkGenPath, "SDK"));

            Generator.GameName = "GameName";
            Generator.GameVersion = "1.0.0";
            Generator.SdkType = SdkType.Internal;
            Generator.IsGObjectsChunks = ObjectsStore.GObjects.IsChunksAddress;
            Generator.SdkLangName = "Cpp";
            Generator.GameModule = "GameModule";
            Generator.GameModuleBase = (IntPtr)0x0;

            if (!InitSdkLang())
                return new GenRetInfo { State = GeneratorState.BadSdkLang };

            // ToDo: Dump To Files
            // Dump To Files
            //if (Utils::GenObj->ShouldDumpArrays())
            //{
            //    *startInfo.State = "Dumping (GNames/GObjects).";
            //    Dump(outputDirectory, *startInfo.State);
            //    *startInfo.State = "Dump (GNames/GObjects) Done.";
            //    Sleep(2 * 1000);
            //}

            await ProcessPackages();

            return ret;
        }

        /// <summary>
        /// Collect all package on target game
        /// </summary>
        /// <returns>Return all Packages on the game as <see cref="GenericTypes.UEObject"/></returns>
        private static List<GenericTypes.UEObject> CollectPackages()
        {
            var ret = ObjectsStore.GObjects.Objects
                .AsParallel()
                .Where(curObj => curObj.IsValid())

                // Get Package for every object
                .Select(curObj => curObj.GetPackageObject().Result)
                .Where(package => package.IsValid())

                // Distinct
                .GroupBy(p => p.GetAddress())
                .Select(y => y.First())

                // To List
                .ToList();

            return ret;
        }

        /// <summary>
        /// Init Selected SdkLang
        /// </summary>
        private static bool InitSdkLang()
        {
            // Check if this lang is supported
            if (!Program.SupportedLangs.ContainsKey(Generator.SdkLangName))
                return false;

            Generator.GenLang = Program.SupportedLangs[Generator.SdkLangName];
            return Generator.GenLang.Init();
        }

        /// <summary>
        /// Process All Packages
        /// </summary>
        private static async Task ProcessPackages()
        {
            var packages = new List<Package>();
            var packageObjects = CollectPackages();

            #region CoreUObject
            {
                /*
		        * First we must complete Core Package.
		        * It's contains all important stuff, (like we need it in 'StaticClass' function)
		        * So before go parallel we must get 'CoreUObject'
		        * Some times CoreUObject not the first Package
		        */

                // Get CoreUObject
                var coreUObject = new GenericTypes.UEObject();
                int coreUObjectIndex = 0;
                for (int i = 0; i < packageObjects.Count; ++i)
                {
                    var packPtr = packageObjects[i];

                    if (await packPtr.GetName() != "CoreUObject")
                        continue;

                    coreUObject = packPtr;
                    coreUObjectIndex = i;
                    break;
                }

                // Process CoreUObject
                var package = new Package(coreUObject);
                await package.Process();

                if (await package.Save())
                {
                    Package.PackageMap[coreUObject] = package;
                    packages.Add(package);
                }

                // Remove CoreUObject Package to not dump it twice
                packageObjects.RemoveAt(coreUObjectIndex);
            }
            #endregion

            #region Packages
            foreach (var packObj in packageObjects)
            {
                var package = new Package(packObj);
                await package.Process();

                if (!await package.Save())
                    return;

                Package.PackageMap[packObj] = package;
                packages.Add(package);
            }

            if (!packages.Empty())
            {
                for (int i = 0; i < packages.Count - 1; i++)
                {
                    for (int j = 0; j < packages.Count - i - 1; j++)
                    {
                        if (!Package.PackageDependencyComparer(packages[j], packages[j + 1]))
                            packages = packages.Swap(j, j + 1);
                    }
                }
            }
            #endregion

            SdkAfterFinish(packages);
        }

        /// <summary>
        /// Called After All Of Packages Proceed 
        /// </summary>
        private static void SdkAfterFinish(List<Package> packages)
        {
            var missing = Package.ProcessedObjects.Where(kv => !kv.Value).ToList();
            var missedList = new List<GenericTypes.UEStruct>();

            if (!missing.Empty())
                missedList = missing.Select(kv => ObjectsStore.GetByAddress(kv.Key).Result.Cast<GenericTypes.UEStruct>()).ToList();

            Generator.GenLang.SdkAfterFinish(packages, missedList);
        }
    }
}
