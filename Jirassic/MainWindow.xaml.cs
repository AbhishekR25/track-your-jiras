using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Configuration;
using System.Xml;
using System.IO;

namespace Jirassic
{
    public class JiraIssues
    {
        public string issueKey { get; set; }
        public string issueType { get; set; }
        public string status { get; set; }
        public string summary { get; set; }
        public string toolTip { get; set; }
        public string jiraAddress { get; set; }
        public DateTime createdDate { get; set; }
        public Boolean addedToTable { get; set; }
    }

    public class CrucibleReviews
    {
        public string reviewKey { get; set; }
        public string description { get; set; }
        public string author { get; set; }
        public string crToolTip { get; set; }
    }

    public class JiraStatus
    {
        public const string NONE = "(None)";

        public const string IN_PROGRESS = "In Progress";
        public const string OPEN = "Open";
        public const string REOPENED = "Reopened";
        public const string WAITING_FOR_REPORTER_INPUT = "Waiting For Reporter Input";
        public const string SUBMITTED = "Submitted";
        public const string INCEP_SPEC = "Incep Spec";
        public const string IN_REVIEW = "In Review";
        public const string VERIFIED = "Verified";
        public const string DEV_REVIEW_DONE = "Dev Review Done";
        public const string VALIDATING_ISSUE = "Validating Issue";

        public const string ON_HOLD = "On Hold";
        public const string READY_FOR_GA = "Ready For GA";
        public const string ISSUE_DONE = "Issue Done";
        public const string BLOCKED = "Blocked";
        public const string RESOLVED = "Resolved";
        public const string CLOSED = "Closed";
        public const string DONE = "Done";
        public const string FAILED_IN_TESTING = "Failed in Testing";
    }

    public partial class MainWindow : Window
    {
        DateTime thresholdDate = new DateTime();

        string associateName = string.Empty;
        string statusBar = string.Empty;        

        List<JiraIssues> assigneeList = new List<JiraIssues>();
        List<JiraIssues> reporterList = new List<JiraIssues>();

        List<JiraIssues> sortedAssigneeList = new List<JiraIssues>();
        List<JiraIssues> sortedReporterList = new List<JiraIssues>();

        DataTable assigneeTable = new DataTable("assigneeTable");
        DataTable reporterTable = new DataTable("reporterTable");

        DataView assigneeDataView;
        DataView reporterDataView;

        List<string> jira1StatusList = new List<string>();
        List<string> jira2StatusList = new List<string>();
        List<string> jira3StatusList = new List<string>();

        List<CrucibleReviews> inboxList = new List<CrucibleReviews>();
        List<CrucibleReviews> outboxList = new List<CrucibleReviews>();

        DataTable inboxTable = new DataTable("inboxTable");
        DataTable outboxTable = new DataTable("outboxTable");

        DataView inboxView;
        DataView outboxView;

        bool statusComboBoxSet = false;
        bool jiraInstanceComboBoxSet = false;
        bool activityComboBoxSet = false;

        string ID = string.Empty;

