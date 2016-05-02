namespace ININ.Testing.Automation.Test.Client.TestCases.TS2047
{
    using System;
    using System.Collections.Generic;
    using ININ.ICWS.Configuration;
    using ININ.ICWS.Configuration.People;
    using ININ.Testing.Automation.Core;
    using ININ.Testing.Automation.Core.SeleniumAPI;
    using ININ.Testing.Automation.Core.Utilities;
    using ININ.Testing.Automation.Lib.Client;
    using ININ.Testing.Automation.Lib.ResourceManager;
    using ININ.Testing.Automation.ManagedICWS.Configuration.People;
    using ININ.Testing.Automation.Tcdb;
    using Xunit;

    /// <summary>
    ///     TC22030 - Persistent Remote Number Login
    /// </summary>
    public class TC22030 : ClientTestCase
    {
        #region Constructors and Destructors
        public TC22030()
        {
            this.TSNum = "2047";
            this.TCNum = "22030.3";
        }
        #endregion

        #region  Constants and Fields
        /// <summary>
        ///     The user that is making the call to _REMOTE_USER
        /// </summary>
        private const int _CALLING_USER = 0;

        /// <summary>
        ///     The user that is actually receiving the call from _CALLING_USER to _REMOTE_USER
        /// </summary>
        private const int _FORWARDED_USER = 1;

        /// <summary>
        ///     The user that is setup with the remote number
        /// </summary>
        private const int _REMOTE_USER = 2;

        /// <summary>
        ///     The interaction on the _FORWARDED_USER's queue
        /// </summary>
        private InteractionRow _forwardedUserInteraction;

        /// <summary>
        ///     The interaction on the _REMOTE_USER's queue
        /// </summary>
        private InteractionRow _remoteUserInteraction;

        /// <summary>
        ///     The test interaction (outbound) for steps 2-3
        /// </summary>
        private InteractionRow _testOutboundInteraction;
        #endregion

        #region Public Methods and Operators
        public override void Run()
        {
            using (Trace.TestCase.scope())
            {
                using (this.Rm = ResourceManagerRuntime.AllocateResources(3, 2))
                {
                    try
                    {
                        IList<StationLogonSettings> stationSettings = new List<StationLogonSettings>(3);
                        IList<string> userExtensions = new List<string>(3);

                        #region Pre Run Setup
                        using (Trace.TestCase.scope("Pre Run Setup"))
                        {
                            // Ensure all of the users have the default roles setup
                            SetUserDefaultRole(this.Rm.Users);

                            // Give the _REMOTE_USER their own client access license (since normally, these are on the station)
                            var userContract = Users.Get(new UsersResource.GetUserRequestParameters
                            {
                                Id = this.Rm.Users[_REMOTE_USER],
                                Select = "LicenseProperties",
                                ActualValues = "true"
                            });
                            userContract.LicenseProperties = new LicensePropertiesDataContract
                            {
                                HasClientAccess = true,
                                MediaLevel = MediaLevelDataContract.Media3,
                                MediaTypes =
                                    new List<MediaTypeDataContract>
                                    {
                                        MediaTypeDataContract.Call,
                                        MediaTypeDataContract.Chat
                                    },
                                LicenseActive = true
                            };
                            Users.Set(userContract);

                            // get a driver for the test.
                            this.Drivers = WebDriverManager.Instance.AddDriver(this.Rm.Users.Count);

                            // Setup the scenario users
                            stationSettings.Insert(_CALLING_USER, new StationLogonSettings(_STANDARD_STATION_TYPE, this.Rm.Stations[_CALLING_USER]));
                            stationSettings.Insert(_FORWARDED_USER, new StationLogonSettings(_STANDARD_STATION_TYPE, this.Rm.Stations[_FORWARDED_USER]));

                            // Get the extensions of the users in the scenario
                            foreach (var user in this.Rm.Users)
                            {
                                userExtensions.Add(Users.Get(
                                    new UsersResource.GetUserRequestParameters
                                    {
                                        Id = user,
                                        Select = "extension"
                                    }).Extension);
                            }

                            stationSettings.Insert(_REMOTE_USER, new StationLogonSettings(StationType.RemoteNumber, userExtensions[_FORWARDED_USER], true));
                        }
                        #endregion

                        #region STEP 1: Logon to the Basl Web Client, with a Persistent Remote Number.
                        using (Trace.TestCase.scope("Step 1: Logon to the Basl Web Client, with a Persistent Remote Number."))
                        {
                            //Step 1 Verify: Logon is successful.
                            Func<bool> logonResult = () => this.UsersLogonAndStatusSet(this.Rm.Users, stationSettings, this.Drivers);
                            this.TraceTrue(logonResult, "The users did not logon as expected.");
                        }
                        #endregion

                        #region STEP 2: Place a call using the Web Client to the remote number by dialing *.
                        using (Trace.TestCase.scope("Step 2: Place a call using the Web Client to the remote number by dialing *."))
                        {
                            //Step 2 Verify: The remote phone rings.

                            // Using the _REMOTE_USER, try an interaction to our FORWARD_USER.
                            WebDriverManager.Instance.SwitchBrowser(this.Drivers[_REMOTE_USER]);
                            ClientMain.InitiateCall("*");

                            // Check that the interaction shows up on _REMOTE_USER's queue
                            this._testOutboundInteraction = FilteredQueue.WaitForInteraction(
                                new Dictionary<string, string>
                                {
                                    {InteractionAttribute.State.AttributeId, InteractionState.SYSTEM}
                                });
                            this.TraceTrue(this._testOutboundInteraction != null, "Couldn't find the interaction on the calling user's queue");
                        }
                        #endregion

                        #region STEP 3: Pickup the connection call.
                        using (Trace.TestCase.scope("Step 3: Pickup the connection call."))
                        {
                            //Step 3 Verify: The call is connected.
                            WebDriverManager.Instance.SwitchBrowser(this.Drivers[_FORWARDED_USER]);

                            // Get the interaction from FORWARD_USER's queue
                            this._forwardedUserInteraction = FilteredQueue.WaitForInteraction(
                                new Dictionary<string, string>
                                {
                                    {InteractionAttribute.State.AttributeId, InteractionState.ALERTING}
                                });

                            // Pick up the call 
                            this._forwardedUserInteraction.ClickInteractionButton(InteractionButton.Pickup);

                            this.TraceTrue(this._forwardedUserInteraction.WaitForAttributes(new Dictionary<string, string>
                            {
                                {InteractionAttribute.State.AttributeId, InteractionState.CONNECTED}
                            }), "Interaction was not connected on the remote end");

                            // Make sure it's connected on the local end as well
                            WebDriverManager.Instance.SwitchBrowser(this.Drivers[_REMOTE_USER]);
                            this.TraceTrue(this._testOutboundInteraction != null && this._testOutboundInteraction.WaitForAttributes(new Dictionary<string, string>
                            {
                                {InteractionAttribute.State.AttributeId, InteractionState.CONNECTED}
                            }), "Interaction was not connected on the local end");
                        }
                        #endregion

                        #region STEP 4: Using the Web Client\'s Disconnect button, disconnect the call.
                        using (Trace.TestCase.scope("Step 4: Using the Web Client\'s Disconnect button, disconnect the call."))
                        {
                            //Step 4 Verify: The call is disconnected on User1\'s queue; the remote phone remains connected to the line.

                            // Disconnect on the local end
                            WebDriverManager.Instance.SwitchBrowser(this.Drivers[_REMOTE_USER]);
                            this._testOutboundInteraction.ClickInteractionButton(InteractionButton.Disconnect);

                            // Check that the interaction was disconnected on the local end
                            this.TraceTrue(() => this._testOutboundInteraction.WaitForAttributes(new Dictionary<string, string>
                            {
                                {InteractionAttribute.State.AttributeId, InteractionState.DISCONNECTED_LOCAL_DISCONNECT}
                            }), "Interaction was not disconnected on the local end");

                            // Check that the interaction is still connected on the remote end
                            WebDriverManager.Instance.SwitchBrowser(this.Drivers[_FORWARDED_USER]);

                            this.TraceTrue(() => this._forwardedUserInteraction.WaitForAttributes(new Dictionary<string, string>
                            {
                                {InteractionAttribute.State.AttributeId, InteractionState.CONNECTED}
                            }), "Remote user's interaction should still be connected.");
                        }
                        #endregion

                        #region STEP 5: Using another phone (not the remote phone), call User1\'s extension.
                        using (Trace.TestCase.scope("Step 5: Using another phone (not the remote phone), call User1\'s extension."))
                        {
                            //Step 5 Verify: The call appears in User1\'s queue.

                            // Use _CALLING_USER to call _REMOTE_USER
                            WebDriverManager.Instance.SwitchBrowser(this.Drivers[_CALLING_USER]);
                            ClientMain.InitiateCall(userExtensions[_REMOTE_USER]);

                            // Check if this shows up in _REMOTE_USER's queue
                            WebDriverManager.Instance.SwitchBrowser(this.Drivers[_REMOTE_USER]);
                            this._remoteUserInteraction = FilteredQueue.WaitForInteraction(
                                new Dictionary<string, string>
                                {
                                    {InteractionAttribute.State.AttributeId, InteractionState.ALERTING},
                                    {InteractionAttribute.Name.AttributeId, this.Rm.Users[_CALLING_USER]}
                                });
                            this.TraceTrue(this._remoteUserInteraction != null, "Couldn't get the interaction from the call target's queue");
                        }
                        #endregion

                        #region STEP 6: With the alerting call selected, click the Pickup button on the Web Client.
                        using (Trace.TestCase.scope("Step 6: With the alerting call selected, click the Pickup button on the Web Client."))
                        {
                            // Pick up the call 
                            this._remoteUserInteraction.ClickInteractionButton(InteractionButton.Pickup);

                            //NOTE-Call pick-up is verified in step 7.
                        }
                        #endregion

                        #region STEP 7: Pickup the connection call
                        using (Trace.TestCase.scope("Step 7: Pickup the connection call"))
                        {
                            //Step 7 Verify: The call is connected.

                            // Check that the call shows connected on the remote user's queue
                            this.TraceTrue(() => this._forwardedUserInteraction.WaitForAttributes(new Dictionary<string, string>
                            {
                                {InteractionAttribute.State.AttributeId, InteractionState.CONNECTED}
                            }), "Remote user's interaction should still be connected.");
                        }
                        #endregion

                        #region STEP 8: Disconnect the call by ending call from the remote phone.
                        using (Trace.TestCase.scope("Step 8: Disconnect the call by ending call from the remote phone."))
                        {
                            //Step 8 Verify: The call is disconnected as well as the telephone persistent connection (the remote phone does not remain connected to the line).

                            // Disconnect the call from FORWARD_USER's queue
                            WebDriverManager.Instance.SwitchBrowser(this.Drivers[_FORWARDED_USER]);

                            // Disconnect the interaction from FORWARD_USER's queue
                            this._forwardedUserInteraction.ClickInteractionButton(InteractionButton.Disconnect);

                            // Verify the interaction is disconnected on _FORWARDED_USER's queue
                            this.TraceTrue(
                                this._forwardedUserInteraction.WaitForAttributes(new Dictionary<string, string>
                                {
                                    {InteractionAttribute.State.AttributeId, InteractionState.DISCONNECTED_LOCAL_DISCONNECT}
                                }), "Interaction didn't disconnect on the receiving end's queue");

                            // Verify that the interaction is connected on _REMOTE_USER's queue
                            WebDriverManager.Instance.SwitchBrowser(this.Drivers[_REMOTE_USER]);

                            this.TraceTrue(
                                this._remoteUserInteraction.WaitForAttributes(new Dictionary<string, string>
                                {
                                    {InteractionAttribute.State.AttributeId, InteractionState.DISCONNECTED_LOCAL_HANG_UP}
                                }), "Interaction didn't disconnect on the call target end's queue");

                            // Verify the interaction is disconnected on the _CALLING_USER's queue
                            WebDriverManager.Instance.SwitchBrowser(this.Drivers[_CALLING_USER]);
                            this.TraceTrue(FilteredQueue.WaitForInteraction(
                                new Dictionary<string, string>
                                {
                                    {InteractionAttribute.State.AttributeId, InteractionState.DISCONNECTED_REMOTE_DISCONNECT},
                                    {InteractionAttribute.Name.AttributeId, this.Rm.Users[_REMOTE_USER]}
                                }) != null, "Interaction didn't disconnect on the calling end's queue");
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

                        this.Attributes.Add(TestCaseAttribute.WebBrowser_Desktop, WebDriverManager.Instance.GetBrowserVersion());
                        TCDBResults.SendResultsToXml(this.TCNum, this.Passed, this.SCRs, this.Stopwatch.Elapsed.TotalSeconds, this.Attributes);
                        TCDBResults.SubmitResult(this.TCNum, this.Passed, this.SCRs, attributes: this.Attributes);

                        #region Cleanup
                        using (Trace.TestCase.scope("Post Run Clean Up"))
                        {
                            this.ClearAllQueues();
                        }
                        #endregion
                    }
                }
            }
        }

        [ConnectFact]
        [Trait("TestSuite", "2047")]
        [Trait("Priority", "P3")]
        [Trait("BFT", "false")]
        public void Test22030_PersistentRemoteNumberLogon()
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