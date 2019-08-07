using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Unreal_Dumping_Agent.Json;
using Unreal_Dumping_Agent.Tools.SdkGen.Engine.UE4;

namespace Unreal_Dumping_Agent.Tools.SdkGen
{

    public enum UnrealVersion
    {
        Unreal4
    }

    /// <summary>
    /// Attribute to make read <see cref="JsonStruct"/> easy. (It's for fields only)
    /// <para>field name must equal <see cref="JsonVar"/> on <see cref="JsonStruct"/></para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class JsonMemoryVar : Attribute
    {
        public static bool HasAttribute<T>() => GetCustomAttributes(typeof(T)).Any(a => a is JsonMemoryVar);
        public static bool HasAttribute(FieldInfo fi) => fi.GetCustomAttributes().Any(a => a is JsonMemoryVar);
    }

    public interface IUnrealStruct
    {
        /// <summary>
        /// Get Type ID of UnrealStruct
        /// </summary>
        /// <returns></returns>
        int TypeId { get; }

        /// <summary>
        /// Get Class Type of Unreal object
        /// </summary>
        /// <returns></returns>
        GenericTypes.UEClass StaticClass { get; }
    }

    public interface IEngineStruct
    {
        /// <summary>
        /// Check if object was init, (aka data was read).
        /// </summary>
        bool Init { get; }

        /// <summary>
        ///  Address of object on Remote process
        /// </summary>
        IntPtr ObjAddress { get; }

        /// <summary>
        /// Get object type name
        /// </summary>
        string TypeName { get; }

        /// <summary>
        /// Get JsonStruct of this class
        /// </summary>
        JsonStruct JsonType { get; }

        /// <summary>
        /// Get Size of struct
        /// </summary>
        /// <returns></returns>
        int StructSize();

        /// <summary>
        /// Fix pointers for 32bit games on 64bit tool
        /// </summary>
        Task FixPointers();

        /// <summary>
        /// Read Object data from remote process
        /// </summary>
        /// <param name="address">Address of target on remote process</param>
        /// <returns>if success will return true</returns>
        Task<bool> ReadData(IntPtr address);

        /// <summary>
        /// Read Object data from remote process
        /// <para>Using <see cref="ObjAddress"/> as data address</para>
        /// </summary>
        /// <returns>if success will return true</returns>
        Task<bool> ReadData();
    }

    public enum SdkType
    {
        None,
        Internal,
        External
    }

    public struct PredefinedMember
    {
        public string Type;
        public string Name;

        public PredefinedMember(string type, string name)
        {
            Type = type;
            Name = name;
        }
    }

    public struct PredefinedMethod
    {
        public enum Type
        {
            Default,
            Inline
        }

        public string Signature;
        public string Body;
        public Type MethodType;

        /// <summary>
        /// Adds a predefined method which gets splitter in declaration and definition.
        /// </summary>
        /// <param name="signature">The method signature.</param>
        /// <param name="body">The method body.</param>
        /// <returns>The method.</returns>
        public static PredefinedMethod Default(string signature, string body)
        {
            return new PredefinedMethod { Signature = signature, Body = body, MethodType = Type.Default};
        }

        /// <summary>
        /// Adds a predefined method which gets included as an inline method.
        /// </summary>
        /// <param name="body">The method body.</param>
        /// <returns>The method.</returns>
        public static PredefinedMethod Inline(string body)
        {
            return new PredefinedMethod { Signature = string.Empty, Body = body, MethodType = Type.Inline };
        }

    }
}