        public MainWindow()
        {
            try
            {
                CultureInfo ci = CultureInfo.CurrentCulture;
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                config.AppSettings.Settings["Culture"].Value = ci.IetfLanguageTag;
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");

                Properties.Resources.Culture = new CultureInfo(ConfigurationManager.AppSettings["Culture"]);

                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
                InitializeComponent();

                this.StateChanged += new EventHandler(Window1_StateChanged);

                assigneeIssueCount.Text = string.Empty;
                reporterIssueCount.Text = string.Empty;

                inboxGrid.CanUserAddRows = false;
                inboxGrid.CanUserDeleteRows = false;
                inboxGrid.AutoGenerateColumns = false;
                inboxGrid.CancelEdit();
                outboxGrid.CanUserAddRows = false;
                outboxGrid.CanUserDeleteRows = false;
                outboxGrid.AutoGenerateColumns = false;
                outboxGrid.CancelEdit();

                assigneeGrid.CanUserAddRows = false;
                assigneeGrid.CanUserDeleteRows = false;
                assigneeGrid.AutoGenerateColumns = false;
                assigneeGrid.CancelEdit();
                reporterGrid.CanUserAddRows = false;
                reporterGrid.CancelEdit();
                reporterGrid.CanUserDeleteRows = false;
                reporterGrid.AutoGenerateColumns = false;
                reporterGrid.CancelEdit();

                jira1StatusList.Add(JiraStatus.NONE);
                jira1StatusList.Add(JiraStatus.ON_HOLD);
                jira1StatusList.Add(JiraStatus.OPEN);
                jira1StatusList.Add(JiraStatus.IN_PROGRESS);
                jira1StatusList.Add(JiraStatus.CLOSED);
                jira1StatusList.Add(JiraStatus.RESOLVED);
                jira1StatusList.Add(JiraStatus.REOPENED);
                jira1StatusList.Add(JiraStatus.WAITING_FOR_REPORTER_INPUT);

                jira2StatusList.Add(JiraStatus.NONE);
                jira2StatusList.Add(JiraStatus.INCEP_SPEC);
                jira2StatusList.Add(JiraStatus.OPEN);
                jira2StatusList.Add(JiraStatus.IN_PROGRESS);
                jira2StatusList.Add(JiraStatus.CLOSED);
                jira2StatusList.Add(JiraStatus.SUBMITTED);
                jira2StatusList.Add(JiraStatus.IN_REVIEW);
                jira2StatusList.Add(JiraStatus.DEV_REVIEW_DONE);
                jira2StatusList.Add(JiraStatus.VALIDATING_ISSUE);
                jira2StatusList.Add(JiraStatus.ISSUE_DONE);
                jira2StatusList.Add(JiraStatus.READY_FOR_GA);

                jira3StatusList.Add(JiraStatus.NONE);
                jira3StatusList.Add(JiraStatus.OPEN);
                jira3StatusList.Add(JiraStatus.IN_PROGRESS);
                jira3StatusList.Add(JiraStatus.CLOSED);
                jira3StatusList.Add(JiraStatus.RESOLVED);
                jira3StatusList.Add(JiraStatus.SUBMITTED);
                jira3StatusList.Add(JiraStatus.IN_REVIEW);
                jira3StatusList.Add(JiraStatus.VERIFIED);
                jira3StatusList.Add(JiraStatus.BLOCKED);
                jira3StatusList.Add(JiraStatus.FAILED_IN_TESTING);

                HashSet<string> listAsSet = new HashSet<string>(jira1StatusList.Concat(jira2StatusList.Concat(jira3StatusList)));
                statusComboBox.ItemsSource = listAsSet;

                List<string> jiraInstanceList = new List<string>
                {
                    JiraStatus.NONE,
                    "JIRA 1",
                    "JIRA 2",
                    "JIRA 3"
                };

                jiraInstanceComboBox.ItemsSource = jiraInstanceList;
                jiraInstanceComboBox.SelectedIndex = 0;

                List<string> activityList = new List<string>
                {
                    JiraStatus.NONE,
                    "< 24 hours",
                    "< 1 week",
                    "< 30 days",
                    "< 3 months",
                    "< 6 months",
                    "< 1 year"
                };

                activityComboBox.ItemsSource = activityList;

                inboxTable.Columns.Add("reviewKey", typeof(string));
                inboxTable.Columns.Add("author", typeof(string));
                inboxTable.Columns.Add("description", typeof(string));
                inboxTable.Columns.Add("crTooltip", typeof(string));

                outboxTable.Columns.Add("reviewKey", typeof(string));
                outboxTable.Columns.Add("author", typeof(string));
                outboxTable.Columns.Add("description", typeof(string));
                outboxTable.Columns.Add("crTooltip", typeof(string));

                assigneeTable.Columns.Add("issueKey", typeof(string));
                assigneeTable.Columns.Add("issueType", typeof(string));
                assigneeTable.Columns.Add("summary", typeof(string));
                assigneeTable.Columns.Add("status", typeof(string));
                assigneeTable.Columns.Add("jiraNumber", typeof(string));
                assigneeTable.Columns.Add("toolTip", typeof(string));
                assigneeTable.Columns.Add("createdDate", typeof(DateTime));

                reporterTable.Columns.Add("issueKey", typeof(string));
                reporterTable.Columns.Add("issueType", typeof(string));
                reporterTable.Columns.Add("summary", typeof(string));
                reporterTable.Columns.Add("status", typeof(string));
                reporterTable.Columns.Add("jiraNumber", typeof(string));
                reporterTable.Columns.Add("toolTip", typeof(string));
                reporterTable.Columns.Add("createdDate", typeof(DateTime));
            }
            catch (ArgumentNullException argExe)
            {
                Console.WriteLine($"MainWindow: {argExe.Message}");
            }
        }

