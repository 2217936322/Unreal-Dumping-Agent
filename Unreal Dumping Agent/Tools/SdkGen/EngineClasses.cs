using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Utils = Unreal_Dumping_Agent.UtilsHelper.Utils;

namespace Unreal_Dumping_Agent.Tools.SdkGen
{
    public interface IUeStruct
    {
        void FixPointers();
    }

    public static class EngineClasses
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct FPointer : IUeStruct
        {
            public IntPtr Dummy;
            public int gg;
            public IntPtr Dummy2;

            public void FixPointers()
            {
                Utils.FixPointers(ref this);
            }
        }
    }
}
