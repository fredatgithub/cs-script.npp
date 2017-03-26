using System;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace CSScriptNpp
{
    class Bootstrapper
    {
        public static void Init()
        {
            try
            {
                //Debug.Assert(false);
                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
                //RoslynHelper.Init();
                //must be a separate method to allow assembly probing
                ConnectPlugins();
            }
            catch(Exception e)
            {
                var customLog = Path.Combine(Plugin.LogDir, "last_startup_error.txt");
                File.WriteAllText(customLog, e.ToString());
                throw;
            }
        }

        static void ConnectPlugins()
        {
            try
            {
                CSScriptHelper.NotifyClient = CSScriptIntellisense.Npp.SetStatusbarLabel;
                CSScriptIntellisense.CSScriptHelper.GetEngineExe = () => CSScriptHelper.cscs_exe;
                UltraSharp.Cecil.Reflector.GetCodeCompileOutput = CSScriptHelper.GetCodeCompileOutput;
            }
            catch(Exception e)
            {
                e.LogAsError();
            }

            CSScriptIntellisense.Logger.Error = PluginLogger.Error;
            CSScriptIntellisense.Logger.Debug = PluginLogger.Debug;
            Logger.LogAsDebug("Plugin has been loaded");
        }

        static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string rootDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            try
            {
                if (args.Name.StartsWith("CSScriptIntellisense,"))
                    return Assembly.LoadFrom(Path.Combine(rootDir, @"CSScriptNpp\CSScriptIntellisense.dll"));
                else if (args.Name.StartsWith("CSScriptLibrary,"))
                    return Assembly.LoadFrom(Path.Combine(rootDir, @"CSScriptNpp\CSScriptLibrary.dll"));
                else if (args.Name.StartsWith("NLog,"))
                    return Assembly.LoadFrom(Path.Combine(rootDir, @"CSScriptNpp\NLog.dll"));
                else if (args.Name == Assembly.GetExecutingAssembly().FullName)
                    return Assembly.GetExecutingAssembly();
            }
            catch { }
            return null;
            //return RoslynHelper.CurrentDomain_AssemblyResolve(sender, args);
        }

        //for future use
        static Assembly Probe(string dir, string asmName)
        {
            var file = Path.Combine(dir, asmName.Split(',')[0] + ".dll");
            if (File.Exists(file))
                return Assembly.LoadFrom(file);
            else
                return null;
        }
    }

    //internal class RoslynHelper
    //{
    //    static bool initialized;
    //    static string probingDir;
    //    static public void Init()
    //    {
    //        if (!initialized)
    //        {
    //            Debug.Assert(false);
    //            initialized = true;

    //            probingDir = Assembly.GetExecutingAssembly().Location.GetDirName();
    //            if (!File.Exists(probingDir.PathJoin("Microsoft.CodeAnalysis.dll")))
    //            {
    //                probingDir = probingDir.PathJoin("Roslyn.Intellisense");
    //                if (!File.Exists(probingDir.PathJoin("Microsoft.CodeAnalysis.dll")))
    //                    probingDir = probingDir.PathJoin("Roslyn");
    //            }

    //            if (File.Exists(probingDir.PathJoin("Microsoft.CodeAnalysis.dll")))
    //                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
    //        }
    //    }

    //    public static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
    //    {
    //        var file = probingDir.PathJoin(args.Name.Split(',').First() + ".dll");
    //        if (File.Exists(file))
    //            return Assembly.LoadFrom(file);
    //        return null;
    //    }
    //}
}