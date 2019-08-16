using System.Collections.Generic;
using System.Threading.Tasks;
using Unreal_Dumping_Agent.Tools.SdkGen.Engine;
using Unreal_Dumping_Agent.Tools.SdkGen.Engine.UE4;

namespace Unreal_Dumping_Agent.UtilsHelper
{
    public abstract class SdkLang
    {
        /// <summary>
        /// Save Structs In Package
        /// </summary>
        /// <param name="package">Package That's Contains Structs To Save</param>
        public abstract Task SaveStructs(Package package);

        /// <summary>
        /// Save Classes In Package
        /// </summary>
        /// <param name="package">Package That's Contains Classes To Save</param>
        public abstract Task SaveClasses(Package package);

        /// <summary>
        /// Save Functions In Package
        /// </summary>
        /// <param name="package">Package That's Contains Functions To Save</param>
        public abstract Task SaveFunctions(Package package);

        /// <summary>
        /// Save Function Parameters In Package
        /// </summary>
        /// <param name="package">Package That's Contains Structs To Save</param>
        public abstract Task SaveFunctionParameters(Package package);

        /// <summary>
        /// Save Constants In Package
        /// </summary>
        /// <param name="package">Package That's Contains Constants To Save</param>
        public abstract Task SaveConstants(Package package);

        /// <summary>
        /// Called After All Packages Processed
        /// </summary>
        /// <param name="packages">Packages That Was Processed</param>
        /// <param name="missing"></param>
        /// <returns></returns>
        public abstract Task SdkAfterFinish(List<Package> packages, List<GenericTypes.UEStruct> missing);

        public virtual bool Init()
        {
            return true;
        }
    }
}
