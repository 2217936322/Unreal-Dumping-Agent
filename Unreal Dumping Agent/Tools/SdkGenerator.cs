using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Unreal_Dumping_Agent.Discord.Misc;
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
            None,
            Good,
            BadGenerator,
            BadGObject,
            BadGName,
            BadSdkLang
        };
        public class GenRetInfo
        {
            public GeneratorState State;
            public DateTime StartTime;
        };
        #endregion

        private int _donePackagesCount;
        private readonly IntPtr _gobjects, _gnames;
        private AgentRequestInfo _requestInfo;
        private bool _sdkWork;
        private List<Package> _packages = new List<Package>();
        private List<GenericTypes.UEObject> _packageObjects = new List<GenericTypes.UEObject>();

        public SdkGenerator(IntPtr gobjects, IntPtr gnames)
        {
            _gobjects = gobjects;
            _gnames = gnames;
        }

        /// <summary>
        /// Update Sdk Gen Discord Message
        /// </summary>
        /// <param name="title">Field Name</param>
        /// <param name="content">Field Content</param>
        /// <param name="inline">Field Inline?</param>
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
        /// Method To Update Sdk Gen Discord Message, Every Some Seconds
        /// </summary>
        private async Task DiscordMessageUpdater()
        {
            while (_sdkWork)
            {
                await UpdateDiscordState("State", $"Dumping **Packages** ( {_donePackagesCount}/{_packageObjects.Count + 1} ).");
                await Task.Delay(2000);
            }
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
            ret.StartTime = DateTime.Now;

            #region Check Address
            var uDiscord = UpdateDiscordState("State", "Check Address !!");

            if (!Utils.IsValidGNamesAddress(_gnames))
                return new GenRetInfo { State = GeneratorState.BadGName };
            if (!Utils.IsTUobjectArray(_gobjects))
                return new GenRetInfo { State = GeneratorState.BadGObject };
            #endregion

            // Flag for `DiscordMessageUpdater`
            _sdkWork = true;

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
                return new GenRetInfo { State = GeneratorState.BadGenerator };

            // Init Generator info
            Generator.SdkPath = Path.Combine(Program.GenPath, "SDK");
            Generator.LangPaths = Program.LangsPath;
            Generator.IsGObjectsChunks = ObjectsStore.GObjects.IsChunksAddress;

            Generator.GameName = "GameName";
            Generator.GameVersion = "1.0.0";
            Generator.SdkType = SdkType.Internal;
            Generator.SdkLangName = "Cpp";
            Generator.GameModule = "GameModule";

            Utils.MemObj.GetModuleInfo(Generator.GameModule, out var mod);
            Generator.GameModuleBase = mod.BaseAddress;

            if (!InitSdkLang())
                return new GenRetInfo { State = GeneratorState.BadSdkLang };
            #endregion

            Directory.CreateDirectory(Generator.SdkPath);

            #region Dump Names/Objects To Files
            //if (Generator.ShouldDumpArrays)
            //{
            //    *startInfo.State = "Dumping (GNames/GObjects).";
            //    Dump(outputDirectory, *startInfo.State);
            //    *startInfo.State = "Dump (GNames/GObjects) Done.";
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
            _packageObjects = CollectPackages();

            // Update Information
            await uDiscord; uDiscord = UpdateDiscordState("Packages", $"*{_packageObjects.Count}*", true);

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
                for (int i = 0; i < _packageObjects.Count; ++i)
                {
                    var packPtr = _packageObjects[i];

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

                _donePackagesCount++;

                if (await package.Save())
                {
                    Package.PackageMap[coreUObject] = package;
                    _packages.Add(package);
                }

                // Remove CoreUObject Package to not dump it twice
                _packageObjects.RemoveAt(coreUObjectIndex);
            }
            #endregion

            // Don't wait
            #pragma warning disable 4014
            DiscordMessageUpdater();
            #pragma warning restore 4014

            #region Packages
            // Update Information
            await uDiscord; uDiscord = UpdateDiscordState("State", $"Dumping **Packages** ( {_packages.Count}/{_packageObjects.Count + 1} ).");

            // Process Packages
            var lockObj = new object();
            Parallel.ForEach(_packageObjects, packObj =>
            {
                var package = new Package(packObj);

                // Async in Parallel => Loot of problems !!
                package.Process().GetAwaiter().GetResult();

                _donePackagesCount++;

                if (!package.Save().Result)
                    return;

                lock (lockObj)
                {
                    Package.PackageMap[packObj] = package;
                    _packages.Add(package);
                }

                Utils.ConsoleText("Dump", package.GetName().Result, ConsoleColor.Red);
            });

            if (!_packages.Empty())
            {
                for (int i = 0; i < _packages.Count - 1; i++)
                {
                    for (int j = 0; j < _packages.Count - i - 1; j++)
                    {
                        if (!Package.PackageDependencyComparer(_packages[j], _packages[j + 1]))
                            _packages = _packages.Swap(j, j + 1);
                    }
                }
            }
            #endregion

            await SdkAfterFinish(_packages);

            _sdkWork = false;
            await uDiscord; await UpdateDiscordState("State", $"FINIIIISHED !!");
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
