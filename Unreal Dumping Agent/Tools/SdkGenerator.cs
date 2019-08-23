using Discord.Rest;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
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
        private AgentRequestInfo _requestInfo;

        public SdkGenerator(IntPtr gobjects, IntPtr gnames)
        {
            _gobjects = gobjects;
            _gnames = gnames;
        }

        private async Task UpdateDiscordState(string title, string content, bool inline = false)
        {
            if (_requestInfo.Context == null)
                return;

            Embed curEmbed;

            // Get old embed
            if (_requestInfo.AgentMessage != null)
            {
                // Update the current embed
                curEmbed = _requestInfo.AgentMessage.Embeds.First();

                // Get old information
                var newEmbed = new EmbedBuilder
                {
                    Title = curEmbed.Title,
                    Color = curEmbed.Color,
                    Description = curEmbed.Description,
                    ImageUrl = curEmbed.Image?.Url,
                    Url = curEmbed.Url,
                    ThumbnailUrl = curEmbed.Thumbnail?.Url,
                    Timestamp = curEmbed.Timestamp?.DateTime,
                    Footer = new EmbedFooterBuilder { Text = curEmbed.Footer?.Text, IconUrl = curEmbed.Footer?.IconUrl }
                };

                // Gen Fields, with same sort of fields
                bool isOldField = false;
                var newFields = new List<EmbedFieldBuilder>();
                foreach (var oldField in curEmbed.Fields)
                {
                    if (oldField.Name != title)
                    {
                        newFields.Add(new EmbedFieldBuilder { Name = oldField.Name, Value = oldField.Value, IsInline = inline });
                    }
                    else
                    {
                        // Add modified field
                        newFields.Add(new EmbedFieldBuilder {Name = title, Value = content, IsInline = inline});
                        isOldField = true;
                    }
                }
                if (!isOldField)
                    newFields.Add(new EmbedFieldBuilder {Name = title, Value = content, IsInline = inline});

                // Gen new embed
                newEmbed.WithFields(newFields);
                curEmbed = newEmbed.Build();
            }
            else
            {
                // First Message
                var embBuilder = new EmbedBuilder
                {
                    Title = "Sdk Generator Info",
                    Description = "Information about `Sdk Generation` progress",
                    Color = Color.Blue
                };

                // Fields
                var fBuild = new EmbedFieldBuilder { Name = title, Value = content, IsInline = inline };

                embBuilder.WithUrl(Utils.DonateUrl);
                embBuilder.WithFields(fBuild);
                embBuilder.WithFooter(Utils.DiscordFooterText, Utils.DiscordFooterImg);

                curEmbed = embBuilder.Build();
            }

            // if first message then create a new message
            if (_requestInfo.AgentMessage == null)
                _requestInfo.AgentMessage = await _requestInfo.Context.Channel.SendMessageAsync(embed: curEmbed);

            // if it's old one then, just edit it !!
            else
                await _requestInfo.AgentMessage.ModifyAsync(msg => msg.Embed = curEmbed);
        }

        /// <summary>
        /// Start Dumping Packages On Target Process<para/>
        /// As Programming Lang Code.
        /// </summary>
        /// <param name="requestInfo">Information About User</param>
        public async Task<GenRetInfo> Start(AgentRequestInfo requestInfo)
        {
            var ret = new GenRetInfo();
            _requestInfo = requestInfo;

            #region Check Address
            var uDiscord = UpdateDiscordState("State", "Check Address !!");

            if (!Utils.IsValidGNamesAddress(_gnames))
                return new GenRetInfo { State = GeneratorState.BadGName };
            if (!Utils.IsTUobjectArray(_gobjects))
                return new GenRetInfo { State = GeneratorState.BadGObject };
            #endregion

            #region GNames
            // Wait Discord message to send (if was not !!)
            await uDiscord; uDiscord = UpdateDiscordState("State", "Dumping **GNames**. !!");

            // Dump GNames
            var gnamesT = NamesStore.Initialize(_gnames);
            if (!await gnamesT)
                return new GenRetInfo { State = GeneratorState.BadGName };

            // Update Information
            await uDiscord; uDiscord = UpdateDiscordState("GNames", $"*{NamesStore.GNames.Names.Count}*");
            #endregion

            #region GObjects
            // Wait Discord message to send (if was not !!)
            await uDiscord; uDiscord = UpdateDiscordState("State", "Dumping **GObjects**. !!");

            // Dump GObjects
            var gobjectsT = ObjectsStore.Initialize(_gobjects);
            if (!await gobjectsT)
                return new GenRetInfo { State = GeneratorState.BadGObject };

            // Update Information
            await uDiscord; uDiscord = UpdateDiscordState("GObjects", $"*{ObjectsStore.GObjects.Objects.Count}*");
            #endregion

            #region Init Generator
            // Init Generator Settings
            if (!Generator.Initialize())
                return new GenRetInfo { State = GeneratorState.Bad };

            // Init Generator info
            //MODULEENTRY32 mod = { };
            //Utils::MemoryObj->GetModuleInfo(startInfo.GameModule, mod);

            Generator.SdkPath = Path.Combine(Program.GenPath, "SDK");
            Generator.LangPaths = Program.LangsPath;
            Generator.GameName = "GameName";
            Generator.GameVersion = "1.0.0";
            Generator.SdkType = SdkType.Internal;
            Generator.IsGObjectsChunks = ObjectsStore.GObjects.IsChunksAddress;
            Generator.SdkLangName = "Cpp";
            Generator.GameModule = "GameModule";
            Generator.GameModuleBase = (IntPtr)0x0;

            Directory.CreateDirectory(Generator.SdkPath);

            if (!InitSdkLang())
                return new GenRetInfo { State = GeneratorState.BadSdkLang };
            #endregion

            #region Dump Names/Objects
            // ToDo: Dump To Files
            // Dump To Files
            //if (Utils::GenObj->ShouldDumpArrays())
            //{
            //    *startInfo.State = "Dumping (GNames/GObjects).";
            //    Dump(outputDirectory, *startInfo.State);
            //    *startInfo.State = "Dump (GNames/GObjects) Done.";
            //    Sleep(2 * 1000);
            //}
            #endregion

            // Wait Discord message to send (if was not !!)
            await uDiscord;

            // Process Packages
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
        private async Task ProcessPackages()
        {
            var uDiscord = UpdateDiscordState("State", "Collect **Packages**. !!");

            var packages = new List<Package>();
            var packageObjects = CollectPackages();

            // Update Information
            await uDiscord; uDiscord = UpdateDiscordState("Packages", $"*{packageObjects.Count}*", true);

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

                await uDiscord; uDiscord = UpdateDiscordState("State", "Dumping **CoreUObject**.");

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
            // Update Information
            await uDiscord; uDiscord = UpdateDiscordState("State", $"Dumping **Packages** ( {packages.Count}/{packageObjects.Count} ).");

            // Process Packages
            var lockObj = new object();
            Parallel.ForEach(packageObjects, packObj =>
            {
                var package = new Package(packObj);

                // Async in Parallel => Loot of problems !!
                package.Process().GetAwaiter().GetResult();

                if (!package.Save().Result)
                    return;

                lock (lockObj)
                {
                    Package.PackageMap[packObj] = package;
                    packages.Add(package);
                }

                UpdateDiscordState("State", $"Dumping **Packages** ( {packages.Count}/{packageObjects.Count} ).").GetAwaiter().GetResult();
                Utils.ConsoleText("Dump", package.GetName().Result, ConsoleColor.Red);
            });

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

            await SdkAfterFinish(packages);
        }

        /// <summary>
        /// Called After All Of Packages Proceed 
        /// </summary>
        private static async Task SdkAfterFinish(List<Package> packages)
        {
            var missing = Package.ProcessedObjects.Where(kv => !kv.Value).ToList();
            var missedList = new List<GenericTypes.UEStruct>();

            if (!missing.Empty())
                missedList = missing.Select(kv => ObjectsStore.GetByAddress(kv.Key).Result.Cast<GenericTypes.UEStruct>()).ToList();

            await Generator.GenLang.SdkAfterFinish(packages, missedList);
        }
    }
}
