namespace ININ.Testing.Automation.Test.Client.TestCases.TS2047
{
    using System;
    using System.Collections.Generic;
    using System.Windows.Forms;
    using ININ.ICWS.Configuration;
    using ININ.ICWS.Configuration.People;
    using ININ.ICWS.Test.Common;
    using ININ.Testing.Automation.Core;
    using ININ.Testing.Automation.Core.SeleniumAPI;
    using ININ.Testing.Automation.Core.Utilities;
    using ININ.Testing.Automation.Lib.Client;
    using ININ.Testing.Automation.Lib.Client.Queues.MyInteractions;
    using ININ.Testing.Automation.Lib.ResourceManager;
    using ININ.Testing.Automation.ManagedICWS.Configuration.People;
    using ININ.Testing.Automation.Tcdb;
    using Xunit;
    using ININ.Testing.Automation.ManagedICWS.Interactions;

    /// <summary>
    ///     TC22029 - Persistent Remote Number Login
    /// </summary>
    public class TC22029 : ClientTestCase
    {
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
        ///     The test interaction (inbound)
        /// </summary>
        private InteractionRow _testInboundInteraction;

        /// <summary>
        ///     The test interaction (outbound) for steps 2-3
        /// </summary>
        private InteractionRow _testOutboundInteraction;

        private Session _callerSession;
        private IList<MyInteractionsView.Interaction> _interaction = new List<MyInteractionsView.Interaction>(3);
        private IList<MyInteractionsView> _myInteractionsView = new List<MyInteractionsView>(3);
        public TC22029()
        {
            TSNum = "2047";
            TCNum = "22029.5";
        }

