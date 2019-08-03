using System;
using System.Collections.Generic;
using System.Text;

namespace Unreal_Dumping_Agent.Tools.SdkGen.Engine.UE4
{
    // ReSharper disable once InconsistentNaming
    public enum UEPropertyFlags : ulong
    {
        Edit = 0x0000000000000001,
        ConstParm = 0x0000000000000002,
        BlueprintVisible = 0x0000000000000004,
        ExportObject = 0x0000000000000008,
        BlueprintReadOnly = 0x0000000000000010,
        Net = 0x0000000000000020,
        EditFixedSize = 0x0000000000000040,
        Parm = 0x0000000000000080,
        OutParm = 0x0000000000000100,
        ZeroConstructor = 0x0000000000000200,
        ReturnParm = 0x0000000000000400,
        DisableEditOnTemplate = 0x0000000000000800,
        Transient = 0x0000000000002000,
        Config = 0x0000000000004000,
        DisableEditOnInstance = 0x0000000000010000,
        EditConst = 0x0000000000020000,
        GlobalConfig = 0x0000000000040000,
        InstancedReference = 0x0000000000080000,
        DuplicateTransient = 0x0000000000200000,
        SubobjectReference = 0x0000000000400000,
        SaveGame = 0x0000000001000000,
        NoClear = 0x0000000002000000,
        ReferenceParm = 0x0000000008000000,
        BlueprintAssignable = 0x0000000010000000,
        Deprecated = 0x0000000020000000,
        IsPlainOldData = 0x0000000040000000,
        RepSkip = 0x0000000080000000,
        RepNotify = 0x0000000100000000,
        Interp = 0x0000000200000000,
        NonTransactional = 0x0000000400000000,
        EditorOnly = 0x0000000800000000,
        NoDestructor = 0x0000001000000000,
        AutoWeak = 0x0000004000000000,
        ContainsInstancedReference = 0x0000008000000000,
        AssetRegistrySearchable = 0x0000010000000000,
        SimpleDisplay = 0x0000020000000000,
        AdvancedDisplay = 0x0000040000000000,
        Protected = 0x0000080000000000,
        BlueprintCallable = 0x0000100000000000,
        BlueprintAuthorityOnly = 0x0000200000000000,
        TextExportTransient = 0x0000400000000000,
        NonPIEDuplicateTransient = 0x0000800000000000,
        ExposeOnSpawn = 0x0001000000000000,
        PersistentInstance = 0x0002000000000000,
        UObjectWrapper = 0x0004000000000000,
        HasGetValueTypeHash = 0x0008000000000000,
        NativeAccessSpecifierPublic = 0x0010000000000000,
        NativeAccessSpecifierProtected = 0x0020000000000000,
        NativeAccessSpecifierPrivate = 0x0040000000000000
    }
    public class PropertyFlags
    {
        public static bool And(UEPropertyFlags lhs, UEPropertyFlags rhs) => ((int)lhs & (int)rhs) == (int)rhs;
        public static string StringifyFlags(UEPropertyFlags flags)
        {
            var buffer = new List<string>();

            if (flags.HasFlag(UEPropertyFlags.Edit)) buffer.Add("Edit");
            if (flags.HasFlag(UEPropertyFlags.ConstParm)) buffer.Add("ConstParm");
            if (flags.HasFlag(UEPropertyFlags.BlueprintVisible)) buffer.Add("BlueprintVisible");
            if (flags.HasFlag(UEPropertyFlags.ExportObject)) buffer.Add("ExportObject");
            if (flags.HasFlag(UEPropertyFlags.BlueprintReadOnly)) buffer.Add("BlueprintReadOnly");
            if (flags.HasFlag(UEPropertyFlags.Net)) buffer.Add("Net");
            if (flags.HasFlag(UEPropertyFlags.EditFixedSize)) buffer.Add("EditFixedSize");
            if (flags.HasFlag(UEPropertyFlags.Parm)) buffer.Add("Parm");
            if (flags.HasFlag(UEPropertyFlags.OutParm)) buffer.Add("OutParm");
            if (flags.HasFlag(UEPropertyFlags.ZeroConstructor)) buffer.Add("ZeroConstructor");
            if (flags.HasFlag(UEPropertyFlags.ReturnParm)) buffer.Add("ReturnParm");
            if (flags.HasFlag(UEPropertyFlags.DisableEditOnTemplate)) buffer.Add("DisableEditOnTemplate");
            if (flags.HasFlag(UEPropertyFlags.Transient)) buffer.Add("Transient");
            if (flags.HasFlag(UEPropertyFlags.Config)) buffer.Add("Config");
            if (flags.HasFlag(UEPropertyFlags.DisableEditOnInstance)) buffer.Add("DisableEditOnInstance");
            if (flags.HasFlag(UEPropertyFlags.EditConst)) buffer.Add("EditConst");
            if (flags.HasFlag(UEPropertyFlags.GlobalConfig)) buffer.Add("GlobalConfig");
            if (flags.HasFlag(UEPropertyFlags.InstancedReference)) buffer.Add("InstancedReference");
            if (flags.HasFlag(UEPropertyFlags.DuplicateTransient)) buffer.Add("DuplicateTransient");
            if (flags.HasFlag(UEPropertyFlags.SubobjectReference)) buffer.Add("SubobjectReference");
            if (flags.HasFlag(UEPropertyFlags.SaveGame)) buffer.Add("SaveGame");
            if (flags.HasFlag(UEPropertyFlags.NoClear)) buffer.Add("NoClear");
            if (flags.HasFlag(UEPropertyFlags.ReferenceParm)) buffer.Add("ReferenceParm");
            if (flags.HasFlag(UEPropertyFlags.BlueprintAssignable)) buffer.Add("BlueprintAssignable");
            if (flags.HasFlag(UEPropertyFlags.Deprecated)) buffer.Add("Deprecated");
            if (flags.HasFlag(UEPropertyFlags.IsPlainOldData)) buffer.Add("IsPlainOldData");
            if (flags.HasFlag(UEPropertyFlags.RepSkip)) buffer.Add("RepSkip");
            if (flags.HasFlag(UEPropertyFlags.RepNotify)) buffer.Add("RepNotify");
            if (flags.HasFlag(UEPropertyFlags.Interp)) buffer.Add("Interp");
            if (flags.HasFlag(UEPropertyFlags.NonTransactional)) buffer.Add("NonTransactional");
            if (flags.HasFlag(UEPropertyFlags.EditorOnly)) buffer.Add("EditorOnly");
            if (flags.HasFlag(UEPropertyFlags.NoDestructor)) buffer.Add("NoDestructor");
            if (flags.HasFlag(UEPropertyFlags.AutoWeak)) buffer.Add("AutoWeak");
            if (flags.HasFlag(UEPropertyFlags.ContainsInstancedReference)) buffer.Add("ContainsInstancedReference");
            if (flags.HasFlag(UEPropertyFlags.AssetRegistrySearchable)) buffer.Add("AssetRegistrySearchable");
            if (flags.HasFlag(UEPropertyFlags.SimpleDisplay)) buffer.Add("SimpleDisplay");
            if (flags.HasFlag(UEPropertyFlags.AdvancedDisplay)) buffer.Add("AdvancedDisplay");
            if (flags.HasFlag(UEPropertyFlags.Protected)) buffer.Add("Protected");
            if (flags.HasFlag(UEPropertyFlags.BlueprintCallable)) buffer.Add("BlueprintCallable");
            if (flags.HasFlag(UEPropertyFlags.BlueprintAuthorityOnly)) buffer.Add("BlueprintAuthorityOnly");
            if (flags.HasFlag(UEPropertyFlags.TextExportTransient)) buffer.Add("TextExportTransient");
            if (flags.HasFlag(UEPropertyFlags.NonPIEDuplicateTransient)) buffer.Add("NonPIEDuplicateTransient");
            if (flags.HasFlag(UEPropertyFlags.ExposeOnSpawn)) buffer.Add("ExposeOnSpawn");
            if (flags.HasFlag(UEPropertyFlags.PersistentInstance)) buffer.Add("PersistentInstance");
            if (flags.HasFlag(UEPropertyFlags.UObjectWrapper)) buffer.Add("UObjectWrapper");
            if (flags.HasFlag(UEPropertyFlags.HasGetValueTypeHash)) buffer.Add("HasGetValueTypeHash");
            if (flags.HasFlag(UEPropertyFlags.NativeAccessSpecifierPublic)) buffer.Add("NativeAccessSpecifierPublic");
            if (flags.HasFlag(UEPropertyFlags.NativeAccessSpecifierProtected)) buffer.Add("NativeAccessSpecifierProtected");
            if (flags.HasFlag(UEPropertyFlags.NativeAccessSpecifierPrivate)) buffer.Add("NativeAccessSpecifierPrivate");

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
