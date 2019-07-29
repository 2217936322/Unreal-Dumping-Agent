using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Utils = Unreal_Dumping_Agent.UtilsHelper.Utils;

namespace Unreal_Dumping_Agent.Tools.SdkGen
{
    public interface IEngineStruct
    {
        void FixPointers();
    }

    public static class EngineClasses
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class FPointer : IEngineStruct
        {
            public IntPtr Dummy;

            public void FixPointers() => Utils.FixPointers(this);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class FQWord
        {
            public int A;
            public int B;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class FName
        {
            public int ComparisonIndex;
            public int Number;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        // ReSharper disable once InconsistentNaming
        public class TArray : IEngineStruct
        {
            public IntPtr Data;
            public int Count;
            public int Max;

            public bool IsValidIndex(int index) => index < Count;
            public void FixPointers() => Utils.FixPointers(this);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class FString : TArray
        {
            public override string ToString()
            {
                return "";
            }
        }
    }
}
