namespace ININ.Testing.Automation.Test.Client.TestCases.TS2047
{
    using System;
    using System.Collections.Generic;
    using ININ.ICWS.Test.Common;
    using ININ.Testing.Automation.Core;
    using ININ.Testing.Automation.Core.SeleniumAPI;
    using ININ.Testing.Automation.Core.Utilities;
    using ININ.Testing.Automation.Lib.Client.Queues.MyInteractions;
    using ININ.Testing.Automation.Lib.ResourceManager;
    using ININ.Testing.Automation.ManagedICWS;
    using ININ.Testing.Automation.ManagedICWS.Configuration.People;
    using ININ.Testing.Automation.ManagedICWS.Queues;
    using ININ.Testing.Automation.Tcdb;
    using Xunit;
    using Call = ININ.Testing.Automation.ManagedICWS.Interactions.Call;
    using Interaction = ININ.Testing.Automation.ManagedICWS.Interactions.Interaction;

    /// <summary>
    ///     TC22029 - Persistent Remote Number Login
    /// </summary>
    public class TC22029 : ClientTestCase
    {
        private const int _CALLING_USER = 0;
        private const int _FORWARDED_USER = 1;
        private const int _REMOTE_USER = 2;
        private readonly IList<Session> _callerSessions = new List<Session>(2);
        private MyInteractionsView.Interaction _interaction;
        private MyInteractionsView _myInteractionsView;

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
                        #region Pre Run Setup
                        using (Trace.TestCase.scope("Pre Run Setup"))
                        {
                            // Add interaction views for each user
                            TraceTrue(() =>
                            {
                                // ICWS call(s)
                                foreach (var user in Rm.Users)
                                {
                                    Users.SetRole(user, _DEFAULT_ROLE);
                                    Status.Set(user, "Available");
                                }

                                // Get ICWS session(s)
                                _callerSessions.Insert(_CALLING_USER, GetSession(Rm.Users[_CALLING_USER], Rm.Stations[_CALLING_USER]));
                                _callerSessions.Insert(_FORWARDED_USER, GetSession(Rm.Users[_FORWARDED_USER], Rm.Stations[_FORWARDED_USER]));

                                // Get driver(s)
                                Drivers = WebDriverManager.Instance.AddDriver(1);
                                return true;
                            }, "Pre run setup failed.");
                        }
                        #endregion

                        #region STEP 1: Log in using a Remote Number.
                        using (Trace.TestCase.scope("Step 1: Log in using a Remote Number."))
                        {
                            //Step 1 Verify: Logon is successful.
                            var remoteStation = new StationLogonSettings(StationType.RemoteNumber, GetUserExtension(Rm.Users[_FORWARDED_USER]));
                            TraceTrue(() => UserLogonAndStatusSet(Rm.Users[_REMOTE_USER], remoteStation, Drivers[0]), "Remote user failed to log on.");
                        }
                        #endregion

                        #region STEP 2: Place a call using the Web Client to the remote number by dialing *
                        using (Trace.TestCase.scope("Step 2: Place a call using the Web Client to the remote number by dialing *"))
                        {
                            TraceTrue(() =>
                            {
                                _myInteractionsView = new MyInteractionsView();
                                WaitFor(() => _myInteractionsView.Displayed);
                                Call.Create("*", Rm.Users[_REMOTE_USER]);
                                _interaction = GetInteraction(new Dictionary<string, string>
                                {
                                    {MyInteractionsView.InteractionAttribute.State.AttributeId, MyInteractionsView.InteractionState._SYSTEM}
                                });
                                return _interaction != null;
                            }, "Couldn't find the interaction on the calling user's queue");
                        }
                        #endregion

                        #region STEP 3: Pickup the connection call.
                        using (Trace.TestCase.scope("Step 3: Pickup the connection call."))
                        {
                            // Pick up call from remote user
                            TraceTrue(() =>
                            {
                                return WaitFor(() =>
                                {
                                    var interactions = UsersQueue.GetInteractionsForUser(Rm.Users[_FORWARDED_USER], new List<string> {InteractionAttributes.CallId});
                                    if (interactions != null && interactions.Count > 0)
                                    {
                                        Interaction.Pickup(interactions[0].InteractionId, Rm.Users[_FORWARDED_USER]);
                                        return interactions[0].IsConnected();
                                    }
                                    return false;
                                });
                            }, "Failed to connect call.");

                            // Make sure it's connected on the local end as well
                            TraceTrue(() =>
                            {
                                _interaction = GetInteraction(new Dictionary<string, string>
                                {
                                    {MyInteractionsView.InteractionAttribute.State.AttributeId, MyInteractionsView.InteractionState._CONNECTED}
                                });
                                return _interaction != null;
                            }, "Interaction was not connected on the local end");
                        }
                        #endregion

