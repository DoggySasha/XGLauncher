﻿using MySql.Data.MySqlClient;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
//using XGL.API;
using XGL.Dialogs.Login;
using XGL.Networking.Database;
using XGL.SLS;

namespace XGL {

    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>

    public partial class App : Application {

        public static bool DevMode = true;
        public static bool OnlineMode = false;
        public static string CurrentVersion { get; private set; } = "0.1.3.1";
        public static string[] AccountData;
        public static string CurrentFolder { get; private set; }
        public static string AppDataFolder { get; private set; }
        public static string AppsFolder { get; private set; }
        public static Account CurrentAccount;
        public static bool RunMySQLCommands { get; set; } = true;
        public static string DBConnectorData { get; private set; }
        public static string VKConnectorData { get; private set; }
        public static string GoogleCS { get; private set; }
        public static string GoogleCID { get; private set; }
        public static bool IsFirstRun { get; set; } = false;

        protected override void OnStartup(StartupEventArgs e) {
            //Instantiate variables.
            InstanceVars();
            RegistrySLS.Setup();
            //Check online status and instantiate account.
            CheckStatus();
            //Read account data.
            if (AccountData.Length > 1 && AccountData[0] != "INS" && AccountData[0] != "False")
                CurrentAccount = new Account(AccountData[0], AccountData[1]);
            //Check for system language.
            if (RegistrySLS.LoadString("Language", "INS") == "INS")
                RegistrySLS.Save("Language", CultureInfo.CurrentCulture);
            //Check for XGLAPI status.
            /*if (RegistrySLS.LoadBool("UseXGLAPI", true))
                Core.Main(e.Args.ToArray());*/
            //Check folders and continue launching.
            FolderCheck();
            if (!OnlineMode) {
                if (!Database.TryOpenConnection()) {
                    LoginWindow l = new LoginWindow();
                    l.Show();
                    return;
                }
                //Check for updates.
                if (RegistrySLS.LoadBool("AutoUpdate", true))
                    CheckForUpdates();
                if (upRes) {
                    ProcessStartInfo pr = new ProcessStartInfo() {
                        FileName = $"{CurrentFolder}\\XGLauncher Updater.exe",
                        UseShellExecute = true,
                        Verb = "runas"
                    };
                    if (File.Exists(pr.FileName))
                        Process.Start(pr);
                    OnExit(null);
                    LoginWindow l = new LoginWindow();
                    l.Show();
                    l.Close();
                    return;
                }
                if (Database.AccountExisting(CurrentAccount))
                    NextWindow();
                else {
                    LoginWindow l = new LoginWindow();
                    l.Show();
                }
                return;
            } else {
                LoginWindow l = new LoginWindow();
                l.Show();
            }
            base.OnStartup(e);
        }

        bool upRes = false;
        void CheckForUpdates() {
            string output = string.Empty;
            MySqlCommand command = new MySqlCommand($"SELECT `latest` FROM `applications` WHERE `id` = 1", Database.Connection);
            try {
                Database.OpenConnection();
                MySqlDataReader dr;
                dr = command.ExecuteReader();
                while (dr.Read()) output = dr.GetString("latest");
                dr.Close();
                Database.CloseConnection();
            }
            catch (Exception ex) { Debug.WriteLine(ex.Message); }
            finally { Database.CloseConnection(); }
            if (output.Split('{')[0] != CurrentVersion)
                upRes = true;
        }

        void NextWindow() {
            if (Utils.NotEmptyAndNotNotSet(CurrentAccount.Password)) {
                //Save username and password to settings.
                RegistrySLS.Save("LastID", Database.GetID(CurrentAccount));
                Database.SetValue(Database.DBDataType.DT_ACTIVITY, 1);
            }
            //Opens the launcher.
            MainWindow m = new MainWindow();
            m.Show();
        }

        private void InstanceVars() {
            CurrentFolder = Environment.CurrentDirectory;
            AppDataFolder = Path.Combine(CurrentFolder, "ApplicationData");
            AppsFolder = Path.Combine(CurrentFolder, "apps");
            CurrentAccount = new Account("INS", "INS");
            AccountData = RegistrySLS.LoadString("LoginData", "INS;INS").Split(';');
            RegistrySLS.Save("Path", CurrentFolder);
            RegistrySLS.Save("Version", CurrentVersion);
            string[] appSData = INTERNAL.ApplicationSData.IndefData;
            DBConnectorData = appSData[0];
            GoogleCID = appSData[1];
            GoogleCS = appSData[2];
            VKConnectorData = appSData[3];
        }

        private void CheckStatus() => OnlineMode = Utils.NotEmptyAndNotNotSet(CurrentAccount.Login) && Utils.NotEmptyAndNotNotSet(CurrentAccount.Password);

        protected override void OnExit(ExitEventArgs e) {
            //Check online status.
            CheckStatus();
            //If online - change activity and lastOnline.
            if(!RunMySQLCommands || !OnlineMode) return;
            Database.SetValue(Database.DBDataType.DT_ACTIVITY, 0);
            Database.SetValue(Database.DBDataType.DT_LASTONLINE, DateTime.Now);
        }

        void FolderCheck() {
            //Create cache folder, if it doesn't exist.
            if (!Directory.Exists(Path.Combine(CurrentFolder, "cache"))) {
                DirectoryInfo di = Directory.CreateDirectory(Path.Combine(CurrentFolder, "cache"));
                di.Attributes = FileAttributes.Directory | FileAttributes.Hidden;
            }
            //Create logs folder, if it doesn't exist.
            if (!Directory.Exists(Path.Combine(CurrentFolder, "logs")))
                Directory.CreateDirectory(Path.Combine(CurrentFolder, "logs"));
            //Create apps folder, if it doesn't exist.
            if (!Directory.Exists(Path.Combine(CurrentFolder, "apps")))
                Directory.CreateDirectory(Path.Combine(CurrentFolder, "apps"));
            //Create localizations folder, if it doesn't exist.
            if (!Directory.Exists(Path.Combine(CurrentFolder, "localizations")))
                Directory.CreateDirectory(Path.Combine(CurrentFolder, "localizations"));
            //Load localization if there aren't any.
            /*if(!Directory.EnumerateFileSystemEntries(Path.Combine(CurrentFolder, "localizations")).Any()) 
                LocalizationManager.LoadLocalization(CultureInfo.CreateSpecificCulture(RegistrySLS.LoadString("Language", "en-US")));*/
        }

    }
}
