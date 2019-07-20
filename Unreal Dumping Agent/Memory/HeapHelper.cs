using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Unreal_Dumping_Agent.Memory
{
    public static class HeapHelper
    {
        public class StructAllocer<TStruct> : IDisposable where TStruct : struct
        {
            public IntPtr Ptr { get; private set; }
            public TStruct ManageStruct { get; private set; }

            public StructAllocer()
            {
                Ptr = Marshal.AllocHGlobal(Marshal.SizeOf<TStruct>());
            }

            ~StructAllocer()
            {
                if (Ptr == IntPtr.Zero) return;

                Marshal.FreeHGlobal(Ptr);
                Ptr = IntPtr.Zero;
            }

            /// <summary>
            /// Update unmanaged data from <see cref="Ptr"/> to managed struct
            /// </summary>
            public bool Update()
            {
                if (Ptr == IntPtr.Zero)
                    return false;

                ManageStruct = Marshal.PtrToStructure<TStruct>(Ptr);
                return true;
            }

            public void Dispose()
            {
                Marshal.FreeHGlobal(Ptr);
                Ptr = IntPtr.Zero;
                GC.SuppressFinalize(this);
            }

            public static implicit operator IntPtr(StructAllocer<TStruct> w)
            {
                return w.Ptr;
            }
        }

        public class StringAllocer : IDisposable
        {
            public enum StringType
            {
                Ansi,
                Unicode
            }

            public IntPtr Ptr { get; private set; }
            public int Length { get; set; }
            public StringType StrType { get; }
            public string ManageString { get; private set; }

            public StringAllocer(int len, StringType stringType)
            {
                StrType = stringType;
                Length = len;
                Ptr = Marshal.AllocHGlobal(Length);
            }

            ~StringAllocer()
            {
                if (Ptr == IntPtr.Zero) return;

                Marshal.FreeHGlobal(Ptr);
                Ptr = IntPtr.Zero;
            }

            /// <summary>
            /// Change size of allocated string.
            /// </summary>
            /// <param name="len">New size of string</param>
            public void ReSize(int len)
            {
                Length = len;
                Ptr = Marshal.ReAllocHGlobal(Ptr, (IntPtr)len);
                Update();
            }

            /// <summary>
            /// Update unmanaged data from <see cref="Ptr"/> to managed struct
            /// </summary>
            public bool Update()
            {
                if (Ptr == IntPtr.Zero)
                    return false;

                switch (StrType)
                {
                    case StringType.Ansi:
                        ManageString = Marshal.PtrToStringAnsi(Ptr);
                        break;
                    case StringType.Unicode:
                        ManageString = Marshal.PtrToStringUni(Ptr);
                        break;
                }

                return true;
            }

            public void Dispose()
            {
                Marshal.FreeHGlobal(Ptr);
                Ptr = IntPtr.Zero;
                GC.SuppressFinalize(this);
            }

            public static implicit operator IntPtr(StringAllocer w)
            {
                return w.Ptr;
            }

            public static implicit operator string(StringAllocer w)
            {
                return w.ManageString;
            }
        }
    }
}
