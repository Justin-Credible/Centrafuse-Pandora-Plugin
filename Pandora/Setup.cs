using System;
using System.Windows.Forms;
using System.Xml;
using System.Web;
using centrafuse.Plugins;
using PandoraSharp;

namespace Pandora
{
    public class Setup : CFSetup
    {

        #region Variables

        internal const string ENCRYPTION_PASSPHRASE = "ACC14F99-E12A-4FF4-B4B4-98766970287D";
        private const string PluginPath = @"plugins\Pandora\";
        private const string PluginPathLanguages = PluginPath + @"Languages\";
        private const string ConfigurationFile = "config.xml";
        private const string ConfigSection = "/APPCONFIG/";
        private const string LanguageSection = "/APPLANG/SETUP/";
        private const string LanguageControlSection = "/APPLANG/PANDORA/";

        #endregion

        #region Construction

        // The setup constructor will be called each time this plugin's setup is opened from the CF Setting Page
        // This setup is opened as a dialog from the CF_pluginShowSetup() call into the main plugin application form.
        public Setup(ICFMain mForm, ConfigReader config, LanguageReader lang)
        {
            // MainForm must be set before calling any Centrafuse API functions
            this.MainForm = mForm;

            // pluginConfig and pluginLang should be set before calling CF_initSetup() so this CFSetup instance 
            // will internally save any changed settings.
            this.pluginConfig = config;
            this.pluginLang = lang;

            // When CF_initSetup() is called, the CFPlugin layer will call back into CF_setupReadSettings() to read the page
            // Note that this.pluginConfig and this.pluginLang must be set before making this call
            CF_initSetup(1, 1);

            // Update the Settings page title
            this.CF_updateText("TITLE", this.pluginLang.ReadField("/APPLANG/SETUP/TITLE"));
        }

        #endregion

        #region CFSetup

        public override void CF_setupReadSettings(int currentpage, bool advanced)
        {
            try
            {
                ButtonHandler[CFSetupButton.One] = new CFSetupHandler(SetUserName);
                ButtonText[CFSetupButton.One] = this.pluginLang.ReadField("/APPLANG/SETUP/USERNAME");
                ButtonValue[CFSetupButton.One] = this.pluginConfig.ReadField("/APPCONFIG/USERNAME");

                ButtonHandler[CFSetupButton.Two] = new CFSetupHandler(SetPassword);
                ButtonText[CFSetupButton.Two] = this.pluginLang.ReadField("/APPLANG/SETUP/PASSWORD");
                string encryptedPassword = this.pluginConfig.ReadField("/APPCONFIG/PASSWORD");
                ButtonValue[CFSetupButton.Two] = String.IsNullOrEmpty(encryptedPassword) ? String.Empty : new String('•', 8);

                ButtonHandler[CFSetupButton.Three] = new CFSetupHandler(SetAudioFormat);
                ButtonText[CFSetupButton.Three] = this.pluginLang.ReadField("/APPLANG/SETUP/AUDIOFORMAT");
                AudioFormats audioFormat = AudioFormats.MP3;
                try
                {
                    audioFormat = (AudioFormats)Enum.Parse(typeof(AudioFormats), this.pluginConfig.ReadField("/APPCONFIG/AUDIOFORMAT"));
                }
                catch {}
                ButtonValue[CFSetupButton.Three] = GetAudioFormatDisplay(audioFormat);

                ButtonHandler[CFSetupButton.Four] = new CFSetupHandler(ClearFavorites);
                ButtonText[CFSetupButton.Four] = this.pluginLang.ReadField("/APPLANG/SETUP/CLEARFAVORITES");
                ButtonValue[CFSetupButton.Four] = this.pluginLang.ReadField("/APPLANG/SETUP/CLEARFAVORITESPROMPT");

                ButtonHandler[CFSetupButton.Five] = new CFSetupHandler(SetClearCache);
                ButtonText[CFSetupButton.Five] = this.pluginLang.ReadField("/APPLANG/SETUP/CLEARCACHE");
                ButtonValue[CFSetupButton.Five] = this.pluginConfig.ReadField("/APPCONFIG/CLEARCACHE");

                ButtonHandler[CFSetupButton.Six] = new CFSetupHandler(SetLogEvents);
                ButtonText[CFSetupButton.Six] = this.pluginLang.ReadField("/APPLANG/SETUP/LOGEVENTS");
                ButtonValue[CFSetupButton.Six] = this.pluginConfig.ReadField("/APPCONFIG/LOGEVENTS");
            }
            catch (Exception errmsg) { CFTools.writeError(errmsg.Message, errmsg.StackTrace); }
        }

        #endregion

        #region User Input Events

        private void SetUserName(ref object value)
        {
            try
            {
                object tempobject;
                string resultvalue, resulttext;

                // Display OSK for user to type display name
                DialogResult dialogResult = this.CF_systemDisplayDialog(CF_Dialogs.OSK, this.pluginLang.ReadField("/APPLANG/SETUP/USERNAME"), ButtonValue[(int)value], null, out resultvalue, out resulttext, out tempobject, null, true, true, true, true, false, false, 1);

                if (dialogResult == DialogResult.OK)
                {
                    // save user value, note this does not save to file yet, as this should only be done when user confirms settings
                    // being overwritten when they click the "Save" button.  Saving is done internally by the CFSetup instance if
                    // pluginConfig and pluginLang were properly set before callin CF_initSetup().
                    this.pluginConfig.WriteField("/APPCONFIG/USERNAME", resultvalue);

                    // Display new value on Settings Screen button
                    ButtonValue[(int)value] = resultvalue;
                }
            }
            catch (Exception errmsg) { CFTools.writeError(errmsg.Message, errmsg.StackTrace); }
        }

