using System.Collections.Generic;
using System.Threading.Tasks;
using Unreal_Dumping_Agent.Tools.SdkGen.Engine;
using Unreal_Dumping_Agent.Tools.SdkGen.Engine.UE4;

namespace Unreal_Dumping_Agent.UtilsHelper
{
    public abstract class SdkLang
    {
        public abstract Task SaveStructs(Package package);
        public abstract Task SaveClasses(Package package);
        public abstract Task SaveFunctions(Package package);
        public abstract Task SaveFunctionParameters(Package package);
        public abstract Task SdkAfterFinish(List<Package> packages, List<GenericTypes.UEStruct> missing);

        public virtual bool Init()
        {
            return true;
        }
    }
}
