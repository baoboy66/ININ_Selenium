namespace ININ.Testing.Automation.Test.Client.TestCases.TS2047
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using ININ.ICWS.Statistics;
    using ININ.Testing.Automation.Core;
    using ININ.Testing.Automation.Core.Configuration;
    using ININ.Testing.Automation.Core.SeleniumAPI;
    using ININ.Testing.Automation.Core.Utilities;
    using ININ.Testing.Automation.Lib.ResourceManager;
    using ININ.Testing.Automation.ManagedICWS;
    using ININ.Testing.Automation.ManagedICWS.Statistics;
    using ININ.Testing.Automation.Tcdb;
    using Xunit;
    using StaleElementReferenceException = OpenQA.Selenium.StaleElementReferenceException;

    /// <summary>
    ///     TC 25772 - Connect to an OSSM
    /// </summary>
    public class TC25772 : ClientTestCase
    {
        #region Constructors and Destructors
        /// <summary>
        ///     Initializes a new instance of the <see cref="TC25772" /> class.
        /// </summary>
        public TC25772()
        {
            this.TSNum = "2047";
            this.TCNum = "25772.1";
        }
        #endregion

        #region  Constants and Fields
        /// <summary>
        ///     The number of logged in users during the most recent statistics value update.
        /// </summary>
        private int _loggedInCurrent;

        /// <summary>
        ///     The number of logged in users during the previous statistics value update.
        /// </summary>
        private int _loggedInLast;

        /// <summary>
        ///     The statistics value to use for the product name in the session count subscription.
        /// </summary>
        private string _smProductNameStatsValue;

        /// <summary>
        ///     The statistics value to use for the server name in the session count subscription.
        /// </summary>
        private string _smServerStatsValue;

        /// <summary>
        ///     The statistics value to use for the session manager name in the session count subscription.
        /// </summary>
        private string _smStatisticValue;

        /// <summary>
        ///     Allows the tests to wait until the next statistics message is received.
        /// </summary>
        private AutoResetEvent _statisticValueMessageEvent;
        #endregion

        #region Public Methods and Operators
        public override void Run()
        {
            using (Trace.TestCase.scope())
            {
                using (this.Rm = ResourceManagerRuntime.AllocateResources(1, 1))
                {
                    this._statisticValueMessageEvent = new AutoResetEvent(false);

                    try
                    {
                        using (Trace.TestCase.scope("Pre Run Setup"))
                        {
                            SetUserDefaultRole(this.Rm.Users);

                            if (string.Equals(Core.ConnectedServer, GlobalConfiguration.Instance.IcServerConfiguration.ServerName))
                            {
                                throw new FailedTestCaseException("The test case must be run while connected to an OSSM.");
                            }

                            this._loggedInCurrent = -1;
                            this._loggedInLast = -1;

                            this.PerformStatisticValueQueries();
                            this.SubscribeToSessionCount();

                            this._statisticValueMessageEvent.WaitOne(GlobalConfiguration.Instance.WebConfiguration.SearchTimeout);

                            this.Drivers = WebDriverManager.Instance.AddDriver(1);
                        }

                        #region STEP 1: Log the user in to the Basl Web Client.
                        using (Trace.TestCase.scope("Step 1: Log the user in to the Basl Web Client."))
                        {
                            // Step 1 Verify: The user is logged into the Basl Web Client.
                            this.TraceTrue(this.UserLogonAndStatusSet(this.Rm.Users[0], this.Rm.Stations[0], this.Drivers[0]),
                                "The user didn't log in as expected.");
                        }
                        #endregion

                        #region STEP 2: Verify that the user is logged into the OSSM and not the Session Manager local to the IC server.
                        using (Trace.TestCase.scope("Step 2: Verify that the user is logged into the OSSM and not the Session Manager local to the IC server."))
                        {
                            // Step 2 Verify: The user is logged into the OSSM.
                            // Comment: One way to check this is view the Session Mangers view in ICBM.
                            var wait = new WebDriverBaseWait();
                            wait.IgnoreExceptionTypes(typeof (StaleElementReferenceException));
                            wait.Timeout = TimeSpan.FromMilliseconds(GlobalConfiguration.Instance.WebConfiguration.SearchTimeout*2);

                            this.TraceTrue(
                                () =>
                                {
                                    Trace.TestCase.verbose("_loggedInLast: {}; _loggedInCurrent: {};", this._loggedInLast, this._loggedInCurrent);
                                    return wait.Until(_ => this._loggedInLast >= 0 && this._loggedInCurrent > this._loggedInLast);
                                },
                                "Could not verify that the user logged in via the statistics api.");
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
                        using (Trace.TestCase.scope("Post Run Clean Up"))
                        {
                            StatisticValues.DeleteSubscription();
                        }
                    }
                }
            }
        }

        [ConnectFact(Skip = "Requires OSSM which is available at systest: unskip once there.")]
        [Trait("TestSuite", "2047")]
        [Trait("Priority", "P2")]
        [Trait("BFT", "false")]
        public void Test25772_ConnectToAnOSSM()
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
        ///     Queries the statistics API for the session count products list.
        /// </summary>
        /// <exception cref="FailedTestCaseException">The value couldn't be received properly.</exception>
        private void PerformProductsQuery()
        {
            var contract = StatisticParameterValues.Queries.Get(new ParameterValueQueryDataContract
            {
                ParameterTypeId = "ININ.SessionManager:Server"
            });

            var value = contract.ParameterValues.FirstOrDefault();
            if (value == null)
            {
                throw new FailedTestCaseException("Couldn't find the server value in the list of session manager statistic values.");
            }

            this._smServerStatsValue = value.Value;
        }

        /// <summary>
        ///     Queries the statistics API for the session count server list.
        /// </summary>
        /// <exception cref="FailedTestCaseException">The value couldn't be received properly.</exception>
        private void PerformServerQuery()
        {
            var contract = StatisticParameterValues.Queries.Get(new ParameterValueQueryDataContract
            {
                ParameterTypeId = "ININ.SessionManager:SessionManager"
            });

            var connectedServer = Core.ConnectedServer.ToLower();
            var values = contract.ParameterValues.Where(val =>
            {
                var display = val.DisplayString.ToLower();
                var regex = new Regex(@"([^\[]*)");

                var match = regex.Match(display);
                if (match.Groups.Count == 0 || match.Groups[0].Captures.Count == 0)
                {
                    return false;
                }

                var statServerName = match.Groups[0].Captures[0].Value.Trim();

                return connectedServer.Contains(statServerName);
            });

            if (!values.Any())
            {
                throw new FailedTestCaseException("Couldn't find the connected server in the list of session manager statistic values.");
            }

            this._smStatisticValue = values.First().Value;
        }

        /// <summary>
        ///     Queries the statistics API for the session count session managers list.
        /// </summary>
        /// <exception cref="FailedTestCaseException">The value couldn't be received properly.</exception>
        private void PerformSessionManagerQuery()
        {
            var contract = StatisticParameterValues.Queries.Get(new ParameterValueQueryDataContract
            {
                ParameterTypeId = "ININ.SessionManager:ProductId"
            });

            var value = contract.ParameterValues.FirstOrDefault(data => string.Equals(data.Value, "Interaction Client, Web Edition"));

            if (value == null)
            {
                throw new FailedTestCaseException("Couldn't find the Web Client's product in the list of session manager statistic values.");
            }

            this._smProductNameStatsValue = value.Value;
        }

        /// <summary>
        ///     Runs all of the statistic value queries in parallel and waits for their completion before returning.
        /// </summary>
        private void PerformStatisticValueQueries()
        {
            Task.WaitAll(Task.Factory.StartNew(this.PerformServerQuery), Task.Factory.StartNew(this.PerformSessionManagerQuery), Task.Factory.StartNew(this.PerformProductsQuery));
        }

        /// <summary>
        ///     The stat value handler for the session count subscription.  Keeps track of the two most
        ///     recent active session counts so that the test can look for the increase in users.
        /// </summary>
        /// <param name="dataContract">The data received from the server.</param>
        private void StatValueHandler(StatisticValueMessageDataContract dataContract)
        {
            if (dataContract.StatisticValueChanges.Count == 0)
            {
                return;
            }

            foreach (var data in dataContract.StatisticValueChanges)
            {
                if (!string.Equals(data.StatisticKey.StatisticIdentifier, "inin.sessionmanager:SessionCount"))
                {
                    continue;
                }

                var intData = data.StatisticValue as StatisticIntValueDataContract;
                if (intData == null)
                {
                    continue;
                }

                this._loggedInLast = this._loggedInCurrent;
                this._loggedInCurrent = intData.Value;
            }

            this._statisticValueMessageEvent.Set();
        }

        /// <summary>
        ///     The subscribe to session count statistic for the SM that we plan on connecting to.
        /// </summary>
        private void SubscribeToSessionCount()
        {
            var parameterValueItems = new List<ParameterValueItemDataContract>
            {
                new ParameterValueItemDataContract
                {
                    ParameterTypeId = "ININ.SessionManager:Server",
                    Value = this._smServerStatsValue
                },
                new ParameterValueItemDataContract
                {
                    ParameterTypeId = "ININ.SessionManager:SessionManager",
                    Value = this._smStatisticValue
                },
                new ParameterValueItemDataContract
                {
                    ParameterTypeId = "ININ.SessionManager:ProductId",
                    Value = this._smProductNameStatsValue
                }
            };

            var dataContract = new StatisticValueSubscriptionDataContract
            {
                StatisticKeys = new List<StatisticKeyDataContract>
                {
                    new StatisticKeyDataContract
                    {
                        StatisticIdentifier = "inin.sessionmanager:SessionCount",
                        ParameterValueItems = parameterValueItems
                    }
                }
            };

            Core.Session.MessageListener.StartListening<StatisticValueMessageDataContract>(this.StatValueHandler);
            StatisticValues.SetSubscription(dataContract);
        }
        #endregion
    }
}