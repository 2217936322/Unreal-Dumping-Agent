using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Rest;
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

        public async Task<GenRetInfo> Start(AgentRequestInfo requestInfo)
        {
            var ret = new GenRetInfo();

            // ToDo: Add some user message here like count, and some things like that
            if (requestInfo.Context != null)
                _sdkMessage = await requestInfo.Context.Channel.SendMessageAsync();

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

            Generator.GameName = "GameName";
            Generator.GameVersion = "1.0.0";
            Generator.SdkType = SdkType.Internal;
            Generator.IsGObjectsChunks = ObjectsStore.GObjects.IsChunksAddress;
            Generator.SdkLang = "Cpp";
            Generator.GameModule = "GameModule";
            Generator.GameModuleBase = (IntPtr)0x0;

            Directory.CreateDirectory(Path.Combine(Program.SdkGenPath, "SDK"));

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
        private Task<List<GenericTypes.UEObject>> CollectPackages()
        {
            return Task.Run(() =>
            {
                var lockObj = new object();

                var ret = ObjectsStore.GObjects.Objects
                    .Where(curObj => curObj.IsValid())

                    // Get Package for every object
                    .Select(curObj => curObj.GetPackageObject().Result)
                    .Where(package => package.IsValid())

                    // Distinct
                    .GroupBy(p => p.GetAddress())
                    .Select(y => y.First())
                    
                    // To List
                    .ToList();

                //Parallel.ForEach(ObjectsStore.GObjects.Objects, (curObj, state) =>
                //{
                //    if (!curObj.IsValid())
                //        return;

                //    var package = curObj.GetPackageObject().Result;
                //    if (!package.IsValid())
                //        return;

                //    //lock (lockObj)
                //    //    ret1.Add(package);
                //});

                // ret = ret.Distinct().ToList();

                return ret;
            });
        }

        private async Task ProcessPackages()
        {
            var packages = new List<Package>();
            var processedObjects = new Dictionary<IntPtr, bool>();
            var packageObjects = new List<GenericTypes.UEObject>();

            packageObjects = await CollectPackages();

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
                processedObjects = await package.Process(processedObjects);

                if (await package.Save())
                {
                    Package.PackageMap[coreUObject] = package;
                    packages.Add(package);
                }

                // Remove CoreUObject Package to not dump it twice
                packageObjects.RemoveAt(coreUObjectIndex);
            }
            #endregion

            var lockObj = new object();
            Parallel.ForEach(packageObjects, (packObj, state) =>
            {
                var package = new Package(packObj);
                var processedObjectsTmp = package.Process(processedObjects).Result;

                lock (lockObj)
                    processedObjects = processedObjectsTmp;

                if (!package.Save().Result)
                    return;

                Package.PackageMap[packObj] = package;
                packages.Add(package);
            });


        }

        private bool InitSdkLang()
        {
            // Check if this lang is supported
            if (!Program.SupportedLangs.ContainsKey(Generator.SdkLang))
                return false;

            Program.Lang = Program.SupportedLangs[Generator.SdkLang];
            Program.Lang.Init();

            return true;
        }

        private void SdkAfterFinish()
        {
            // Program.Lang.SdkAfterFinish(_packageObj, );
        }
    }
}
