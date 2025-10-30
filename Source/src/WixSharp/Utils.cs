#region Licence...

/*
The MIT License (MIT)
Copyright (c) 2014 Oleg Shilo
Permission is hereby granted,
free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

#endregion Licence...

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WixSharp.CommonTasks;
using WixSharp.Utilities;
using WixToolset.Dtf.WindowsInstaller;
using IO = System.IO;
using Reflection = System.Reflection;

#pragma warning disable IL3000 // Avoid accessing Assembly file path when publishing as a single file

namespace WixSharp
{
    /// <summary>
    /// Collection of a 'utility' routines.
    /// </summary>
    public static class Utils
    {
        internal static bool IsUpToDateCopyOf(this string destAssembly, string srcAssembly)
        {
            if (!destAssembly.FileExists() || !srcAssembly.FileExists())
                return false;

            var srcInfo = new IO.FileInfo(srcAssembly);
            var dstInfo = new IO.FileInfo(destAssembly);

            // check if the destination file is already copied and up-to-date
            if (IO.File.Exists(srcAssembly))
            {
                if (srcInfo.Length == dstInfo.Length &&
                    srcInfo.LastWriteTimeUtc <= dstInfo.LastWriteTimeUtc)
                    return true;

                // the destination file may be the signed version of the source file
                if (VerifyFileSignature.IsSigned(destAssembly) && !VerifyFileSignature.IsSigned(srcAssembly) && destAssembly.Contains(".wixsharp"))
                {
                    if (srcInfo.LastWriteTimeUtc <= dstInfo.LastWriteTimeUtc)
                        return true;
                }
            }
            return false;
        }

        internal static string ResolveClientAsm()
        {
            if (Compiler.ClientAssembly.IsEmpty())
                Compiler.ClientAssembly = Compiler.FindClientAssemblyInCallStack();

            return IsolateAsm(Compiler.ClientAssembly);
        }

        static string wixsharpBuildDir = null;

        internal static string WixsharpBuildDir
        {
            get
            {
                if (wixsharpBuildDir == null)
                {
                    if (Compiler.ClientAssembly.IsEmpty())
                        Compiler.ClientAssembly = Compiler.FindClientAssemblyInCallStack();
                    wixsharpBuildDir = Compiler.ClientAssembly.PathGetDirName().PathCombine(".wixsharp").EnsureDirExists();
                }
                return wixsharpBuildDir;
            }
        }

        internal static string IsolateAsm(this string file)
        {
            // restore the original assembly name as tools like MakeSfxCA do not like renamed assemblies

            var originalFile = Utils.OriginalAssemblyFile(file);
            var originalName = originalFile.PathGetFileNameWithoutExtension(); // assembly name

            var isolatedAssemblyFile = originalFile.PathChangeDirectory(WixsharpBuildDir);

            if (!isolatedAssemblyFile.IsUpToDateCopyOf(file))
            {
                IO.File.Copy(file, isolatedAssemblyFile, true);

                var clientPdb = file.PathChangeExtension(".pdb");

                if (clientPdb.FileExists())
                    clientPdb.CopyFileTo(isolatedAssemblyFile.PathChangeExtension(".pdb"), overwrite: true);
            }

            // last point of validation
            if (originalName != isolatedAssemblyFile.PathGetFileNameWithoutExtension())
            {
                Compiler.OutputWriteLine($"Error: assembly file name is inconsistent with the assembly name.\n  File name: {isolatedAssemblyFile}\n  Assembly name: {originalName}");
            }

            return isolatedAssemblyFile;
        }

        /// <summary>
        /// Combines two path strings.
        /// <para>
        /// It is a fix for unexpected behavior of System.IO.Path.Combine: Path.Combine(@"C:\Test", @"\Docs\readme.txt") return @"\Docs\readme.txt";
        /// </para>
        /// </summary>
        /// <param name="path1">The path1.</param>
        /// <param name="path2">The path2.</param>
        /// <returns></returns>
        public static string PathCombine(string path1, string path2)
        {
            var p1 = (path1 ?? "").ExpandEnvVars();
            var p2 = (path2 ?? "").ExpandEnvVars();

            if (p2.Length == 0)
            {
                return p1;
            }
            else if (p2.Length == 1 && p2[0] == IO.Path.DirectorySeparatorChar)
            {
                return p1;
            }
            else if (p2[0] == IO.Path.DirectorySeparatorChar)
            {
                if (p2[0] != p2[1])
                    return IO.Path.Combine(p1, p2.Substring(1));
            }

            return IO.Path.Combine(p1, p2);
        }

        /// <summary>
        /// Converts a path into a relative path with respect to another path..
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="referencePath">The reference path.</param>
        /// <returns></returns>
        public static string MakeRelativeTo(this string filePath, string referencePath)
        {
            //1 - 'Uri.MakeRelativeUri' doesn't work without *.config file
            //2 - Substring doesn't work for paths containing ..\..\

            char dirSeparator = IO.Path.DirectorySeparatorChar;
            string[] split(string path) => IO.Path.GetFullPath(path).Trim(dirSeparator).Split(dirSeparator);

            string[] absParts = split(filePath);
            string[] relParts = split(referencePath);

            int commonElementsLength = 0;
            do
            {
                if (string.Compare(absParts[commonElementsLength], relParts[commonElementsLength], true) != 0)
                    break;
            }
            while (++commonElementsLength < Math.Min(absParts.Length, relParts.Length));

            if (commonElementsLength == 0)
                //throw new ArgumentException("The two paths don't have common root.");
                return IO.Path.GetFullPath(filePath);

            var result = relParts.Skip(commonElementsLength)
                                 .Select(x => "..")
                                 .Concat(absParts.Skip(commonElementsLength))
                                 .ToArray();

            return string.Join(dirSeparator.ToString(), result);
        }

        internal static string[] AllConstStringValues<T>()
        {
            var fields = typeof(T).GetFields()
                                  .Where(f => f.IsStatic && f.IsPublic && f.IsLiteral && f.FieldType == typeof(string))
                                  .Select(f => f.GetValue(null) as string)
                                  .ToArray();

            return fields;
        }

        /// <summary>
        /// Gets the version of Windows. This method executes Windows command `ver` and returns the output string (version).
        /// </summary>
        /// <returns></returns>
        public static string GetWinVer()
        {
            var version = "";
            string batchFile = System.IO.Path.GetTempFileName().DeleteIfExists();
            batchFile += ".cmd";
            try
            {
                System.IO.File.WriteAllText(batchFile, "ver");
                var ver = new ExternalTool
                {
                    EchoOn = false,
                    ExePath = batchFile
                }.ConsoleRun(x => version = x); // capture the last line

                // Microsoft Windows [Version 10.0.22621.2861]
                return version.Split(' ').Last().Trim(']');
            }
            finally
            {
                batchFile.DeleteIfExists();
            }
        }

        internal static string GetTempDirectory()
        {
            string tempDir = IO.Path.GetTempFileName(); // always creates the file

            // delete the file and create the dir instead
            tempDir.DeleteIfExists();
            tempDir.EnsureDirExists();

            return tempDir;
        }

        internal static string OriginalAssemblyFile(string file)
        {
            //need to do it in a separate domain as we do not want to lock the assembly
            string dir = IO.Path.GetDirectoryName(IO.Path.GetFullPath(file));

            var asm = AppDomain.CurrentDomain
                               .GetAssemblies()
                               .FirstOrDefault(a => a.GetLocation().SamePathAs(file));

            // Get the original name to handle 'setup.cs.dll' vs 'setup.cs.compiled' cases

            string name;
            if (asm == null)
            {
                // Issue #835
                // Cannot use ReflectionOnlyLoad as it will throw if it is called more than once from the same AppDomain.
                // will use temp domain then
#if !NETCORE
                name = (string)ExecuteInTempDomain<AsmReflector>(asmReflector => asmReflector.AssemblyScopeName(file));
#else
                throw new NotImplementedException("The method is not implemented on .NET Core");
#endif
            }
            else
                name = asm.ManifestModule.ScopeName;

            return IO.Path.Combine(dir, name);
        }

#if NETCORE

        [Obsolete("The method is not implemented on .NET Core", true)]
#endif

        internal static void ExecuteInTempDomain<T>(Action<T> action) where T : MarshalByRefObject
        {
            ExecuteInTempDomain<T>(asm =>
            {
                action(asm);
                return null;
            });
        }

#if NETCORE

        [Obsolete("The method is not implemented on .NET Core", true)]
#endif

        internal static object ExecuteInTempDomain<T>(Func<T, object> action) where T : MarshalByRefObject
        {
            var domain = AppDomain.CurrentDomain.Clone();

            AppDomain.CurrentDomain.AssemblyResolve += Domain_AssemblyResolve;
            domain.AssemblyResolve += Domain_AssemblyResolve;
            try
            {
                var obj = domain.CreateInstanceFromAndUnwrap<T>();

                var result = action(obj);
                return result;
            }
            finally
            {
                domain.AssemblyResolve -= Domain_AssemblyResolve;
                AppDomain.CurrentDomain.AssemblyResolve -= Domain_AssemblyResolve;
                domain.Unload();
            }
        }

        static System.Reflection.Assembly Domain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (Compiler.AssemblyResolve != null)
                return Compiler.AssemblyResolve(sender, args);
            else
                return DefaultDomain_AssemblyResolve(sender, args);
        }

        static System.Reflection.Assembly DefaultDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            //args.Name -> "mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"

            string asmName = args.Name.Split(',').First() + ".dll";
            string wixSharpAsmLocation = IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            string potentialAsm = IO.Path.Combine(wixSharpAsmLocation, asmName);

            if (IO.File.Exists(potentialAsm))
                try
                {
                    return System.Reflection.Assembly.LoadFrom(potentialAsm);
                }
                catch { }

            return null;
        }

        internal static void Unload(this AppDomain domain)
        {
#if !NETCORE
            AppDomain.Unload(domain);
#endif
        }

        internal static T CreateInstanceFromAndUnwrap<T>(this AppDomain domain)
        {
            return (T)domain.CreateInstanceFromAndUnwrap(typeof(T).Assembly.Location, typeof(T).ToString());
        }

        internal static AppDomain Clone(this AppDomain domain, string name = null)
        {
#if !NETCORE
            var setup = new AppDomainSetup();
            setup.ApplicationBase = IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            setup.ShadowCopyFiles = "true";
            setup.ShadowCopyDirectories = setup.ApplicationBase;
            setup.PrivateBinPath = domain.BaseDirectory;
            return AppDomain.CreateDomain(name ?? Guid.NewGuid().ToString(), null, setup);
#endif
            throw new NotImplementedException();
        }

        internal static void EnsureFileDir(string file)
        {
            var dir = IO.Path.GetDirectoryName(file);
            if (!IO.Directory.Exists(dir))
                IO.Directory.CreateDirectory(dir);
        }

        /// <summary>
        /// Gets the program files directory.
        /// </summary>
        /// <value>
        /// The program files directory.
        /// </value>
        internal static string ProgramFilesDirectory
        {
            get
            {
                string programFilesDir = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                if ("".GetType().Assembly.Location.Contains("Framework64"))
                    programFilesDir += " (x86)"; //for x64 systems
                return programFilesDir;
            }
        }

        /// <summary>
        /// Returns the hash code for the instance of a string. It uses deterministic hash-code generation algorithm,
        /// which produces the same result on x86 and x64 OSs (ebject.GetHashCode doesn't).
        /// </summary>
        /// <param name="s">The string.</param>
        /// <returns></returns>
        public static int GetHashCode32(this string s)
        {
            char[] chars = s.ToCharArray();
            int lastCharInd = chars.Length - 1;
            int num1 = 0x15051505;
            int num2 = num1;
            int ind = 0;
            while (ind <= lastCharInd)
            {
                char ch = chars[ind];
                char nextCh = ++ind > lastCharInd ? '\0' : chars[ind];
                num1 = (((num1 << 5) + num1) + (num1 >> 0x1b)) ^ (nextCh << 16 | ch);
                if (++ind > lastCharInd)
                    break;
                ch = chars[ind];
                nextCh = ++ind > lastCharInd ? '\0' : chars[ind++];
                num2 = (((num2 << 5) + num2) + (num2 >> 0x1b)) ^ (nextCh << 16 | ch);
            }
            return num1 + num2 * 0x5d588b65;
        }
    }
}