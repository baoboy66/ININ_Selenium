namespace ININ.Testing.Automation.Test.Client.TestCases.TS2047
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using ININ.Testing.Automation.Core;
    using ININ.Testing.Automation.Core.Configuration;
    using ININ.Testing.Automation.Core.SeleniumAPI;
    using ININ.Testing.Automation.Core.Utilities;
    using ININ.Testing.Automation.Lib.Common;
    using Xunit;

    /// <summary>
    ///     TC26513 - Ordering entries in the servers.json
    /// </summary>
    public class TC26513 : ClientTestCase
    {
        #region Constructors and Destructors
        public TC26513()
        {
            this.TSNum = "2047";
            this.TCNum = "26513.1";
        }
        #endregion

        #region  Constants and Fields
        /// <summary>
        ///     The first entry in the dropdown.
        /// </summary>
        private const string _CHOOSE_A_SERVER = "Choose a server...";

        /// <summary>
        ///     List of servers in dropdown
        /// </summary>
        private readonly List<string> _displayList = new List<string>();

        /// <summary>
        ///     Path to the config directory
        /// </summary>
        private string _configDirectory;

        /// <summary>
        ///     Placeholder for existing servers.json.  Used during cleanup to return to the original settings.
        /// </summary>
        private string _currentJsonFileUri;

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

                        this._displayList.Add(_CHOOSE_A_SERVER);
                        this._displayList.Add(this._webServer);
                        for (var i = 2; i <= 4; i++)
                        {
                            this._displayList.Add("displayName" + i);
                        }

                        this._configDirectory = string.Format(@"\\{0}\BaslApps\client\config", this._webServer);

                        if (!Directory.Exists(this._configDirectory))
                        {
                            Trace.TestCase.verbose("Directory {} doesn't exist; creating it.", this._configDirectory);
                            Directory.CreateDirectory(this._configDirectory);
                        }

                        this._serversJsonFileUri = string.Format(@"{0}\servers.json", this._configDirectory);

                        this._currentJsonFileUri = string.Format(@"{0}\oldServers.json", this._configDirectory);

                        Trace.TestCase.verbose("Expecting servers.json at {}; oldServers.json at {}", this._serversJsonFileUri, this._currentJsonFileUri);

                        //This test creates a new servers.json
                        if (File.Exists(this._serversJsonFileUri))
                        {
                            Trace.TestCase.verbose("Backing up old servers.json {} to {}", this._serversJsonFileUri, this._currentJsonFileUri);
                            if (File.Exists(this._currentJsonFileUri))
                            {
                                File.Delete(this._currentJsonFileUri);
                            }
                            File.Move(this._serversJsonFileUri, this._currentJsonFileUri);
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
                            var serversList = "{" + "\"version\": 1," + "\"servers\": [" + "{ \"hostName\": \"" + this.IcServer + "\", \"displayName\": \"" + this.IcServer + "\", \"order\": 0" + "}, {\"hostName\":\"hostname2\", \"displayName\": \"displayName2\", \"order\": 1}, {\"hostName\":\"hostname3\", \"displayName\": \"displayName3\", \"order\": 1}, {\"hostName\":\"hostname4\", \"displayName\": \"displayName4\", \"order\": 2}]}";
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

                    #region STEP 2: Create at least four entries in the servers.json file, giving one an order priorty of 0, two of them an order priroty of 1 and one an order priorty of 2.  See attached sample for reference.
                    using (Trace.TestCase.scope("Step 2: Create at least four entries in the servers.json file, giving one an order priorty of 0, two of them an order priroty of 1 and one an order priorty of 2.  See attached sample for reference."))
                    {
                        //Step 2 Verify: Four server entries are created in the servers.json file, with the correct order priority..
                        //Done in Step 1
                    }
                    #endregion

                    #region STEP 3: Launch the Basl Client.
                    using (Trace.TestCase.scope("Step 3: Launch the Basl Client."))
                    {
                        //Step 3 Verify: The user is taken to the server selection page.
                        this.Drivers = WebDriverManager.Instance.AddDriver(1);
                        Logon.GoToLogon();
                        this.TraceTrue(Logon.IsAtAuthTypeForm(), "Not redirected to the server page when trying to order the servers in the servers.json config file.");
                        this.TraceTrue(Logon.Get().ChooseServerButton.WaitUntil(WaitUntilType.Exists), "Failed to be directed to the server selection page");
                    }
                    #endregion

                    #region STEP 4: Click the drop down list and observe that the servers are displayed in the correct order.
                    using (Trace.TestCase.scope("Step 4: Click the drop down list and observe that the servers are displayed in the correct order."))
                    {
                        //Step 4 Verify: The servers are displayed in the correct order from priority 0 to priority 2.
                        var availableServerList = Logon.GetListOfServersFromDropDown();
                        Trace.TestCase.verbose("expected server list order: {}; actual server list order: {};",
                            string.Join(", ", this._displayList), string.Join(", ", availableServerList));
                        this.TraceTrue(availableServerList.SequenceEqual(this._displayList, StringComparer.OrdinalIgnoreCase), "List is not ordered correctly.");
                    }
                    #endregion

                    this.Passed = true;
                }
                catch (KnownScrException exception)
                {
                    Graphics.TakeScreenshot();
                    this.TraceTrue(
                        false,
                        "Failed due to known SCR: " + exception.SCR + ". SCR Description: " + exception.Message,
                        exception.SCR);
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
                        if (File.Exists(this._currentJsonFileUri))
                        {
                            // revert the servers.json file
                            if (File.Exists(this._serversJsonFileUri))
                            {
                                File.Delete(this._serversJsonFileUri);
                            }
                            File.Move(this._currentJsonFileUri, this._serversJsonFileUri);
                        }
                        else
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
                    }
                    #endregion
                }
            }
        }

        [ConnectFact(Skip = "No longer automatable since using shared web server.")]
        [Trait("TestSuite", "2047")]
        [Trait("Priority", "P2")]
        [Trait("BFT", "false")]
        public void Test26513_OrderingEntriesInTheServersjson()
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