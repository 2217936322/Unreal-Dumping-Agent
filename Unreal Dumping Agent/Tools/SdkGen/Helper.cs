using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Unreal_Dumping_Agent.Json;

namespace Unreal_Dumping_Agent.Tools.SdkGen
{
    /// <summary>
    /// Attribute to make read <see cref="JsonStruct"/> easy. (It's for fields only)
    /// <para>field name must equal <see cref="JsonVar"/> on <see cref="JsonStruct"/></para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class UnrealMemoryVar : Attribute
    {
        public static bool HasAttribute<T>() => GetCustomAttributes(typeof(T)).Any(a => a is UnrealMemoryVar);
        public static bool HasAttribute(FieldInfo fi) => fi.GetCustomAttributes().Any(a => a is UnrealMemoryVar);
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
}