                        #region STEP 4: Using the Web Client\'s Disconnect button, disconnect the call.
                        using (Trace.TestCase.scope("Step 4: Using the Web Client\'s Disconnect button, disconnect the call."))
                        {
                            //Step 4 Verify: The call is disconnected on the remote phone without error; the Web Client shows the call as Disconnected.

                            // Disconnect on the local end
                            _myInteractionsView = new MyInteractionsView();
                            _interaction.Select();
                            _myInteractionsView.ClickInteractionButton(MyInteractionsView.InteractionButton.Disconnect);

                            // Check that the interaction was disconnected on the local end
                            TraceTrue(() => _interaction.Refresh().State == MyInteractionsView.InteractionState._DISCONNECTED_LOCAL_DISCONNECT, "Interaction was not disconnected on the local end.");
                        }
                        #endregion

                        #region STEP 5: Using another phone (not your remote phone), call TestUser by dialing the user\'s extension.
                        using (Trace.TestCase.scope("Step 5: Using another phone (not your remote phone), call TestUser by dialing the user\'s extension."))
                        {
                            //Step 5 Verify: The call appears in TestUser\'s queue.

                            // Use _CALLING_USER to call _REMOTE_USER
                            Call.Create(Users.GetExtension(Rm.Users[_REMOTE_USER]), Rm.Users[_CALLING_USER]);

                            // Check if this shows up in _REMOTE_USER's queue

                            TraceTrue(() =>
                            {
                                _interaction = GetInteraction(new Dictionary<string, string>
                                {
                                    {MyInteractionsView.InteractionAttribute.State.AttributeId, MyInteractionsView.InteractionState._ALERTING},
                                    {MyInteractionsView.InteractionAttribute.Name.AttributeId, Rm.Users[_CALLING_USER]}
                                });
                                return _interaction.State == MyInteractionsView.InteractionState._ALERTING;
                            }, "Couldn't get the interaction from the call target's queue");
                        }
                        #endregion

                        #region STEP 6: With the alerting call selected, click the Pickup button on the Web Client.
                        using (Trace.TestCase.scope("Step 6: With the alerting call selected, click the Pickup button on the Web Client."))
                        {
                            //Step 6 Verify: The call is connected. The remote phone rings.

                            // Pick up the call (but note that the interaction will still be alerting at this point, since the remote end hasn't picked up)
                            _interaction.Select();
                            _myInteractionsView.ClickInteractionButton(MyInteractionsView.InteractionButton.Pickup);

                            TraceTrue(() => { return WaitFor(() => _interaction.Refresh().State == MyInteractionsView.InteractionState._ALERTING); }, "Interaction was not connected on the reciever end");
                        }
                        #endregion

                        #region STEP 7: Pickup the connection call
                        using (Trace.TestCase.scope("Step 7: Pickup the connection call"))
                        {
                            //Step 7 Verify: The call is connected.
                            // Pick up the call 
                            //Todo: need a better way to check which interaction has status: alert
                            TraceTrue(() =>
                            {
                                return WaitFor(() =>
                                {
                                    var interactions = UsersQueue.GetInteractionsForUser(Rm.Users[_FORWARDED_USER], new List<string> {InteractionAttributes.CallId});
                                    if (interactions != null && interactions.Count > 0)
                                    {
                                        Interaction.Pickup(interactions[1].InteractionId, Rm.Users[_FORWARDED_USER]);
                                        return interactions[1].IsConnected();
                                    }
                                    return false;
                                });
                            }, "Failed to connect call.");

                            //Veryfy: The call is connected on remote phone
                            TraceTrue(_interaction.Refresh().State == MyInteractionsView.InteractionState._CONNECTED, "Interaction was not connected on the remote end");
                        }
                        #endregion

                        #region STEP 8: Disconnect the call by ending call from remote phone.
                        using (Trace.TestCase.scope("Step 8: Disconnect the call by ending call from remote phone."))
                        {
                            //Step 8 Verify: The call is disconnected for the remote phone; the Web Client shows the call as Disconnected.

                            // Disconnect the call from remote phone

                            _interaction.Select();
                            _myInteractionsView.ClickInteractionButton(MyInteractionsView.InteractionButton.Disconnect);
                            TraceTrue(() => _interaction.Refresh().State == MyInteractionsView.InteractionState._DISCONNECTED_LOCAL_DISCONNECT, "Interaction was not disconnected on the local end.");

                            // Verify the interaction is disconnected on _FORWARDED_USER's queue
                            TraceTrue(() =>
                            {
                                var interactions = UsersQueue.GetInteractionsForUser(Rm.Users[_FORWARDED_USER], new List<string> {InteractionAttributes.CallId});
                                if (interactions != null && interactions.Count > 0)
                                {
                                    foreach (var workItem in interactions)
                                    {
                                        if (workItem.IsConnected())
                                        {
                                            var item = workItem;
                                            WaitFor(() => !item.IsConnected());
                                        }
                                    }
                                    return true;
                                }
                                return false;
                            }, "Interaction was not disconnected.");
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
                            foreach (var session in _callerSessions)
                            {
                                session.Disconnect();
                            }
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