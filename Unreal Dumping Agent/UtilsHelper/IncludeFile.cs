using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unreal_Dumping_Agent.Tools.SdkGen;

namespace Unreal_Dumping_Agent.UtilsHelper
{
    public abstract class IncludeFile<TLang> where TLang : SdkLang
    {
        public TLang TargetLang { get; }
        public string SdkPath { get; }

        public abstract string FileName { get; set; }

        protected IncludeFile() : this((TLang)Generator.GenLang, Generator.SdkPath)
        {
        }

        protected IncludeFile(TLang targetLang, string sdkPath)
        {
            TargetLang = targetLang;
            SdkPath = sdkPath;
        }
        public abstract void Process(string includePath);

        public void CreateFile()
        {
            File.CreateText($@"{SdkPath}\{FileName}").Close();
        }
        public static void CreateFile(string sdkPah, string fileName)
        {
            File.CreateText($@"{sdkPah}\{fileName}").Close();
        }
        public CorrmStringBuilder ReadThisFile(string includePath)
        {
            return new CorrmStringBuilder(File.ReadAllText($@"{includePath}\{FileName}"));
        }
        public void CopyToSdk(CorrmStringBuilder fileStr)
        {
            File.WriteAllText($@"{SdkPath}\{FileName}", fileStr.ToString());
        }
        public void CopyToSdk(string fileStr)
        {
            File.WriteAllText($@"{SdkPath}\{FileName}", fileStr);
        }
        public void AppendToSdk(string text)
        {
            File.AppendAllText($@"{SdkPath}\{FileName}", text);
        }
        public static void AppendToSdk(string sdkPath, string fileName, string text)
        {
            File.AppendAllText($@"{sdkPath}\{fileName}", text);
        }
        public static void WriteToSdk(string sdkPath, string fileName, string text)
        {
            File.WriteAllText($@"{sdkPath}\{fileName}", text);
        }
    }

}
