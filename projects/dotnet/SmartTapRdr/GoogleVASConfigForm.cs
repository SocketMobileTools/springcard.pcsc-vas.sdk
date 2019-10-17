﻿using SpringCard.LibCs;
using SpringCard.GoogleVas;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SpringCard.LibCs.Windows;
using System.IO;

namespace SmartTapRdr
{
    public partial class GoogleVASConfigForm : Form
    {
        public GoogleVASConfigForm()
        {
            InitializeComponent();
        }

        private void ConfigForm_Load(object sender, EventArgs e)
        {
            RegistryCfgFile registry = RegistryCfgFile.OpenApplicationSectionReadOnly("TerminalSettings");
            GoogleVasConfig config = new GoogleVasConfig();
            config.Load(registry);
            ConfigToForm(config);
        }

        public GoogleVasConfig GetConfig()
        {
            RegistryCfgFile registry = RegistryCfgFile.OpenApplicationSectionReadOnly("TerminalSettings");
            GoogleVasConfig config = new GoogleVasConfig();
            config.Load(registry);
            return config;
        }

        private void ConfigToCheckbox(GoogleVasConfig config, CheckBox control)
        {
            string name = control.Name;

            if (name.StartsWith("cb"))
            {
                name = name.Substring(2);

                try
                {
                    bool value = (bool)config.GetFieldValue(name);
                    Logger.Debug("- {0}: {1}", name, value);
                    control.Checked = value;
                    control.Enabled = true;
                }
                catch
                {
                    Logger.Warning("Unknown config property: {0}", name);
                    control.Checked = false;
                    control.Enabled = false;
                }
            }
        }

        private void CheckboxToConfig(GoogleVasConfig config, CheckBox control)
        {
            string name = control.Name;

            if (name.StartsWith("cb"))
            {
                name = name.Substring(2);

                try
                {
                    config.SetFieldValue(name, control.Checked);
                }
                catch
                {
                    Logger.Warning("Unknown config property: {0}", name);
                }
            }
        }

        private void ConfigToText(GoogleVasConfig config, TextBox control)
        {
            string name = control.Name;

            if (name.StartsWith("ehex"))
            {
                name = name.Substring(4);

                try
                {
                    byte[] value = (byte[])config.GetFieldValue(name);
                    Logger.Debug("- {0}: {1}", name, BinConvert.ToHex(value));
                    control.Text = BinConvert.ToHex(value);
                    control.Enabled = true;
                }
                catch
                {
                    Logger.Warning("Unknown config property: {0}", name);
                    control.Text = "";
                    control.Enabled = false;
                }
            }
            else if (name.StartsWith("estr"))
            {
                name = name.Substring(4);

                try
                {
                    string value = (string)config.GetFieldValue(name);
                    Logger.Debug("- {0}: {1}", name, value);
                    control.Text = value;
                    control.Enabled = true;
                }
                catch
                {
                    Logger.Warning("Unknown config property: {0}", name);
                    control.Text = "";
                    control.Enabled = false;
                }
            }
        }

        private void TextToConfig(GoogleVasConfig config, TextBox control)
        {
            string name = control.Name;

            if (name.StartsWith("ehex"))
            {
                name = name.Substring(4);

                byte[] value;
                try
                {
                    value = BinConvert.HexToBytes(control.Text);
                }
                catch
                {
                    Logger.Trace("Invalid hex value for  {0}", name);
                    value = null;
                }

                try
                {
                    config.SetFieldValue(name, value);
                }
                catch
                {
                    Logger.Warning("Unknown config property: {0}", name);
                }
            }
            else if (name.StartsWith("estr"))
            {
                name = name.Substring(4);

                try
                {
                    config.SetFieldValue(name, control.Text);
                }
                catch
                {
                    Logger.Warning("Unknown config property: {0}", name);
                }
            }
        }

        private void ConfigToNum(GoogleVasConfig config, NumericUpDown control)
        {
            string name = control.Name;

            if (name.StartsWith("num"))
            {
                name = name.Substring(3);

                try
                {
                    uint value = (uint)config.GetFieldValue(name);
                    Logger.Debug("- {0}: {1}", name, value);
                    control.Value = value;
                    control.Enabled = true;
                }
                catch
                {
                    Logger.Warning("Unknown config property: {0}", name);
                    control.Value = 0;
                    control.Enabled = false;
                }
            }
        }

        private void NumToConfig(GoogleVasConfig config, NumericUpDown control)
        {
            string name = control.Name;

            if (name.StartsWith("num"))
            {
                name = name.Substring(3);

                try
                {
                    config.SetFieldValue(name, (uint) control.Value);
                }
                catch
                {
                    Logger.Warning("Unknown config property: {0}", name);
                }
            }
        }

        private void ConfigToControl(GoogleVasConfig config, Control control)
        {
            foreach (Control childControl in control.Controls)
            {
                ConfigToControl(config, childControl);
            }
            if (control is CheckBox)
            {
                ConfigToCheckbox(config, control as CheckBox);
            }
            else if (control is TextBox)
            {
                ConfigToText(config, control as TextBox);
            }
            else if (control is NumericUpDown)
            {
                ConfigToNum(config, control as NumericUpDown);
            }
        }

        private void ConfigToForm(GoogleVasConfig config)
        {
            switch (config.SmartTapVersion)
            {
                case 0x0000:
                    rbVersion20.Checked = true;
                    break;
                case 0x0001:
                    rbVersion21.Checked = true;
                    break;
                case 0x0002:
                    rbVersion22.Checked = true;
                    break;
                default:
                    Logger.Warning("Unsupported SmartTapVersion {0:X04}", config.SmartTapVersion);
                    rbVersion21.Checked = true;
                    break;
            }
            ConfigToControl(config, this);
        }

        private void ControlToConfig(GoogleVasConfig config, Control control)
        {
            foreach (Control childControl in control.Controls)
            {
                ControlToConfig(config, childControl);
            }
            if (control is CheckBox)
            {
                CheckboxToConfig(config, control as CheckBox);
            }
            else if (control is TextBox)
            {
                TextToConfig(config, control as TextBox);
            }
            else if (control is NumericUpDown)
            {
                NumToConfig(config, control as NumericUpDown);
            }
        }

        private void FormToConfig(GoogleVasConfig config)
        {
            if (rbVersion20.Checked) config.SmartTapVersion = 0x0000; else
            if (rbVersion21.Checked) config.SmartTapVersion = 0x0001; else
            if (rbVersion21.Checked) config.SmartTapVersion = 0x0002;

            ControlToConfig(config, this);
        }

        private void lkLoadFromFile_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            dlgOpenFile.InitialDirectory = AppUtils.BaseDirectory + Path.DirectorySeparatorChar + "conf";
            if (dlgOpenFile.ShowDialog() == DialogResult.OK)
            {
                GoogleVasConfig config = GoogleVasConfig.LoadFromJsonFile(dlgOpenFile.FileName);
                ConfigToForm(config);
            }
        }

        private void lkLoadDefault_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            GoogleVasConfig config = GoogleVasConfig.SpringCardDemo();
            ConfigToForm(config);
        }

        private void lkLoadGoogleDemo_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            GoogleVasConfig config = GoogleVasConfig.GoogleDemo();
            ConfigToForm(config);
        }

        private void lkClose_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            GoogleVasConfig config = new GoogleVasConfig();
            FormToConfig(config);
            RegistryCfgFile registry = RegistryCfgFile.OpenApplicationSectionReadWrite("TerminalSettings");
            config.Save(registry);
            Close();
        }

    }
}
