﻿using SpringCard.GoogleVas;
using SpringCard.LibCs;
using SpringCard.LibCs.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SmartTapRdr
{
    static class Program
    {
        /// <summary>
        /// Point d'entrée principal de l'application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            SystemConsole.ReadArgs(args);
            Logger.ReadArgs(args);

            string ConfigFileName = null;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith("-"))
                    continue;
                if (ConfigFileName == null)
                    ConfigFileName = args[i];
            }

            if (!GoogleVasLicense.AutoLoad())
                Logger.Info("No license file");

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            MainForm f = new MainForm(args);
            if (ConfigFileName != null)
                f.LoadConfigFromFile(ConfigFileName);
            Application.Run(f);
        }
    }
}