        private void Window1_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                hyperlinkOpenBrowser.Height = 680;
            }
            else if (this.WindowState == WindowState.Normal)
            {
                hyperlinkOpenBrowser.Height = hyperlinkOpenBrowser.MinHeight;
            }
        }

        public void StatusComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            assigneeIssueCount.Text = string.Empty;
            reporterIssueCount.Text = string.Empty;
            string selectedStatus = Convert.ToString(statusComboBox.SelectedItem);
            if (!selectedStatus.Equals(JiraStatus.NONE))
            {
                statusComboBoxSet = true;
            }
            else
            {
                statusComboBoxSet = false;
            }
            DataView_Filters();
        }

        public void JiraInstanceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            assigneeIssueCount.Text = string.Empty;
            reporterIssueCount.Text = string.Empty;
            string selectedJiraInstance = Convert.ToString(jiraInstanceComboBox.SelectedItem);
            string previousSelectedStatus = Convert.ToString(statusComboBox.SelectedItem);

            bool flag = false;

            if (selectedJiraInstance.Equals("JIRA 1"))
            {
                statusComboBox.ItemsSource = jira1StatusList;
                if (!jira1StatusList.Contains(previousSelectedStatus))
                {
                    flag = true;
                }
            }
            else if (selectedJiraInstance.Equals("JIRA 2"))
            {
                statusComboBox.ItemsSource = jira2StatusList;
                if (!jira2StatusList.Contains(previousSelectedStatus))
                {
                    flag = true;
                }
            }
            else if (selectedJiraInstance.Equals("JIRA 3"))
            {
                statusComboBox.ItemsSource = jira3StatusList;
                if (!jira3StatusList.Contains(previousSelectedStatus))
                {
                    flag = true;
                }
            }
            else
            {
                HashSet<string> listAsSet = new HashSet<string>(jira1StatusList.Concat(jira2StatusList.Concat(jira3StatusList)));
                statusComboBox.ItemsSource = listAsSet;
            }

            if (flag == true)
            {
                statusComboBox.SelectedIndex = 0;
                activityComboBox.SelectedIndex = 0;
                statusComboBoxSet = false;
                activityComboBoxSet = false;
            }

            if (!selectedJiraInstance.Equals(JiraStatus.NONE))
            {
                statusComboBox.IsEnabled = true;
                activityComboBox.IsEnabled = true;
                jiraInstanceComboBoxSet = true;
            }
            else
            {
                jiraInstanceComboBoxSet = false;
            }
            DataView_Filters();
        }

        public void ActivityComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DateTime currentDateAndTime = DateTime.Now;
            string selectedDuration = Convert.ToString(activityComboBox.SelectedIndex);
            assigneeIssueCount.Text = string.Empty;
            reporterIssueCount.Text = string.Empty;

            switch (selectedDuration)
            {
                case "1":
                    thresholdDate = currentDateAndTime.AddHours(-24);
                    break;
                case "2":
                    thresholdDate = currentDateAndTime.AddDays(-7);
                    break;
                case "3":
                    thresholdDate = currentDateAndTime.AddMonths(-1);
                    break;
                case "4":
                    thresholdDate = currentDateAndTime.AddMonths(-3);
                    break;
                case "5":
                    thresholdDate = currentDateAndTime.AddMonths(-6);
                    break;
                case "6":
                    thresholdDate = currentDateAndTime.AddYears(-1);
                    break;
                default:
                    thresholdDate = new DateTime();
                    break;
            }

            if (!Convert.ToString(activityComboBox.SelectedItem).Equals(JiraStatus.NONE))
            {
                activityComboBoxSet = true;
            }
            else
            {
                activityComboBoxSet = false;
            }
            DataView_Filters();
        }

        public void Grid_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            DataGridCell cellInfo = sender as DataGridCell;

            if (cellInfo != null)
            {
                var index = cellInfo.Column.DisplayIndex;
                if (index == 0)
                {
                    DataRowView data = cellInfo.DataContext as DataRowView;
                    if (data != null)
                    {
                        statusProgressBar.Visibility = Visibility.Visible;
                        statusProgressBar.IsIndeterminate = true;
                        Mouse.OverrideCursor = Cursors.Wait;
                        string link = null;
                        if(tabControl.SelectedIndex == 0)
                            link = Convert.ToString(data["toolTip"]);
                        else if(tabControl.SelectedIndex == 1)
                            link = Convert.ToString(data["crToolTip"]);
                        hyperlinkOpenBrowser.Navigate(link);
                        browserTab.Visibility = Visibility.Visible;
                        tabControl.SelectedIndex = 2;
                    }
                }
            }
        }

        public void DataView_Filters()
        {
            if (statusComboBoxSet && jiraInstanceComboBoxSet && activityComboBoxSet)
            {
                assigneeDataView.RowFilter = string.Format("Status = '{0}' and JiraNumber = '{1}' and createdDate >= #{2}#", Convert.ToString(statusComboBox.SelectedItem), jiraInstanceComboBox.SelectedItem.ToString(), thresholdDate);
                reporterDataView.RowFilter = string.Format("Status = '{0}' and JiraNumber = '{1}' and createdDate >= #{2}#", Convert.ToString(statusComboBox.SelectedItem), jiraInstanceComboBox.SelectedItem.ToString(), thresholdDate);
            }
            else if (statusComboBoxSet && jiraInstanceComboBoxSet)
            {
                assigneeDataView.RowFilter = string.Format("Status = '{0}' and JiraNumber = '{1}'", Convert.ToString(statusComboBox.SelectedItem), Convert.ToString(jiraInstanceComboBox.SelectedItem));
                reporterDataView.RowFilter = string.Format("Status = '{0}' and JiraNumber = '{1}'", Convert.ToString(statusComboBox.SelectedItem), Convert.ToString(jiraInstanceComboBox.SelectedItem));
            }
            else if (jiraInstanceComboBoxSet && activityComboBoxSet)
            {
                assigneeDataView.RowFilter = string.Format("JiraNumber = '{0}' and createdDate >= #{1}#", Convert.ToString(jiraInstanceComboBox.SelectedItem), thresholdDate);
                reporterDataView.RowFilter = string.Format("JiraNumber = '{0}' and createdDate >= #{1}#", jiraInstanceComboBox.SelectedItem.ToString(), thresholdDate);
            }
            else if (statusComboBoxSet && activityComboBoxSet)
            {
                assigneeDataView.RowFilter = string.Format("Status = '{0}' and createdDate >= #{1}#", Convert.ToString(statusComboBox.SelectedItem), thresholdDate);
                reporterDataView.RowFilter = string.Format("Status = '{0}' and createdDate >= #{1}#", Convert.ToString(statusComboBox.SelectedItem), thresholdDate);
            }
            else if (statusComboBoxSet)
            {
                assigneeDataView.RowFilter = string.Format("Status = '{0}'", Convert.ToString(statusComboBox.SelectedItem));
                reporterDataView.RowFilter = string.Format("Status = '{0}'", Convert.ToString(statusComboBox.SelectedItem));
            }
            else if (jiraInstanceComboBoxSet)
            {
                assigneeDataView.RowFilter = string.Format("JiraNumber = '{0}'", Convert.ToString(jiraInstanceComboBox.SelectedItem));
                reporterDataView.RowFilter = string.Format("JiraNumber = '{0}'", Convert.ToString(jiraInstanceComboBox.SelectedItem));
            }
            else if (activityComboBoxSet)
            {
                assigneeDataView.RowFilter = string.Format("createdDate >= #{0}#", thresholdDate);
                reporterDataView.RowFilter = string.Format("createdDate >= #{0}#", thresholdDate);
            }
            else
            {
                assigneeDataView = new DataView(assigneeTable);
                reporterDataView = new DataView(reporterTable);
            }
            assigneeGrid.ItemsSource = assigneeDataView;
            reporterGrid.ItemsSource = reporterDataView;
            assigneeIssueCount.Text = Convert.ToString(assigneeDataView.Count);
            reporterIssueCount.Text = Convert.ToString(reporterDataView.Count);
        }

        private void copyAlltoClipboard()
        {
            try
            {
                assigneeGrid.SelectAllCells();
                assigneeGrid.ClipboardCopyMode = DataGridClipboardCopyMode.IncludeHeader;
                ApplicationCommands.Copy.Execute(null, assigneeGrid);
                String resultat = (string)Clipboard.GetData(DataFormats.CommaSeparatedValue);
                String result = (string)Clipboard.GetData(DataFormats.Text);
                assigneeGrid.UnselectAllCells();
                StreamWriter file1 = new StreamWriter(@"\\userprofiles01\XDdata04\Abhishek\My Documents\ExportData.xls");
                file1.WriteLine(result.Replace(',', ' '));
                file1.Close();

                MessageBox.Show("Exporting Assignee JIRA data to Excel file. ExportData.xls is created successfully.");
            }
            catch (AccessViolationException)
            {
                MessageBox.Show("Have encountered access violation. This could be issue with Excel 2000 if that is only version installed on computer","Access Violation");
            }
            catch (Exception)
            {
                MessageBox.Show("Unknown error","Unknown error");
            }

        }
        public void AssociateIdClearButton_Click(object sender, RoutedEventArgs e)
        {
            copyAlltoClipboard();
        }

        public void AssociateIdSubmitButton_Click(object sender, RoutedEventArgs e)
        {
            associateIdSubmitButton.IsEnabled = false;
            Mouse.OverrideCursor = Cursors.Wait;
            statusComboBox.IsEnabled = false;
            activityComboBox.IsEnabled = false;
            string associateID = inputText.Text.Trim();
            ID = GetAssociateID(associateID);
            string password = inputPass.Password.ToString();
            inputPass.IsEnabled = false;
            inboxList.Clear();
            outboxList.Clear();
            assigneeList.Clear();
            reporterList.Clear();
            sortedAssigneeList.Clear();
            sortedReporterList.Clear();
            assigneeIssueCount.Text = string.Empty;
            reporterIssueCount.Text = string.Empty;

            jiraInstanceComboBox.SelectedIndex = 0;
            statusComboBox.SelectedIndex = 0;
            activityComboBox.SelectedIndex = 0;
            inboxTable.Rows.Clear();
            outboxTable.Rows.Clear();
            assigneeTable.Rows.Clear();
            reporterTable.Rows.Clear();

            statusProgressBar.IsIndeterminate = true;
            statusProgressBar.Visibility = Visibility.Visible;

            ExtractingDataFromJIRATasks(associateID);
            ExtractingDataFromCrucibleReviews(associateID, password);
        }

        public string GetAssociateID(string associateID)
        {
            return associateID.ToUpper();
        }

        public string Authentication(string assID, string pass)
        {
            string _tkn;
            string url = "http://crucible03.com/viewer/rest-service/auth-v1/login?userName=" + assID + "&password=" + pass;
            WebClient _wclient = new WebClient();
            _wclient.UseDefaultCredentials = true;

            string _content = null;
            try
            {
                _content = _wclient.DownloadString(url);
            }
            catch(WebException excep)
            {
                Console.WriteLine(excep.Message);
            }
            _tkn = parseContent(_content);
            return _tkn;
        }

        public string parseContent(string data)
        {
            XmlDocument xdoc = new XmlDocument() { XmlResolver = null };

            try
            {
                StringReader reader = new StringReader(data);
                XmlTextReader xmlreader = new XmlTextReader(reader)
                {
                    DtdProcessing = DtdProcessing.Prohibit
                };
                xdoc.Load(xmlreader);

                var nodes = xdoc.SelectNodes("/loginResult");
                string _token = null;

                foreach (XmlNode xn in nodes)
                {
                    _token = _token + xn["token"].InnerText;
                }
                return _token;
            }
            catch(Exception excep)
            {
                Console.WriteLine(excep.Message);
                return null;
            }
        }

        public void ExtractingDataFromCrucibleReviews(string associateID, string password)
        {
            string token = Authentication(associateID, password);
            GetCrucibleReviews(token);
           
        }

        public void GetCrucibleReviews(string _linkToken)
        {
            StatusBlock.Text = statusBar;
            string inboxURL = @"http://crucible03.com/viewer/cru/rssReviewFilter?filter=inbox&FEAUTH=" + _linkToken;
            string outboxURL = @"http://crucible03.com/viewer/cru/rssReviewFilter?filter=outbox&FEAUTH=" + _linkToken;
            string inboxcontent, outboxcontent = string.Empty;

            try
            {
                WebClient _wclient = new WebClient();
                _wclient.UseDefaultCredentials = true;
                inboxcontent = _wclient.DownloadString(inboxURL);
                outboxcontent = _wclient.DownloadString(outboxURL);
                AddContentToCrucibleTable(inboxcontent, "inbox");
                AddContentToCrucibleTable(outboxcontent, "outbox");
                statusProgressBar.Value = 0;
            }
            catch(Exception excep)
            {
                Console.WriteLine(excep.Message);
            }
        }

        public void AddContentToCrucibleTable(string xml, string indicator)
        {
            string[] cruID = null;
            string title = null;
            XmlDocument xdoc = new XmlDocument() { XmlResolver = null };
            try
            {
                StringReader reader = new StringReader(xml);
                XmlTextReader xmlreader = new XmlTextReader(reader)
                {
                    DtdProcessing = DtdProcessing.Prohibit
                };
                xdoc.Load(xmlreader);
                var nodes = xdoc.SelectNodes("/rss/channel/item");

                if (indicator == "inbox")
                {

                    int inbox = 0;
                    foreach (XmlNode xn in nodes)
                    {
                        CrucibleReviews cr = new CrucibleReviews();
                        title = xn["title"].InnerText;
                        cr.author = xn["author"].InnerText;
                        cruID = title.Split(new Char[] { ':' });
                        cr.reviewKey = cruID[0];
                        cr.description = cruID[1];
                        cr.crToolTip = "http://crucible03.com/viewer/cru/" + cruID[0];
                        inboxList.Add(cr);
                        inboxTable.Rows.Add(cr.reviewKey, cr.author, cr.description, cr.crToolTip);
                        inbox++;
                    }
                    InboxReviewCount.Text = inbox.ToString();
                }
                else if (indicator == "outbox")
                {
                    int outbox = 0;
                    foreach (XmlNode xn in nodes)
                    {
                        CrucibleReviews cr = new CrucibleReviews();
                        cr.author = xn["author"].InnerText;
                        title = xn["title"].InnerText;
                        cruID = title.Split(new Char[] { ':' });
                        cr.reviewKey = cruID[0];
                        cr.description = cruID[1];
                        cr.crToolTip = "http://crucible03.com/viewer/cru/" + cruID[0];
                        outboxList.Add(cr);
                        outboxTable.Rows.Add(cr.reviewKey, cr.author, cr.description, cr.crToolTip);
                        outbox++;
                    }
                    OutboxReviewCount.Text = outbox.ToString();
                }

                inboxView = new DataView(inboxTable);
                outboxView = new DataView(outboxTable);
                inboxGrid.DataContext = inboxView;
                inboxGrid.ItemsSource = inboxView;
                outboxGrid.ItemsSource = outboxView;
            }
            catch(Exception excep)
            {
                Console.WriteLine(excep.Message);
            }
        }

        public async void ExtractingDataFromJIRATasks(string associateID)
        {
            statusBar = "Loading details...";

            try
            {

                if (string.IsNullOrWhiteSpace(associateID))
                {
                    inputText.Text = string.Empty;
                    assigneeTable.Rows.Clear();
                    reporterTable.Rows.Clear();
                    statusProgressBar.Value = 0;
                    MessageBox.Show("Associate ID field should not be blank.", "User Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    inputText.Focus();
                }
                else
                {
                    if (!CountIdLength(associateID))
                    {
                        statusProgressBar.Value = 0;
                        MessageBoxResult mes = MessageBox.Show("Associate ID is invalid. Please enter a valid Associate ID.", "Warning", MessageBoxButton.OK, MessageBoxImage.Error);
                        inputText.Focus();
                        
                        inputText.Text = string.Empty;
                        assigneeGrid.Items.Clear();
                        reporterGrid.Items.Clear();
                        inboxGrid.Items.Clear();
                        outboxGrid.Items.Clear();
                    }
                    else
                    {
                        string assigneeJiraURL = $"https://jira.com/rest/api/latest/search?jql=assignee={associateID}&maxResults=10000";
                        string assigneeJira2URL = $"https://jira2.com/rest/api/latest/search?jql=assignee={associateID}&maxResults=10000";
                        string assigneeJira3URL = $"https://jira3.com/rest/api/latest/search?jql=assignee={associateID}&maxResults=10000";

                        string reporterJiraURL = $"https://jira.com/rest/api/latest/search?jql=reporter={associateID}&maxResults=10000";
                        string reporterJira2URL = $"https://jira2.com/rest/api/latest/search?jql=reporter={associateID}&maxResults=10000";
                        string reporterJira3URL = $"https://jira3.com/rest/api/latest/search?jql=reporter={associateID}&maxResults=10000";

                        StatusBlock.Text = statusBar;
                        statusProgressBar.Value = 0;
                        statusProgressBar.IsIndeterminate = true;

                        // Assignee
                        Task<dynamic> assigneeResult = JsonParse(assigneeJiraURL);
                        Task<dynamic> assignee2Result = JsonParse(assigneeJira2URL);
                        Task<dynamic> assignee3Result = JsonParse(assigneeJira3URL);

                        // Reporter
                        Task<dynamic> reporterResult = JsonParse(reporterJiraURL);
                        Task<dynamic> reporter2Result = JsonParse(reporterJira2URL);
                        Task<dynamic> reporter3Result = JsonParse(reporterJira3URL);

                        await Task.WhenAll(assigneeResult, assignee2Result, assignee3Result, reporterResult, reporter2Result, reporter3Result);

                        var assigneeMsgprop = assigneeResult.Result.Property("warningMessages");
                        var reporterMsgprop = reporterResult.Result.Property("warningMessages");

                        if (assigneeMsgprop != null || reporterMsgprop!= null)
                        {
                            MessageBoxResult mes = MessageBox.Show("Associate ID is invalid. Please enter a valid Associate ID.", "Warning", MessageBoxButton.OK, MessageBoxImage.Error);
                            inputText.Focus();

                            inputText.Text = string.Empty;
                            assigneeGrid.Items.Clear();
                            reporterGrid.Items.Clear();
                            inboxGrid.Items.Clear();
                            outboxGrid.Items.Clear();
                        }

                        else
                        {
                            //Assignee
                            StoreIssuesToList(assigneeResult.Result, 1, "assignee");
                            StoreIssuesToList(assignee2Result.Result, 2, "assignee");
                            StoreIssuesToList(assignee3Result.Result, 3, "assignee");

                            SortingOrderPriority(assigneeList, sortedAssigneeList);
                            ExportToXL(assigneeList);
                            PushListValuesToTable(sortedAssigneeList, "assignee");
                            assigneeIssueCount.Text = Convert.ToString(sortedAssigneeList.Count);

                            //Reporter
                            StoreIssuesToList(reporterResult.Result, 1, "reporter");
                            StoreIssuesToList(reporter2Result.Result, 2, "reporter");
                            StoreIssuesToList(reporter3Result.Result, 3, "reporter");

                            SortingOrderPriority(reporterList, sortedReporterList);
                            PushListValuesToTable(sortedReporterList, "reporter");
                            reporterIssueCount.Text = Convert.ToString(sortedReporterList.Count);

                            associateNameBlock.Text = "Associate Name: " + associateName;
                        }

                        StatusBlock.Text = "Ready";
                        statusProgressBar.Value = 100;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AssociateIdSubmitButton_Click: {ex.Message}");
            }
            finally
            {
                Console.WriteLine("Finally AssociateIdSubmitButton_Click");
                Mouse.OverrideCursor = null;
                statusProgressBar.IsIndeterminate = false;
                statusProgressBar.Visibility = Visibility.Hidden;
            }
        }

        private void ExportToXL(List<JiraIssues> assigneeList)
        {
            string csv = String.Join(",", assigneeList.Select(x => x.ToString()).ToArray());
        }

        public void PushListValuesToTable(List<JiraIssues> List, string gridName)
        {
            foreach (JiraIssues item in List)
            {
                string jiraNumber = "JIRA 1";
                switch (item.jiraAddress)
                {
                    case "https://jira.com/browse/":
                        jiraNumber = "JIRA 1";
                        break;
                    case "https://jira2.com/browse/":
                        jiraNumber = "JIRA 2";
                        break;
                    case "https://jira3.com/browse/":
                        jiraNumber = "JIRA 3";
                        break;
                    default:
                        jiraNumber = string.Empty;
                        break;
                }

                if (gridName.Equals("assignee"))
                    assigneeTable.Rows.Add(item.issueKey, item.issueType, item.summary, item.status, jiraNumber, item.toolTip, item.createdDate);
                else if (gridName.Equals("reporter"))
                    reporterTable.Rows.Add(item.issueKey, item.issueType, item.summary, item.status, jiraNumber, item.toolTip, item.createdDate);
            }

            if (gridName.Equals("assignee"))
            {
                assigneeDataView = new DataView(assigneeTable);
                assigneeGrid.DataContext = assigneeDataView;
                assigneeGrid.ItemsSource = assigneeDataView;
            }
            else if (gridName.Equals("reporter"))
            {
                reporterDataView = new DataView(reporterTable);
                reporterGrid.DataContext = reporterDataView;
                reporterGrid.ItemsSource = reporterDataView;
            }
            
        }

        public void StoreIssuesToList(dynamic Result, int jiraNumber, string gridName)
        {
            foreach (dynamic issue in Result.issues)
            {
                JiraIssues jiraIssue = new JiraIssues();
                jiraIssue.issueKey = issue.key;
                jiraIssue.issueType = issue.fields.issuetype.name;
                jiraIssue.status = issue.fields.status.name;
                jiraIssue.summary = issue.fields.summary;
                jiraIssue.createdDate = issue.fields.updated;

                switch (jiraNumber)
                {
                    case 1:
                        jiraIssue.jiraAddress = "https://jira.com/browse/";
                        break;
                    case 2:
                        jiraIssue.jiraAddress = "https://jira2.com/browse/";
                        break;
                    case 3:
                        jiraIssue.jiraAddress = "https://jira3.com/browse/";
                        break;
                    default:     
                        jiraIssue.jiraAddress = string.Empty;
                        break;
                }

                jiraIssue.toolTip = jiraIssue.jiraAddress + issue.key;

                if (gridName.Equals("assignee"))
                {
                    associateName = issue.fields.assignee.displayName;
                    assigneeList.Add(jiraIssue);
                }

                if (gridName.Equals("reporter"))
                {
                    reporterList.Add(jiraIssue);
                }
            }
        }

        public void SortingOrderPriority(List<JiraIssues> actualList, List<JiraIssues> sortedList)
        {
            //High Priority            
            SortingValues(JiraStatus.IN_PROGRESS, actualList, sortedList);
            SortingValues(JiraStatus.OPEN, actualList, sortedList);
            SortingValues(JiraStatus.REOPENED, actualList, sortedList);
            SortingValues(JiraStatus.WAITING_FOR_REPORTER_INPUT, actualList, sortedList);
            SortingValues(JiraStatus.SUBMITTED, actualList, sortedList);
            SortingValues(JiraStatus.INCEP_SPEC, actualList, sortedList);
            SortingValues(JiraStatus.IN_REVIEW, actualList, sortedList);
            SortingValues(JiraStatus.VERIFIED, actualList, sortedList);
            SortingValues(JiraStatus.DEV_REVIEW_DONE, actualList, sortedList);
            SortingValues(JiraStatus.VALIDATING_ISSUE, actualList, sortedList);

            //Low Priority
            SortingValues(JiraStatus.ON_HOLD, actualList, sortedList);
            SortingValues(JiraStatus.READY_FOR_GA, actualList, sortedList);
            SortingValues(JiraStatus.ISSUE_DONE, actualList, sortedList);
            SortingValues(JiraStatus.BLOCKED, actualList, sortedList);
            SortingValues(JiraStatus.RESOLVED, actualList, sortedList);
            SortingValues(JiraStatus.CLOSED, actualList, sortedList);
            SortingValues(JiraStatus.DONE, actualList, sortedList);
            SortingValues(JiraStatus.FAILED_IN_TESTING, actualList, sortedList);
        }

        public void SortingValues(string status, List<JiraIssues> listValues, List<JiraIssues> sortedList)
        {
            foreach (JiraIssues issue in listValues)
            {
                if (!issue.addedToTable)
                {
                    if (issue.status.Equals(status))
                    {
                        sortedList.Add(issue);
                        issue.addedToTable = true;
                    }
                }
            }
        }

        public static Task<dynamic> JsonParse(string JiraURL)
        {            
            return Task.Run<dynamic>(() =>
            {
                string jsonResult = GetJsonWeb(JiraURL);
                dynamic jObject = JObject.Parse(jsonResult);
                return jObject;
            });
        }

        public static string GetJsonWeb(string url)
        {
            WebClient webClient = new WebClient();
            return webClient.DownloadString(url);
        }

        public void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            StatusBlock.Text = string.Empty;
            statusProgressBar.Visibility = Visibility.Visible;

            if (tabControl.SelectedIndex.Equals(0))
            {
                FilterGroupBox.IsEnabled = true;
            }
            else if (tabControl.SelectedIndex.Equals(1) | tabControl.SelectedIndex.Equals(3) | tabControl.SelectedIndex.Equals(4))
            {
                FilterGroupBox.IsEnabled = false;
            }
            else
            {
                FilterGroupBox.IsEnabled = false;
                StatusBlock.Text = string.Empty;
                statusProgressBar.Value = 0;
            }
            if (tabControl.SelectedIndex.Equals(2) | tabControl.SelectedIndex.Equals(3) | tabControl.SelectedIndex.Equals(4))
            {
                statusBar = string.Empty;
                MainWindow1.ResizeMode = ResizeMode.CanResize;
            }
            else
            {
                this.WindowState = WindowState.Normal;
                MainWindow1.ResizeMode = ResizeMode.CanMinimize;
            }
        }

        public void InputText_TextChanged(object sender, TextChangedEventArgs e)
        {
            inputPass.IsEnabled = true;
            associateIdSubmitButton.IsEnabled = true;
            statusComboBox.SelectedIndex = 0;
            statusComboBoxSet = false;
            jiraInstanceComboBox.SelectedIndex = 0;
            jiraInstanceComboBoxSet = false;
            activityComboBox.SelectedIndex = 0;
            activityComboBoxSet = false;

            assigneeGrid.ItemsSource = null;
            reporterGrid.ItemsSource = null;
            inboxGrid.ItemsSource = null;
            outboxGrid.ItemsSource = null;

            assigneeIssueCount.Text = null;
            reporterIssueCount.Text = null;

            statusComboBox.IsEnabled = false;
            activityComboBox.IsEnabled = false;
            associateNameBlock.Text = string.Empty;
            StatusBlock.Text = string.Empty;
            statusProgressBar.Value = 0;
            statusProgressBar.Visibility = Visibility.Hidden;

            hyperlinkOpenBrowser.Navigate("about:blank");
        }

        public bool CountIdLength(string associateID)  
        {
            if (associateID.Length.Equals(8))
                return true;
            return false;
        }       

        private void LoadCompletedAction(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            statusProgressBar.IsIndeterminate = false;
            Mouse.OverrideCursor = null;
            statusProgressBar.Value = 100;
            statusProgressBar.Visibility = Visibility.Hidden;
        }
    }
}
