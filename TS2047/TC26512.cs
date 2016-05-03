namespace ININ.Testing.Automation.Test.Client.TestCases.TS2047
{
    using System;
    using System.IO;
    using ININ.Testing.Automation.Core;
    using ININ.Testing.Automation.Core.Configuration;
    using ININ.Testing.Automation.Core.SeleniumAPI;
    using ININ.Testing.Automation.Core.Utilities;
    using ININ.Testing.Automation.Lib.Common;
    using Xunit;

    /// <summary>
    ///     TC26512 - Single server entry in servers.json
    /// </summary>
    public class TC26512 : ClientTestCase
    {
        #region Constructors and Destructors
        public TC26512()
        {
            this.TSNum = "2047";
            this.TCNum = "26512.1";
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
        public override void Run()
        {
            using (Trace.TestCase.scope())
            {
                try
                {
                    #region Pre Run Setup
                    using (Trace.TestCase.scope("Pre Run Setup"))
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

                        //This test creates a new server.json
                        if (File.Exists(this._serversJsonFileUri))
                        {
                            Trace.TestCase.verbose("{} already exists; removing.", this._serversJsonFileUri);
                            File.Delete(this._serversJsonFileUri);
                        }
                    }
                    #endregion

                    #region STEP 1: Create a servers.json file in the Config directory on the Web Server.
                    using (Trace.TestCase.scope("Step 1: Create a servers.json file in the Config directory on the Web Server."))
                    {
                        //Step 1 Verify: The servers.json file is created
                        //Comment: The config directory does not exist by default, so you may have to create it in the route of the application directory.
                        try
                        {
                            var serversList = "{" + "\"version\": 1," + "\"servers\": [" + "{ \"hostName\": \"" + this.IcServer + "\", \"displayName\": \"" + this.IcServer + "\", \"order\": 0" + "}]}";
                            var writer = new StreamWriter(this._serversJsonFileUri);
                            writer.Write(serversList);
                            writer.Close();
                        }
                        catch (Exception)
                        {
                            Trace.TestCase.error("Failed to modify server.json");
                            throw;
                        }
                        this.TraceTrue(File.Exists(this._serversJsonFileUri), "Failed to create servers.json file");
                    }
                    #endregion

                    #region STEP 2: Create only one server entry in the servers.json file.  See attached sample for reference.
                    using (Trace.TestCase.scope("Step 2: Create only one server entry in the servers.json file.  See attached sample for reference."))
                    {
                        //Step 2 Verify: One server entry is created in the servers.json file.
                        //Done in Step 1
                    }
                    #endregion

                    #region STEP 3: Launch the Basl Client and observe that the user is taken directly to the Logon page.
                    using (Trace.TestCase.scope("Step 3: Launch the Basl Client and observe that the user is taken directly to the Logon page."))
                    {
                        //Step 3 Verify: The user is taken directly to the Logon page and the IC server name is not displayed.
                        this.Drivers = WebDriverManager.Instance.AddDriver(1);
                        Logon.GoToLogon();
                        this.TraceTrue(Logon.IsAtIcAuthForm(), "Failed to navigate directly to the IC auth logon form.");
                    }
                    #endregion

                    this.Passed = true;
                }
                catch (KnownScrException exception)
                {
                    Graphics.TakeScreenshot();
                    this.TraceTrue(false, "Failed due to known SCR: " + exception.SCR + ". SCR Description: " + exception.Message, exception.SCR);
                    this.Passed = false;
                    throw;
                }
                catch (Exception e)
                {
                    Graphics.TakeScreenshot();
                    Trace.TestCase.exception(e);
                    this.Passed = false;
                    throw;
                }
                finally
                {
                    // Perform an HTML Dump into i3trace.
                    Trace.TestCase.always("Html dump: \n{}", WebDriverManager.Instance.HtmlDump);

                    TCDBResults.SendResultsToXml(this.TCNum, this.Passed, this.SCRs, this.Stopwatch.Elapsed.TotalSeconds, this.Attributes);
                    TCDBResults.SubmitResult(this.TCNum, this.Passed, this.SCRs, attributes: this.Attributes);

                    #region Cleanup
                    using (Trace.TestCase.scope("Post Run Clean Up"))
                    {
                        try
                        {
                            File.Delete(this._serversJsonFileUri);
                        }
                        catch (Exception e)
                        {
                            Trace.TestCase.exception(e, "Couldn't delete {}", this._serversJsonFileUri ?? "null");
                        }
                    }
                    #endregion
                }
            }
        }

        [ConnectFact(Skip = "No longer automatable since using shared web server.")]
        [Trait("TestSuite", "2047")]
        [Trait("Priority", "P2")]
        [Trait("BFT", "false")]
        public void Test26512_SingleServerEntryInServersjson()
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
    }
}