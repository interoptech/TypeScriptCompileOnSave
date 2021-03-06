﻿using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TypeScriptCompileOnSave
{
    public static class VsHelpers
    {
        public static TInterface GetService<TService, TInterface>() where TService : class where TInterface : class
        {
            return (TInterface)ServiceProvider.GlobalProvider.GetService(typeof(TService));
        }

        public static Version GetVisualStudioVersion()
        {
            var process = System.Diagnostics.Process.GetCurrentProcess();
            System.Diagnostics.FileVersionInfo v = process.MainModule.FileVersionInfo;

            return new Version(v.ProductMajorPart, v.ProductMinorPart, v.ProductBuildPart);
        }

        public static bool FileExistAtOrAbove(string sourceFile, string fileNameToLookFor, out string directory)
        {
            directory = null;
            var currentDir = new DirectoryInfo(Path.GetDirectoryName(sourceFile));

            while (currentDir != null)
            {
                string config = Path.Combine(currentDir.FullName, fileNameToLookFor);

                if (File.Exists(config))
                {
                    directory = currentDir.FullName;
                    return true;
                }

                currentDir = currentDir.Parent;
            }

            return false;
        }

        // From http://stackoverflow.com/a/17936541/1074470
        public static Task<bool> WaitForExitAsync(this System.Diagnostics.Process process, TimeSpan timeout)
        {
            var processWaitObject = new ManualResetEvent(false)
            {
                SafeWaitHandle = new SafeWaitHandle(process.Handle, false)
            };

            var tcs = new TaskCompletionSource<bool>();
            RegisteredWaitHandle registeredProcessWaitHandle = null;

            registeredProcessWaitHandle = ThreadPool.RegisterWaitForSingleObject(
                processWaitObject,
                (state, timedOut) =>
                {
                    if (!timedOut)
                    {
                        registeredProcessWaitHandle.Unregister(null);
                    }

                    processWaitObject.Dispose();
                    tcs.SetResult(!timedOut);
                },
                null /* state */,
                timeout,
                true /* executeOnlyOnce */);

            return tcs.Task;
        }

        public static void OpenFileAndSelect(this DTE dte, string file)
        {
            dte.ItemOperations.OpenFile(file);

            Command cmd = dte.Commands.Item("SolutionExplorer.SyncWithActiveDocument");

            if (cmd != null && cmd.IsAvailable)
            {
                dte.Commands.Raise(cmd.Guid, cmd.ID, null, null);
            }
        }
    }
}
