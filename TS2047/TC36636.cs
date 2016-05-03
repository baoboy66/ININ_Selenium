namespace ININ.Testing.Automation.Test.Client.TestCases.TS2047
{
    using System;
    using ININ.ICWS.Configuration.System;
    using ININ.Testing.Automation.Core;
    using ININ.Testing.Automation.Core.SeleniumAPI;
    using ININ.Testing.Automation.Core.Utilities;
    using ININ.Testing.Automation.Lib.Common;
    using ININ.Testing.Automation.Lib.ResourceManager;
    using ININ.Testing.Automation.Tcdb;
    using Xunit;

    /// <summary>
    ///     TC36636 - Cancel logon at station selection
    /// </summary>
    public class TC36636 : ClientTestCase
    {
        #region Constructors and Destructors
        public TC36636()
        {
            this.TSNum = "2047";
            this.TCNum = "36636.3";
        }
        #endregion

        #region Public Methods and Operators
        public override void Run()
        {
            using (Trace.TestCase.scope())
            {
                using (this.Rm = ResourceManagerRuntime.AllocateResources(1))
                {
                    try
                    {
                        #region Pre Run Setup
                        using (Trace.TestCase.scope("Pre Run Setup"))
                        {
                            SetUserDefaultRole(this.Rm.Users);

                            this.Drivers = WebDriverManager.Instance.AddDriver(this.Rm.Users.Count);

                            SetLoginAuthentication(null);
                        }
                        #endregion

                        #region STEP 1: Enter the Interaction Center server name, user ID and proceed to the station selection page.
                        using (Trace.TestCase.scope("Step 1: Enter the Interaction Center server name, user ID and proceed to the station selection page."))
                        {
                            Logon.DoLogon(this.Rm.Users[0], this.UserPassword, this.IcServer, shouldSetStation: false);
                            //Step 1 Verify: Station selection page is displayed.
                            this.TraceTrue(ChangeStation.ChangeStationViewIsShown(), "The change station form was not shown.");
                        }
                        #endregion

                        #region STEP 2: Cancel the station selection.
                        using (Trace.TestCase.scope("Step 2: Cancel the station selection."))
                        {
                            ChangeStation.Get().CancelButton.Click();
                            //Step 2 Verify: User ID authentication page is displayed.
                            this.TraceTrue(Logon.IsAtIcAuthForm(), "The user was not put back at the IC auth form.");
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
                            SetLoginAuthentication(new LoginAuthenticationDataContract
                            {
                                DisableAlternateWindowsAuth = false,
                                DisableCachedCredentials = false,
                                DisableIcAuth = false,
                                DisableSingleSignOn = true,
                                DisableWindowsAuth = false
                            });
                        }
                        #endregion
                    }
                }
            }
        }

        [ConnectFact]
        [Trait("TestSuite", "2047")]
        [Trait("Priority", "P2")]
        [Trait("BFT", "hazard")]
        public void Test36636_CancelLogonAtStationSelection()
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