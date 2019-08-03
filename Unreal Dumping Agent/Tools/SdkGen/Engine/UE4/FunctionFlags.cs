using System;
using System.Collections.Generic;
using System.Text;

namespace Unreal_Dumping_Agent.Tools.SdkGen.Engine.UE4
{
    // ReSharper disable once InconsistentNaming
    public enum UEFunctionFlags : uint
    {
        Final = 0x00000001,
        RequiredApi = 0x00000002,
        BlueprintAuthorityOnly = 0x00000004,
        BlueprintCosmetic = 0x00000008,
        Net = 0x00000040,
        NetReliable = 0x00000080,
        NetRequest = 0x00000100,
        Exec = 0x00000200,
        Native = 0x00000400,
        Event = 0x00000800,
        NetResponse = 0x00001000,
        Static = 0x00002000,
        NetMulticast = 0x00004000,
        MulticastDelegate = 0x00010000,
        Public = 0x00020000,
        Private = 0x00040000,
        Protected = 0x00080000,
        Delegate = 0x00100000,
        NetServer = 0x00200000,
        HasOutParms = 0x00400000,
        HasDefaults = 0x00800000,
        NetClient = 0x01000000,
        DllImport = 0x02000000,
        BlueprintCallable = 0x04000000,
        BlueprintEvent = 0x08000000,
        BlueprintPure = 0x10000000,
        Const = 0x40000000,
        NetValidate = 0x80000000
    }

    public class FunctionFlags
    {
        public static bool And(UEFunctionFlags lhs, UEFunctionFlags rhs) => ((int)lhs & (int)rhs) == (int)rhs;
        public static string StringifyFlags(UEFunctionFlags flags)
        {
            var buffer = new List<string>();

            if (flags.HasFlag(UEFunctionFlags.Final)) buffer.Add("Final");
            if (flags.HasFlag(UEFunctionFlags.RequiredApi)) buffer.Add("RequiredAPI");
            if (flags.HasFlag(UEFunctionFlags.BlueprintAuthorityOnly)) buffer.Add("BlueprintAuthorityOnly");
            if (flags.HasFlag(UEFunctionFlags.BlueprintCosmetic)) buffer.Add("BlueprintCosmetic");
            if (flags.HasFlag(UEFunctionFlags.Net)) buffer.Add("Net");
            if (flags.HasFlag(UEFunctionFlags.NetReliable)) buffer.Add("NetReliable");
            if (flags.HasFlag(UEFunctionFlags.NetRequest)) buffer.Add("NetRequest");
            if (flags.HasFlag(UEFunctionFlags.Exec)) buffer.Add("Exec");
            if (flags.HasFlag(UEFunctionFlags.Native)) buffer.Add("Native");
            if (flags.HasFlag(UEFunctionFlags.Event)) buffer.Add("Event");
            if (flags.HasFlag(UEFunctionFlags.NetResponse)) buffer.Add("NetResponse");
            if (flags.HasFlag(UEFunctionFlags.Static)) buffer.Add("Static");
            if (flags.HasFlag(UEFunctionFlags.NetMulticast)) buffer.Add("NetMulticast");
            if (flags.HasFlag(UEFunctionFlags.MulticastDelegate)) buffer.Add("MulticastDelegate");
            if (flags.HasFlag(UEFunctionFlags.Public)) buffer.Add("Public");
            if (flags.HasFlag(UEFunctionFlags.Private)) buffer.Add("Private");
            if (flags.HasFlag(UEFunctionFlags.Protected)) buffer.Add("Protected");
            if (flags.HasFlag(UEFunctionFlags.Delegate)) buffer.Add("Delegate");
            if (flags.HasFlag(UEFunctionFlags.NetServer)) buffer.Add("NetServer");
            if (flags.HasFlag(UEFunctionFlags.HasOutParms)) buffer.Add("HasOutParms");
            if (flags.HasFlag(UEFunctionFlags.HasDefaults)) buffer.Add("HasDefaults");
            if (flags.HasFlag(UEFunctionFlags.NetClient)) buffer.Add("NetClient");
            if (flags.HasFlag(UEFunctionFlags.DllImport)) buffer.Add("DLLImport");
            if (flags.HasFlag(UEFunctionFlags.BlueprintCallable)) buffer.Add("BlueprintCallable");
            if (flags.HasFlag(UEFunctionFlags.BlueprintEvent)) buffer.Add("BlueprintEvent");
            if (flags.HasFlag(UEFunctionFlags.BlueprintPure)) buffer.Add("BlueprintPure");
            if (flags.HasFlag(UEFunctionFlags.Const)) buffer.Add("Const");
            if (flags.HasFlag(UEFunctionFlags.NetValidate)) buffer.Add("NetValidate");

            switch (buffer.Count)
            {
                case 0:
                    return string.Empty;
                case 1:
                    return buffer[0];
                default:
                    return string.Join(", ", buffer); // TODO: Check if it's good
            }
        }
    }
}