        private void SetPassword(ref object value)
        {
            try
            {
                object tempobject;
                string resultvalue, resulttext;

                // Display OSK for user to type display name
                DialogResult dialogResult = this.CF_systemDisplayDialog(CF_Dialogs.OSK, this.pluginLang.ReadField("/APPLANG/SETUP/PASSWORD"), String.Empty, null, out resultvalue, out resulttext, out tempobject, null, true, true, true, true, false, false, 1);

                if (dialogResult == DialogResult.OK)
                {
                    // save user value, note this does not save to file yet, as this should only be done when user confirms settings
                    // being overwritten when they click the "Save" button.  Saving is done internally by the CFSetup instance if
                    // pluginConfig and pluginLang were properly set before callin CF_initSetup().
                    this.pluginConfig.WriteField("/APPCONFIG/PASSWORD", EncryptionHelper.EncryptString(resultvalue, ENCRYPTION_PASSPHRASE));

                    // Display new value on Settings Screen button
                    ButtonValue[(int)value] = new String('•', 8);
                }
            }
            catch (Exception errmsg) { CFTools.writeError(errmsg.Message, errmsg.StackTrace); }
        }

        private void SetAudioFormat(ref object value)
        {
            try
            {
                object resultObject;
                string resultvalue, resulttext;

                CFControls.CFListViewItem[] audioFormatItems = new CFControls.CFListViewItem[3];
                audioFormatItems[0] = new CFControls.CFListViewItem(GetAudioFormatDisplay(AudioFormats.AAC_PLUS), AudioFormats.AAC_PLUS.ToString(), 0, false, (object)AudioFormats.AAC_PLUS);
                audioFormatItems[1] = new CFControls.CFListViewItem(GetAudioFormatDisplay(AudioFormats.MP3), AudioFormats.MP3.ToString(), 0, false, (object)AudioFormats.MP3);
                audioFormatItems[2] = new CFControls.CFListViewItem(GetAudioFormatDisplay(AudioFormats.MP3_HIFI), AudioFormats.MP3_HIFI.ToString(), 0, false, (object)AudioFormats.MP3_HIFI);

                // Display OSK for user to type display name
                DialogResult dialogResult = this.CF_systemDisplayDialog(CF_Dialogs.FileBrowser, this.pluginLang.ReadField("/APPLANG/SETUP/AUDIOFORMAT"), null, null, out resultvalue, out resulttext, out resultObject, audioFormatItems, false, true, false, false, false, false, 1);

                if (dialogResult == DialogResult.OK)
                {
                    AudioFormats audioFormat = (AudioFormats)resultObject;

                    // save user value, note this does not save to file yet, as this should only be done when user confirms settings
                    // being overwritten when they click the "Save" button.  Saving is done internally by the CFSetup instance if
                    // pluginConfig and pluginLang were properly set before callin CF_initSetup().
                    this.pluginConfig.WriteField("/APPCONFIG/AUDIOFORMAT", audioFormat.ToString());

                    // Display new value on Settings Screen button
                    ButtonValue[(int)value] = resulttext;

                    if (audioFormat == AudioFormats.MP3_HIFI)
                    {
                        CFDialogParams dialogParams = new CFDialogParams("High quality audio is only available to Pandora One subscribers. If you are not a subscriber regular quality will be used instead.");
                        this.CF_displayDialog(CF_Dialogs.OkBox, dialogParams);
                    }
                }
            }
            catch (Exception errmsg) { CFTools.writeError(errmsg.Message, errmsg.StackTrace); }
        }

        private void ClearFavorites(ref object value)
        {
            try
            {
                CFDialogParams dialogParams = new CFDialogParams();
                dialogParams.displaytext = "Are you sure you want to clear your favorite stations?";
                DialogResult dialogResult = CF_displayDialog(CF_Dialogs.YesNo, dialogParams);

                if (dialogResult == DialogResult.OK)
                    this.pluginConfig.WriteField("/APPCONFIG/FAVORITES", String.Empty);
            }
            catch (Exception errmsg) { CFTools.writeError(errmsg.Message, errmsg.StackTrace); }
        }

        private void SetClearCache(ref object value)
        {
            // save user value, note this does not save to file yet, as this should only be done when user confirms settings
            // being overwritten when they click the "Save" button.  Saving is done internally by the CFSetup instance if
            // pluginConfig and pluginLang were properly set before callin CF_initSetup().
            this.pluginConfig.WriteField("/APPCONFIG/CLEARCACHE", value.ToString());
        }

        private void SetLogEvents(ref object value)
        {
            // save user value, note this does not save to file yet, as this should only be done when user confirms settings
            // being overwritten when they click the "Save" button.  Saving is done internally by the CFSetup instance if
            // pluginConfig and pluginLang were properly set before callin CF_initSetup().
            this.pluginConfig.WriteField("/APPCONFIG/LOGEVENTS", value.ToString());
        }


        #endregion

        #region Helpers

        private string GetAudioFormatDisplay(AudioFormats audioFormat)
        {
            switch (audioFormat)
            {
                case AudioFormats.AAC_PLUS:
                    return this.pluginLang.ReadField("/APPLANG/SETUP/MOBILEQUALITY");
                case AudioFormats.MP3:
                    return this.pluginLang.ReadField("/APPLANG/SETUP/REGULARQUALITY");
                case AudioFormats.MP3_HIFI:
                    return this.pluginLang.ReadField("/APPLANG/SETUP/HIGHQUALITY");
                default:
                    return String.Empty;
            }
        }

        #endregion
    }
}
