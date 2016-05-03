namespace ININ.Testing.Automation.Test.Client.TestCases.TS2047
{
    using System;
    using System.IO;
    using ININ.Testing.Automation.Core;
    using ININ.Testing.Automation.Core.Configuration;
    using ININ.Testing.Automation.Core.SeleniumAPI;
    using ININ.Testing.Automation.Core.Utilities;
    using ININ.Testing.Automation.Lib.Common;
    using ININ.Testing.Automation.Tcdb;
    using Xunit;

    /// <summary>
    ///     The tc TC26511.
    /// </summary>
    public class TC26511 : ClientTestCase
    {
        #region Constructors and Destructors
        /// <summary>
        ///     Initializes a new instance of the class.
        /// </summary>
        public TC26511()
        {
            this.TSNum = "2047";
            this.TCNum = "26511.1";
        }
        #endregion

        #region  Constants and Fields
        /// <summary>
        ///     Path to the config directory
        /// </summary>
        private string _configDirectory;

        /// <summary>
        ///     The uri of the servers.json file for the webserver.
        /// </summary>
        private string _serversJsonFileUri;

        /// <summary>
        ///     Name of webserver to add to servers.json
        /// </summary>
        private string _webServer;
        #endregion

        #region Public Methods and Operators
        /// <summary>
        ///     Runs the test case.
        /// </summary>
        public override void Run()
        {
            this.RunTest();
        }

        /// <summary>
        ///     Tests Logon dialog with no Servers.Json file
        /// </summary>
        [ConnectFact(Skip = "Shared media server prevents web server recycling.")]
        [Trait("TestSuite", "2047")]
        [Trait("Priority", "P2")]
        [Trait("BFT", "false")]
        public void Test26511_NoServersjsonFileExists()
        {
            try
            {
                this.Run();
            }
            catch (Exception e)
            {
                if (this.Passed)
                {
                    Trace.TestCase.exception(e, "Cleanup threw an exception. Make sure you are using ICWS APIs to do cleanup.");
                }
                else
                {
                    Trace.TestCase.exception(e, "The test case failed. Informing XUnit.");
                    throw;
                }
            }
        }
        #endregion

        #region Methods
        /// <summary>
        ///     Runs the actual test steps for the test case.
        /// </summary>
        private void RunTest()
        {
            try
            {
                using (Trace.TestCase.scope("Pre Run Step"))
                {
                    this._webServer = GlobalConfiguration.Instance.WebConfiguration.ServerName;

                    this._configDirectory = string.Format(@"\\{0}\BaslApps\client\config", this._webServer);

                    this._serversJsonFileUri = string.Format(@"{0}\servers.json", this._configDirectory);

                    Trace.TestCase.verbose("Expecting servers.json at {}", this._serversJsonFileUri);

                    if (!Directory.Exists(this._configDirectory))
                    {
                        Trace.TestCase.verbose("Directory {} doesn't exist; creating it.", this._configDirectory);
                        Directory.CreateDirectory(this._configDirectory);
                    }

                    //This test case assumes no servers.json file exists
                    if (File.Exists(this._serversJsonFileUri))
                    {
                        Trace.TestCase.verbose("{} already exists; removing.", this._serversJsonFileUri);
                        File.Delete(this._serversJsonFileUri);
                    }
                }

                #region STEP 1: Launch the Basl Client.
                using (Trace.TestCase.scope("Step 1: Launch the Basl Client."))
                {
                    //Step 1 Verify: The user is directed to the server selection page.
                    Logon.GoToLogon();
                    this.TraceTrue(Logon.Get().ChooseServerButton.WaitUntil(WaitUntilType.Exists), "Failed to be directed to the server selection page");
                }
                #endregion

                #region STEP 2: Observe that the server selection page, contains a text box and not a drop down.
                using (Trace.TestCase.scope("Step 2: Observe that the server selection page, contains a text box and not a drop down."))
                {
                    //Step 2 Verify: The server selection page, contains a text box and not a drop down.                    
                    this.TraceTrue(Logon.Get().ServerTextField.Exists, "Server selection should be a text box.");
                }
                #endregion

                this.Passed = true;
            }
            catch (KnownScrException ex)
            {
                Graphics.TakeScreenshot();
                var message = string.Format("Failed due to known SCR: {0}. SCR Description: {1}", ex.SCR, ex.Message);
                this.TraceTrue(false, message, ex.SCR);

                this.Passed = false;
                throw;
            }
            catch (Exception ex)
            {
                Graphics.TakeScreenshot();
                Trace.TestCase.exception(ex);

                this.Passed = false;
                throw;
            }
            finally
            {
                // Perform an HTML Dump into i3trace.
                Trace.TestCase.always("Html dump:\n{}", WebDriverManager.Instance.HtmlDump);

                this.Attributes.Add(TestCaseAttribute.WebBrowser_Desktop, WebDriverManager.Instance.GetBrowserVersion());
                TCDBResults.SendResultsToXml(this.TCNum, this.Passed, this.SCRs, this.Stopwatch.Elapsed.TotalSeconds, this.Attributes);
                TCDBResults.SubmitResult(this.TCNum, this.Passed, this.SCRs, attributes: this.Attributes);
            }
        }
        #endregion
    }
}