        public override void Run()
        {
            using (Trace.TestCase.scope())
            {
                // 3 Users (User under test, caller, remote user), 2 Stations (User under test, caller)
                using (Rm = ResourceManagerRuntime.AllocateResources(3, 2))
                {
                    try
                    {
                        IList<StationLogonSettings> stationSettings = new List<StationLogonSettings>(3);
                        IList<string> userExtensions = new List<string>(3);

                        #region Pre Run Setup
                        using (Trace.TestCase.scope("Pre Run Setup"))
                        {

                            // Add interaction views for each user
                            _interaction.Insert(_CALLING_USER, null);
                            _interaction.Insert(_FORWARDED_USER, null);
                            _interaction.Insert(_REMOTE_USER, null);
                            _myInteractionsView.Insert(_CALLING_USER, new MyInteractionsView());
                            _myInteractionsView.Insert(_FORWARDED_USER, new MyInteractionsView());
                            _myInteractionsView.Insert(_REMOTE_USER, new MyInteractionsView());
                            // Ensure all of the users have the default roles setup
                            SetUserDefaultRole(Rm.Users);

                            // Give the _REMOTE_USER their own client access license (since normally, these are on the station)
                            var userContract = Users.Get(new UsersResource.GetUserRequestParameters
                            {
                                Id = Rm.Users[_REMOTE_USER],
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
                            Drivers = WebDriverManager.Instance.AddDriver(Rm.Users.Count);

                            // Setup the scenario users
                            stationSettings.Insert(_CALLING_USER, new StationLogonSettings(_STANDARD_STATION_TYPE, Rm.Stations[_CALLING_USER]));
                            stationSettings.Insert(_FORWARDED_USER, new StationLogonSettings(_STANDARD_STATION_TYPE, Rm.Stations[_FORWARDED_USER]));

                            // Get the extensions of the users in the scenario
                            foreach (var user in Rm.Users)
                            {
                                userExtensions.Add(Users.Get(
                                    new UsersResource.GetUserRequestParameters
                                    {
                                        Id = user,
                                        Select = "extension"
                                    }).Extension);
                            }

                            stationSettings.Insert(_REMOTE_USER, new StationLogonSettings(StationType.RemoteNumber, userExtensions[_FORWARDED_USER]));
                        }
                        #endregion

                        #region STEP 1: Log in using a Remote Number.
                        using (Trace.TestCase.scope("Step 1: Log in using a Remote Number."))
                        {
                            //Step 1 Verify: Logon is successful.
                            Func<bool> logonResult = () => UsersLogonAndStatusSet(Rm.Users, stationSettings, Drivers);

                            TraceTrue(logonResult, "The users did not logon as expected.");
                        }
                        #endregion

                        #region STEP 2: Place a call using the Web Client to the remote number by dialing *
                        using (Trace.TestCase.scope("Step 2: Place a call using the Web Client to the remote number by dialing *"))
                        {
                            //Step 2 Verify: The remote phone rings.
                            
                            // Using the _REMOTE_USER, try an interaction to our FORWARD_USER.
                            /*WebDriverManager.Instance.SwitchBrowser(Drivers[_REMOTE_USER]);
                            ClientMain.InitiateCall("*");

                            // Check that the interaction shows up on _REMOTE_USER's queue
                            _testOutboundInteraction = FilteredQueue.WaitForInteraction(
                                new Dictionary<string, string>
                                {
                                    {InteractionAttribute.State.AttributeId, InteractionState.SYSTEM}
                                });
                                */
                            //TraceTrue(_testOutboundInteraction != null, "Couldn't find the interaction on the calling user's queue");

                            TraceTrue(() =>
                            {
                                WebDriverManager.Instance.SwitchBrowser(Drivers[_REMOTE_USER]);
                                Call.Create("*", Rm.Users[_REMOTE_USER]);
                                //ClientMain.InitiateCall("*");                             
                                //_interaction[_REMOTE_USER] = null;
                                _interaction[_REMOTE_USER] = GetInteraction(new Dictionary<string, string>
                                {
                                    {MyInteractionsView.InteractionAttribute.State.AttributeId, MyInteractionsView.InteractionState._SYSTEM},
                                });
                                return _interaction[_REMOTE_USER] != null;
                               
                            }, "Couldn't find the interaction on the calling user's queue");
                            
                        }
                        #endregion

                        #region STEP 3: Pickup the connection call.
                        using (Trace.TestCase.scope("Step 3: Pickup the connection call."))
                        {
                            //Step 3 Verify: The call is connected.
                            WebDriverManager.Instance.SwitchBrowser(Drivers[_FORWARDED_USER]);
                            // Get the interaction from FORWARD_USER's queue
                            /*_testInboundInteraction = FilteredQueue.WaitForInteraction(s
                                new Dictionary<string, string>
                                {
                                    {InteractionAttribute.State.AttributeId, InteractionState.ALERTING}
                                });
                            */
                            _myInteractionsView[_FORWARDED_USER] = new MyInteractionsView();
                            TraceTrue(() =>
                            {
                                _interaction[_FORWARDED_USER] = GetInteraction(new Dictionary<string, string>
                                {
                                    {MyInteractionsView.InteractionAttribute.State.AttributeId, MyInteractionsView.InteractionState._ALERTING},
                                    //{MyInteractionsView.InteractionAttribute.Name.AttributeId, Rm.Users[_REMOTE_USER]}
                                });
                                return _interaction[_FORWARDED_USER] != null;
                            },"Couldn't find the interaction on the reciever's queue");

                            // Pick up the call 
                            // _testInboundInteraction.ClickInteractionButton(InteractionButton.Pickup);
                            _interaction[_FORWARDED_USER].Select();
                            _myInteractionsView[_FORWARDED_USER].ClickInteractionButton(MyInteractionsView.InteractionButton.Pickup);
                            /*TraceTrue(_testInboundInteraction.WaitForAttributes(new Dictionary<string, string>
                            {
                                {InteractionAttribute.State.AttributeId, InteractionState.CONNECTED}
                            }), "Interaction was not connected on the remote end");
                            */

                            TraceTrue(() =>
                            {
                                _interaction[_FORWARDED_USER] = GetInteraction(new Dictionary<string, string>
                                {
                                    {MyInteractionsView.InteractionAttribute.State.AttributeId, MyInteractionsView.InteractionState._CONNECTED},
                                });
                                return _interaction[_FORWARDED_USER] != null;
                            }, "Interaction was not connected on the remote end");

                            // Make sure it's connected on the local end as well
                            WebDriverManager.Instance.SwitchBrowser(Drivers[_REMOTE_USER]);
                            TraceTrue(() =>
                            {
                                _interaction[_REMOTE_USER] = GetInteraction(new Dictionary<string, string>
                                {
                                    {MyInteractionsView.InteractionAttribute.State.AttributeId, MyInteractionsView.InteractionState._CONNECTED},
                                });
                                return _interaction[_REMOTE_USER] != null;
                            }, "Interaction was not connected on the local end");
                        }
                        #endregion

                        #region STEP 4: Using the Web Client\'s Disconnect button, disconnect the call.
                        using (Trace.TestCase.scope("Step 4: Using the Web Client\'s Disconnect button, disconnect the call."))
                        {
                            //Step 4 Verify: The call is disconnected on the remote phone without error; the Web Client shows the call as Disconnected.

                            // Disconnect on the local end
                            WebDriverManager.Instance.SwitchBrowser(Drivers[_REMOTE_USER]);
                            //_testOutboundInteraction.ClickInteractionButton(InteractionButton.Disconnect);
                            _myInteractionsView[_REMOTE_USER] = new MyInteractionsView();
                            _interaction[_REMOTE_USER].Select();
                            _myInteractionsView[_REMOTE_USER].ClickInteractionButton(MyInteractionsView.InteractionButton.Disconnect);

                            // Check that the interaction was disconnected on the local end
                            /*TraceTrue(_testOutboundInteraction.WaitForAttributes(new Dictionary<string, string>
                            {
                                {InteractionAttribute.State.AttributeId, InteractionState.DISCONNECTED_LOCAL_DISCONNECT}
                            }), "Interaction was not disconnected on the local end");
                            */
                            TraceTrue(() =>
                            {
                                _interaction[_REMOTE_USER] = GetInteraction(new Dictionary<string, string>
                                {
                                    {MyInteractionsView.InteractionAttribute.State.AttributeId, MyInteractionsView.InteractionState._DISCONNECTED_LOCAL_DISCONNECT},
                                    //{MyInteractionsView.InteractionAttribute.Name.AttributeId, Rm.Users[_REMOTE_USER]}
                                });
                                return _interaction[_REMOTE_USER] != null;
                            }, "Interaction was not disconnected on the local end");

                            // Check that the interaction was disconnected on the remote end
                            WebDriverManager.Instance.SwitchBrowser(Drivers[_FORWARDED_USER]);
                            /*TraceTrue(_testInboundInteraction.WaitForAttributes(new Dictionary<string, string>
                            {
                                {InteractionAttribute.State.AttributeId, InteractionState.DISCONNECTED_REMOTE_DISCONNECT}
                            }), "Interaction was not disconnected on the remote end");
                            */
                            TraceTrue(() =>
                            {
                                _interaction[_FORWARDED_USER] = GetInteraction(new Dictionary<string, string>
                                {
                                    {MyInteractionsView.InteractionAttribute.State.AttributeId, MyInteractionsView.InteractionState._DISCONNECTED_REMOTE_DISCONNECT},
                                });
                                return _interaction[_FORWARDED_USER] != null;
                            }, "Interaction was not connected on the remote end");
                        }
                        #endregion

                        #region STEP 5: Using another phone (not your remote phone), call TestUser by dialing the user\'s extension.
                        using (Trace.TestCase.scope("Step 5: Using another phone (not your remote phone), call TestUser by dialing the user\'s extension."))
                        {
                            //Step 5 Verify: The call appears in TestUser\'s queue.

                            // Use _CALLING_USER to call _REMOTE_USER
                            WebDriverManager.Instance.SwitchBrowser(Drivers[_CALLING_USER]);
                            //ClientMain.InitiateCall(userExtensions[_REMOTE_USER]);
                            Call.Create(Users.GetExtension(Rm.Users[_REMOTE_USER]), Rm.Users[_CALLING_USER]);
                            // Check if this shows up in _REMOTE_USER's queue
                            WebDriverManager.Instance.SwitchBrowser(Drivers[_REMOTE_USER]);
                            /*_remoteUserInteraction = FilteredQueue.WaitForInteraction(
                                new Dictionary<string, string>
                                {
                                    {InteractionAttribute.State.AttributeId, InteractionState.ALERTING},
                                    {InteractionAttribute.Name.AttributeId, Rm.Users[_CALLING_USER]}
                                });
                            TraceTrue(_remoteUserInteraction != null, "Couldn't get the interaction from the call target's queue");
                            */
                            TraceTrue(() =>
                            {
                                _interaction[_REMOTE_USER] = GetInteraction(new Dictionary<string, string>
                                {
                                    {MyInteractionsView.InteractionAttribute.State.AttributeId, MyInteractionsView.InteractionState._ALERTING},
                                });
                                return _interaction[_REMOTE_USER] != null;
                            }, "Couldn't get the interaction from the call target's queue");
                        }
                        #endregion

                        #region STEP 6: With the alerting call selected, click the Pickup button on the Web Client.
                        using (Trace.TestCase.scope("Step 6: With the alerting call selected, click the Pickup button on the Web Client."))
                        {
                            //Step 6 Verify: The call is connected. The remote phone rings.


                            // Pick up the call (but note that the interaction will still be alerting at this point, since the remote end hasn't picked up)
                            //_remoteUserInteraction.ClickInteractionButton(InteractionButton.Pickup);
                            WebDriverManager.Instance.SwitchBrowser(Drivers[_REMOTE_USER]);
                            /*_myInteractionsView[_REMOTE_USER] = new MyInteractionsView();
                            TraceTrue(() =>
                            {
                                _interaction[_REMOTE_USER] = GetInteraction(new Dictionary<string, string>
                                {
                                    {MyInteractionsView.InteractionAttribute.State.AttributeId, MyInteractionsView.InteractionState._ALERTING},
                                });
                                return _interaction[_REMOTE_USER] != null;
                            }, "Interaction was not connected on the reciever end");
                            _myInteractionsView[_REMOTE_USER].ClickInteractionButton(MyInteractionsView.InteractionButton.Pickup);
                            */
                            _myInteractionsView[_REMOTE_USER] = new MyInteractionsView();
                            _interaction[_REMOTE_USER] = GetInteraction(new Dictionary<string, string>
                                {
                                    {MyInteractionsView.InteractionAttribute.State.AttributeId, MyInteractionsView.InteractionState._ALERTING},
                                });
                            _interaction[_REMOTE_USER].Select();
                            _myInteractionsView[_REMOTE_USER].ClickInteractionButton(MyInteractionsView.InteractionButton.Pickup);

                        }
                        #endregion

                        #region STEP 7: Pickup the connection call
                        using (Trace.TestCase.scope("Step 7: Pickup the connection call"))
                        {
                            //Step 7 Verify: The call is connected.

                            WebDriverManager.Instance.SwitchBrowser(Drivers[_FORWARDED_USER]);
                            _myInteractionsView[_FORWARDED_USER] = new MyInteractionsView();

                            // Get the interaction from FORWARD_USER's queue
                            /*_forwardedUserInteraction = FilteredQueue.WaitForInteraction(
                                new Dictionary<string, string>
                                {
                                    {InteractionAttribute.State.AttributeId, InteractionState.ALERTING}
                                });
                                */
                            TraceTrue(() =>
                            {
                                _interaction[_FORWARDED_USER] = GetInteraction(new Dictionary<string, string>
                                {
                                    {MyInteractionsView.InteractionAttribute.State.AttributeId, MyInteractionsView.InteractionState._ALERTING},
                                });
                                return _interaction[_FORWARDED_USER] != null;
                            }, "Couldn't find the interaction on the reciever's queue");

                            // Pick up the call 
                            /*_forwardedUserInteraction.ClickInteractionButton(InteractionButton.Pickup);

                            // Check that the call shows connected on the forwarded user's queues
                            TraceTrue(
                                _forwardedUserInteraction.WaitForAttributes(new Dictionary<string, string>
                                {
                                    {InteractionAttribute.State.AttributeId, InteractionState.CONNECTED}
                                }), "Couldn't find connected interaction after picking the interaction on the receiving end's queue");

                            // Check that the call shows connected on the remote user's queue
                            WebDriverManager.Instance.SwitchBrowser(Drivers[_REMOTE_USER]);
                            TraceTrue(
                                _remoteUserInteraction.WaitForAttributes(new Dictionary<string, string>
                                {
                                    {InteractionAttribute.State.AttributeId, InteractionState.CONNECTED}
                                }), "Couldn't find connected interaction after picking the interaction on the call target's queue");
                                */
                            _interaction[_FORWARDED_USER].Select();
                            _myInteractionsView[_FORWARDED_USER].ClickInteractionButton(MyInteractionsView.InteractionButton.Pickup);
                            /*TraceTrue(_testInboundInteraction.WaitForAttributes(new Dictionary<string, string>
                            {
                                {InteractionAttribute.State.AttributeId, InteractionState.CONNECTED}
                            }), "Interaction was not connected on the remote end");
                            */

                            TraceTrue(() =>
                            {
                                _interaction[_FORWARDED_USER] = GetInteraction(new Dictionary<string, string>
                                {
                                    {MyInteractionsView.InteractionAttribute.State.AttributeId, MyInteractionsView.InteractionState._CONNECTED},
                                    //{MyInteractionsView.InteractionAttribute.Name.AttributeId, Rm.Users[_FORWARDED_USER]}
                                });
                                return _interaction[_FORWARDED_USER] != null;
                            }, "Interaction was not connected on the remote end");

                            // Make sure it's connected on the local end as well
                            WebDriverManager.Instance.SwitchBrowser(Drivers[_REMOTE_USER]);
                            TraceTrue(() =>
                            {
                                _interaction[_REMOTE_USER] = GetInteraction(new Dictionary<string, string>
                                {
                                    {MyInteractionsView.InteractionAttribute.State.AttributeId, MyInteractionsView.InteractionState._CONNECTED},
                                    //{MyInteractionsView.InteractionAttribute.Name.AttributeId, Rm.Users[_REMOTE_USER]}
                                });
                                return _interaction[_REMOTE_USER] != null;
                            }, "Interaction was not connected on the local end");
                        }
                        #endregion

                        #region STEP 8: Disconnect the call by ending call from remote phone.
                        using (Trace.TestCase.scope("Step 8: Disconnect the call by ending call from remote phone."))
                        {
                            //Step 8 Verify: The call is disconnected for the remote phone; the Web Client shows the call as Disconnected.

                            // Disconnect the call from FORWARD_USER's queue
                            //WebDriverManager.Instance.SwitchBrowser(Drivers[_FORWARDED_USER]);

                            // Disconnect the interaction from FORWARD_USER's queue
                            //_forwardedUserInteraction.ClickInteractionButton(InteractionButton.Disconnect);

                            WebDriverManager.Instance.SwitchBrowser(Drivers[_FORWARDED_USER]);
                            _interaction[_FORWARDED_USER] = GetInteraction(new Dictionary<string, string>
                                {
                                    {MyInteractionsView.InteractionAttribute.State.AttributeId, MyInteractionsView.InteractionState._CONNECTED},
                                });
                            _interaction[_FORWARDED_USER].Select();
                            _myInteractionsView[_FORWARDED_USER].ClickInteractionButton(MyInteractionsView.InteractionButton.Disconnect);

                            // Verify the interaction is disconnected on _FORWARDED_USER's queue
                            /*TraceTrue(
                                _forwardedUserInteraction.WaitForAttributes(new Dictionary<string, string>
                                {
                                    {InteractionAttribute.State.AttributeId, InteractionState.DISCONNECTED_LOCAL_DISCONNECT}
                                }), "Interaction didn't disconnect on the receiving end's queue");
                                */

                            TraceTrue(() =>
                            {
                                _interaction[_FORWARDED_USER] = GetInteraction(new Dictionary<string, string>
                                {
                                    {MyInteractionsView.InteractionAttribute.State.AttributeId, MyInteractionsView.InteractionState._DISCONNECTED_LOCAL_DISCONNECT},
                                });
                                return _interaction[_FORWARDED_USER] != null;
                            }, "Interaction didn't disconnect on the receiving end's queue");


                            // Verify that the interaction is connected on _REMOTE_USER's queue
                            WebDriverManager.Instance.SwitchBrowser(Drivers[_REMOTE_USER]);

                            /*TraceTrue(
                                _remoteUserInteraction.WaitForAttributes(new Dictionary<string, string>
                                {
                                    {InteractionAttribute.State.AttributeId, InteractionState.DISCONNECTED_LOCAL_HANG_UP}
                                }), "Interaction didn't disconnect on the call target end's queue");
                                */

                            TraceTrue(() =>
                            {
                                _interaction[_REMOTE_USER] = GetInteraction(new Dictionary<string, string>
                                {
                                    {MyInteractionsView.InteractionAttribute.State.AttributeId, MyInteractionsView.InteractionState._DISCONNECTED_LOCAL_HANG_UP},
                                });
                                return _interaction[_REMOTE_USER] != null;
                            }, "Interaction didn't disconnect on the call target end's queue");

                            // Verify the interaction is disconnected on the _CALLING_USER's queue
                            WebDriverManager.Instance.SwitchBrowser(Drivers[_CALLING_USER]);
                            /*TraceTrue(FilteredQueue.WaitForInteraction(
                                new Dictionary<string, string>
                                {
                                    {InteractionAttribute.State.AttributeId, InteractionState.DISCONNECTED_REMOTE_DISCONNECT},
                                    {InteractionAttribute.Name.AttributeId, Rm.Users[_REMOTE_USER]}
                                }) != null, "Interaction didn't disconnect on the calling end's queue");
                            */
                            TraceTrue(() =>
                            {
                                _interaction[_CALLING_USER] = GetInteraction(new Dictionary<string, string>
                                {
                                    {MyInteractionsView.InteractionAttribute.State.AttributeId, MyInteractionsView.InteractionState._DISCONNECTED_REMOTE_DISCONNECT},
                                });
                                return _interaction[_CALLING_USER] != null;
                            }, "Interaction didn't disconnect on the calling end's queue");
                        }
                        #endregion

                        Passed = true;
                    }
                    catch (KnownScrException exception)
                    {
                        Graphics.TakeScreenshot();
                        TraceTrue(
                            false,
                            "Failed due to known SCR: " + exception.SCR + ". SCR Description: " + exception.Message,
                            exception.SCR);
                        Passed = false;
                        throw;
                    }
                    catch (Exception e)
                    {
                        Graphics.TakeScreenshot();
                        Trace.TestCase.exception(e);
                        Passed = false;
                        throw;
                    }
                    finally
                    {
                        // Perform an HTML Dump into i3trace.
                        Trace.TestCase.always("Html dump: \n{}", WebDriverManager.Instance.HtmlDump);

                        Attributes.Add(TestCaseAttribute.WebBrowser_Desktop, WebDriverManager.Instance.GetBrowserVersion());
                        TCDBResults.SendResultsToXml(TCNum, Passed, SCRs, Stopwatch.Elapsed.TotalSeconds, Attributes);
                        TCDBResults.SubmitResult(TCNum, Passed, SCRs, attributes: Attributes);

                        #region Cleanup
                        using (Trace.TestCase.scope("Post Run Clean Up"))
                        {
                            ClearAllQueues();
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
        public void Test22029_RemoteNumberLogon()
        {
            try
            {
                Run();
            }
            catch (Exception e)
            {
                if (Passed)
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
    }
}