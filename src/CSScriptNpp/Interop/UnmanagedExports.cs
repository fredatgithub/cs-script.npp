﻿using NppPlugin.DllExport;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace CSScriptNpp
{
    class UnmanagedExports
    {
        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        static bool isUnicode()
        {
            return true;
        }

        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        static void setInfo(NppData notepadPlusData)
        {
            //System.Diagnostics.Debug.Assert(false);
            Bootstrapper.Init();

            Plugin.NppData = notepadPlusData;

            InitPlugin();
        }

        static void InitPlugin()
        {
            CSScriptIntellisense.Plugin.NppData._nppHandle = Plugin.NppData._nppHandle;
            CSScriptIntellisense.Plugin.NppData._scintillaMainHandle = Plugin.NppData._scintillaMainHandle;
            CSScriptIntellisense.Plugin.NppData._scintillaSecondHandle = Plugin.NppData._scintillaSecondHandle;

            Intellisense.EnsureIntellisenseIntegration();

            CSScriptNpp.Plugin.CommandMenuInit(); //this will also call CSScriptIntellisense.Plugin.CommandMenuInit

            foreach (var item in CSScriptIntellisense.Plugin.FuncItems.Items)
                Plugin.FuncItems.Add(item.ToLocal());

            CSScriptIntellisense.Plugin.FuncItems.Items.Clear();

            Debugger.OnFrameChanged += () => Npp.OnCalltipRequest(-1); //clear_all_cache
        }

        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        static IntPtr getFuncsArray(ref int nbF)
        {
            nbF = Plugin.FuncItems.Items.Count;
            return Plugin.FuncItems.NativePointer;
        }

        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        static uint messageProc(uint Message, IntPtr wParam, IntPtr lParam)
        {
            return 1;
        }

        static IntPtr _ptrPluginName = IntPtr.Zero;

        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        static IntPtr getName()
        {
            if (_ptrPluginName == IntPtr.Zero)
                _ptrPluginName = Marshal.StringToHGlobalUni(Plugin.PluginName);
            return _ptrPluginName;
        }

        const int _SC_MARGE_SYBOLE = 1; //bookmark and breakpoint margin
        const int SCI_CTRL = 2; //Ctrl pressed modifier for SCN_MARGINCLICK

        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        static void beNotified(IntPtr notifyCode)
        {
            try
            {
                SCNotification nc = (SCNotification)Marshal.PtrToStructure(notifyCode, typeof(SCNotification));
                if (nc.nmhdr.code == (uint)NppMsg.NPPN_READY)
                {
                    CSScriptIntellisense.Plugin.OnNppReady();
                    CSScriptNpp.Plugin.OnNppReady();
                    Npp.SetCalltipTime(200);
                }
                else if (nc.nmhdr.code == (uint)NppMsg.NPPN_TBMODIFICATION)
                {
                    CSScriptNpp.Plugin.OnToolbarUpdate();
                }
                else if (nc.nmhdr.code == (uint)SciMsg.SCN_CHARADDED)
                {
                    CSScriptIntellisense.Plugin.OnCharTyped((char)nc.ch);
                }
                else if (nc.nmhdr.code == (uint)SciMsg.SCN_MARGINCLICK)
                {
                    if (nc.margin == _SC_MARGE_SYBOLE && nc.modifiers == SCI_CTRL)
                    {

                        int lineClick = Npp.GetLineFromPosition(nc.position);
                        Debugger.ToggleBreakpoint(lineClick);
                    }
                }
                else if (nc.nmhdr.code == (uint)SciMsg.SCN_DWELLSTART) //tooltip
                {
                    //Npp.ShowCalltip(nc.position, "\u0001  1 of 3 \u0002  test tooltip " + Environment.TickCount);
                    //Npp.ShowCalltip(nc.position, CSScriptIntellisense.Npp.GetWordAtPosition(nc.position));
                    //                    tooltip = @"Creates all directories and subdirectories as specified by path.

                    Npp.OnCalltipRequest(nc.position);
                }
                else if (nc.nmhdr.code == (uint)SciMsg.SCN_DWELLEND)
                {
                    Npp.CancelCalltip();
                }
                else if (nc.nmhdr.code == (uint)NppMsg.NPPN_BUFFERACTIVATED)
                {
                    CSScriptIntellisense.Plugin.OnCurrentFileChanegd();
                    CSScriptNpp.Plugin.OnCurrentFileChanged();
                    Debugger.OnCurrentFileChanged();
                }
                else if (nc.nmhdr.code == (uint)NppMsg.NPPN_FILEOPENED)
                {
                    string file = Npp.GetTabFile((int)nc.nmhdr.idFrom);
                    Debugger.LoadBreakPointsFor(file);
                }
                else if (nc.nmhdr.code == (uint)NppMsg.NPPN_FILESAVED || nc.nmhdr.code == (uint)NppMsg.NPPN_FILEBEFORECLOSE)
                {
                    string file = Npp.GetTabFile((int)nc.nmhdr.idFrom);
                    Debugger.SaveBreakPointsFor(file);
                }
                else if (nc.nmhdr.code == (uint)NppMsg.NPPN_SHUTDOWN)
                {
                    Marshal.FreeHGlobal(_ptrPluginName);

                    CSScriptNpp.Plugin.CleanUp();
                }

                Plugin.OnNotification(nc);
            }
            catch { }//this is indeed the last line of defense as all CS-S calls have the error handling inside 
        }
    }
}