/*
 * MassEffectModder
 *
 * Copyright (C) 2014-2017 Pawel Kolodziejski <aquadran at users.sourceforge.net>
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.

 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.

 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301, USA.
 *
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MassEffectModder
{
    public class CachePackageMgr
    {
        public List<Package> packages;
        MainWindow mainWindow;
        static Installer _installer;

        public CachePackageMgr(MainWindow main, Installer installer)
        {
            packages = new List<Package>();
            mainWindow = main;
            _installer = installer;
        }

        public Package OpenPackage(string path, bool headerOnly = false)
        {
            if (!packages.Exists(p => p.packagePath == path))
            {
                Package pkg = new Package(path, headerOnly);
                packages.Add(pkg);
                return pkg;
            }
            else
            {
                return packages.Find(p => p.packagePath == path);
            }
        }

        public void ClosePackageWithoutSave(Package package)
        {
            int index = packages.IndexOf(package);
            packages[index].Dispose();
            packages.RemoveAt(index);
        }

        public void CloseAllWithoutSave()
        {
            foreach (Package pkg in packages)
            {
                pkg.Dispose();
            }
            packages.Clear();
        }

        public void CloseAllWithSave()
        {
            for (int i = 0; i < packages.Count; i++)
            {
                Package pkg = packages[i];
                if (mainWindow != null)
                    mainWindow.updateStatusLabel2("Saving package " + (i + 1) + " of " + packages.Count);
                if (_installer != null)
                    _installer.updateStatusStore("Progress... " + (i * 100 / packages.Count) + " % ");
                pkg.SaveToFile();
                pkg.Dispose();
            }

            if (GameData.gameType == MeType.ME3_TYPE)
            {
                updateMainTOC();
                updateDLCsTOC();
            }

            if (mainWindow != null)
                mainWindow.updateStatusLabel2("");
            packages.Clear();
        }

        static public void updateMainTOC()
        {
            List<string> mainFiles = Directory.GetFiles(GameData.MainData, "*.pcc", SearchOption.AllDirectories).Where(item => item.EndsWith(".pcc", StringComparison.OrdinalIgnoreCase)).ToList();
            mainFiles.AddRange(Directory.GetFiles(GameData.MainData, "*.tfc", SearchOption.AllDirectories).Where(item => item.EndsWith(".tfc", StringComparison.OrdinalIgnoreCase)).ToList());
            string tocFilename = Path.Combine(GameData.bioGamePath, "PCConsoleTOC.bin");
            if (!File.Exists(tocFilename))
            {
                if (_installer == null)
                    MessageBox.Show("ERROR: File at " + tocFilename + " is missing!");
                return;
            }
            TOCBinFile tocFile = new TOCBinFile(tocFilename);
            for (int i = 0; i < mainFiles.Count; i++)
            {
                int pos = mainFiles[i].IndexOf("BioGame", StringComparison.OrdinalIgnoreCase);
                string filename = mainFiles[i].Substring(pos);
                tocFile.updateFile(filename, mainFiles[i]);
            }
            tocFile.saveToFile(Path.Combine(GameData.bioGamePath, @"PCConsoleTOC.bin"));
        }

        static public void updateDLCsTOC()
        {
            if (!Directory.Exists(GameData.DLCData))
                return;

            List<string> DLCs = Directory.GetDirectories(GameData.DLCData).ToList();
            for (int i = 0; i < DLCs.Count; i++)
            {
                List<string> dlcFiles = Directory.GetFiles(DLCs[i], "*.pcc", SearchOption.AllDirectories).Where(item => item.EndsWith(".pcc", StringComparison.OrdinalIgnoreCase)).ToList();
                if (dlcFiles.Count == 0)
                    continue;
                dlcFiles.AddRange(Directory.GetFiles(DLCs[i], "*.tfc", SearchOption.AllDirectories).Where(item => item.EndsWith(".tfc", StringComparison.OrdinalIgnoreCase)).ToList());
                string DLCname = Path.GetFileName(DLCs[i]);
                string tocFilename = Path.Combine(GameData.DLCData, DLCname, "PCConsoleTOC.bin");
                if (!File.Exists(tocFilename))
                {
                    if (_installer == null)
                        MessageBox.Show("ERROR: File at " + tocFilename + " is missing!");
                    continue;
                }
                TOCBinFile tocDLC = new TOCBinFile(Path.Combine(tocFilename));
                for (int f = 0; f < dlcFiles.Count; f++)
                {
                    int pos = dlcFiles[f].IndexOf(DLCname + "\\", StringComparison.OrdinalIgnoreCase);
                    string filename = dlcFiles[f].Substring(pos + DLCname.Length + 1);
                    tocDLC.updateFile(filename, dlcFiles[f]);
                }
                tocDLC.saveToFile(Path.Combine(GameData.DLCData, DLCname, "PCConsoleTOC.bin"));
            }
        }
    }
}
