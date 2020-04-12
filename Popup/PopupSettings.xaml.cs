﻿using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using PursuitLib;
using PursuitLib.Windows.WPF;
using PursuitLib.Extensions;
using System.Globalization;
using System.Collections.Generic;
using System.Threading;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json.Linq;
using PursuitLib.Windows.WPF.Dialogs;

namespace GTAVModdingLauncher.Popup
{
	/// <summary>
	/// The "Settings" popup
	/// </summary>
	public partial class PopupSettings : Window
	{
		private static Dictionary<string, string> supportedGtaLanguages = null;

		private delegate void Callback();
		private string OldLanguage;
		private Thread verifyUpdatesThread;

		public PopupSettings(Window parent)
		{
			this.OldLanguage = Launcher.Instance.Settings.Language;
			InitializeComponent();
			this.SetParent(parent);

			for(int i = 0; i < I18n.SupportedLanguages.Count; i++)
			{
				string language = I18n.SupportedLanguages[i];
				this.Languages.Items.Add(CultureInfo.GetCultureInfo(language).NativeName);
				if(this.OldLanguage == language)
					this.Languages.SelectedIndex = i;
			}

			string currentGtaLanguage = Launcher.Instance.Settings.GetGtaLanguage();
			int index = 0;
			foreach(string language in this.GetSupportedGtaLanguages().Keys)
				this.GtaLanguages.Items.Add(language);
			foreach(string language in supportedGtaLanguages.Values)
			{
				if(language == currentGtaLanguage)
				{
					this.GtaLanguages.SelectedIndex = index;
					break;
				}
				index++;
			}
			
			this.UseRph.IsChecked = Launcher.Instance.Settings.UseRph;
			this.Delete.IsChecked = Launcher.Instance.Settings.DeleteLogs;
			this.Offline.IsChecked = Launcher.Instance.Settings.OfflineMode;
			this.CheckUpdates.IsChecked = Launcher.Instance.Settings.CheckUpdates;
			this.UseLogFile.IsChecked = Launcher.Instance.Settings.UseLogFile;
			this.SelectedVersion.Text = Launcher.Instance.Installs.Selected?.Path;
		}

		private Dictionary<string,string> GetSupportedGtaLanguages()
		{
			if(supportedGtaLanguages != null)
				return supportedGtaLanguages;
			else
			{
				supportedGtaLanguages = new Dictionary<string, string>();
				supportedGtaLanguages.Add("English", "american");
				supportedGtaLanguages.Add("French", "french");
				supportedGtaLanguages.Add("Italian", "italian");
				supportedGtaLanguages.Add("German", "german");
				supportedGtaLanguages.Add("Spanish", "spanish");
				supportedGtaLanguages.Add("Japanese", "japanese");
				supportedGtaLanguages.Add("Russian", "russian");
				supportedGtaLanguages.Add("Polish", "polish");
				supportedGtaLanguages.Add("Portuguese", "portuguese");
				supportedGtaLanguages.Add("Traditional Chinese", "chinese");
				supportedGtaLanguages.Add("Latin American Spanish", "mexican");
				supportedGtaLanguages.Add("Korean", "korean");
				return supportedGtaLanguages;
			}
		}

		private void Save(object sender, EventArgs e)
		{
			Launcher.Instance.Settings.UseRph = (bool)this.UseRph.IsChecked;
			Launcher.Instance.Settings.DeleteLogs = (bool)this.Delete.IsChecked;
			Launcher.Instance.Settings.OfflineMode = (bool)this.Offline.IsChecked;
			Launcher.Instance.Settings.CheckUpdates = (bool)this.CheckUpdates.IsChecked;
			Launcher.Instance.Settings.UseLogFile = (bool)this.UseLogFile.IsChecked;
            Launcher.Instance.Settings.Language = I18n.SupportedLanguages[this.Languages.SelectedIndex];
			Launcher.Instance.Settings.GtaLanguage = this.GetSupportedGtaLanguages()[(string)this.GtaLanguages.SelectedItem];

			Launcher.Instance.SaveSettings();

			if(Log.HasLogFile && !Launcher.Instance.Settings.UseLogFile)
			{
				Log.Info("The user chose to disable logging.");
				Log.LogFile = null;
			}
			else if(!Log.HasLogFile && Launcher.Instance.Settings.UseLogFile)
			{
				Log.LogFile = Path.Combine(Launcher.Instance.UserDirectory, "latest.log");
				Log.Info("GTA V Modding Launcher " + Launcher.Instance.Version);
				Log.Info("Using PursuitLib " + typeof(Log).GetVersion());
				Log.Info("The user chose to enable logging.");
			}

			if(Launcher.Instance.Settings.Language != this.OldLanguage)
				I18n.LoadLanguage(Launcher.Instance.Settings.Language);

			this.Close();
		}

		private void Cancel(object sender, EventArgs e)
		{
			this.Close();
		}

		private void OnKeyDown(object sender, KeyEventArgs e)
		{
			if(e.Key == Key.Return)
				this.Save(null, null);
		}

		private void CheckForUpdates(object sender, RoutedEventArgs e)
		{
			if(this.verifyUpdatesThread == null || !this.verifyUpdatesThread.IsAlive)
			{
				this.verifyUpdatesThread = new Thread(VerifyUpdates);
				this.verifyUpdatesThread.Start();
			}
		}

		private void VerifyUpdates()
		{
			JObject obj = Launcher.Instance.IsUpToDate();

			if(obj != null)
				Launcher.Instance.ShowUpdatePopup(this, obj);
			else this.Dispatcher.Invoke(new Callback(ShowUpToDatePopup));
		}

		private void ShowUpToDatePopup()
		{
			LocalizedMessage.Show(this, "UpToDate", "Info", TaskDialogStandardIcon.Information, TaskDialogStandardButtons.Ok);
		}

		private void ManageInstalls(object sender, RoutedEventArgs e)
		{
			new PopupChooseInstall(this).ShowDialog();
			this.SelectedVersion.Text = Launcher.Instance.Installs.Selected?.Path;
		}
	}
}
