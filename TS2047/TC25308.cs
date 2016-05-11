namespace ININ.Testing.Automation.Test.Client.TestCases.TS2047
{
    using System;
    using System.Linq;
    using ININ.Testing.Automation.Core;
    using ININ.Testing.Automation.Core.SeleniumAPI;
    using ININ.Testing.Automation.Core.Utilities;
    using ININ.Testing.Automation.Lib.Common.LogonForm;
    using ININ.Testing.Automation.Lib.ResourceManager;
    using ININ.Testing.Automation.ManagedICWS;
    using ININ.Testing.Automation.ManagedICWS.Configuration.People;
    using ININ.Testing.Automation.Tcdb;
    using Xunit;

    public class TC25308 : ClientTestCase
    {
        private const string _POLISH = "polski";
        private LogonForm _logon;
        private const string _INVALID_ID = "fdjskaljfdklsjfdjskla";
        private const string _EXPECTED_ERROR_MESSAGE = "Proces uwierzytelniania nie powiódł się.";
        public TC25308()
        {
            TSNum = "2047";
            TCNum = "25308.6";
        }

        public override void Run()
        {
            using (Trace.TestCase.scope())
            {
                using (Rm = ResourceManagerRuntime.AllocateResources(1, 1))
                {
                    try
                    {
                        #region Pre Run Setup
                        using (Trace.TestCase.scope("Pre Run Setup"))
                        {
                            TraceTrue(() =>
                            {
                                // get driver
                                Drivers = WebDriverManager.Instance.AddDriver(Rm.Users.Count);

                                // set user role and status
                                Users.SetRole(Rm.Users[0], _DEFAULT_ROLE);
                                Status.Set(Rm.Users[0], "Available");
                                return true;
                            }, "Pre run setup failed.");
                        }
                        #endregion

                        #region STEP 1: On the login page, click the language menu.
                        using (Trace.TestCase.scope("Step 1: On the login page, click the language menu."))
                        {
                            //Step 1 Verify: A menu containing available language options is loaded.  All of the appropriate language options are shown in the menu.
                            //Comment: See Notes to determine what should be seen in the menu.
                            TraceTrue(() =>
                            {
                                _logon = new LogonForm();
                                _logon.GoTo();
                                var serverForm = new ServerForm();
                                if (WaitFor(() => serverForm.Displayed))
                                    serverForm.Set(IcServer).Submit();
                                return _logon.AvailableLanguages.Any(lang => string.Equals(lang, _POLISH, StringComparison.OrdinalIgnoreCase));
                            }, "Step 1 -Polish was not an option in the list of languages.");
                        }
                        #endregion

                        #region STEP 2: Inside the menu, choose an available language other than English and click on it.
                        using (Trace.TestCase.scope("Step 2: Inside the menu, choose an available language other than English and click on it."))
                        {
                            //Step 2 Verify: The login page reloads and the text on the page is now displayed in the language that was selected.
                            //Comment: We basically want to verify here that the page is not being displayed in English.
                            TraceTrue(() => WaitFor(() =>
                            {
                                _logon.GoTo();
                                _logon.SelectedLanguage = _POLISH;
                                _logon.GoTo();
                                var authForm = new AuthForm();
                                authForm.Set(_INVALID_ID, _INVALID_ID).LogOn();
                                WaitFor(() => authForm.Displayed);
                                return _logon.SelectedLanguage.Equals(_POLISH) && authForm.Error.Equals(_EXPECTED_ERROR_MESSAGE);
                            }), "Step 2 - Language was not changed.");
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
                    }
                }
            }
        }

        [ConnectFact]
        [Trait("TestSuite", "2047")]
        [Trait("Priority", "P2")]
        [Trait("BFT", "false")]
        [Trait("Patch", "true")]
        public void Test25308_ChangeLanguageOnTheLoginPage()
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