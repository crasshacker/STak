#if POWERSHELL

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace STak.WinTak
{
    public partial class ConsoleWindow : StickyTakWindow
    {
        private PowerShell m_powershell;
        private Runspace   m_runspace;


        public ConsoleWindow()
        {
            InitializeComponent();
            m_console.CaretIndex = 0;
            m_console.PreviewKeyDown += (s, e) => OnPreviewKeyDown(s, e);  
        }


        public void InitializePowerShell()
        {
            m_runspace = RunspaceFactory.CreateRunspace();
            m_runspace.ThreadOptions = PSThreadOptions.UseCurrentThread;
            m_runspace.Open();

            m_runspace.SessionStateProxy.SetVariable("w", MainWindow.Instance);
            m_runspace.SessionStateProxy.SetVariable("g", MainWindow.Instance.GetMirroringGame(Guid.Empty));

            m_powershell = PowerShell.Create();
            m_powershell.Runspace = m_runspace;
        }


        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                int index = m_console.Text.LastIndexOf("\n");
                string command = m_console.Text.Substring(index+1);
                string result = "\n";

                if (command.Length > 0)
                {
                    command += " | Out-String";
                    m_powershell.Commands.Clear();

                    try
                    {
                        Collection<PSObject> output = m_powershell.AddScript(command).Invoke();

                        foreach (PSObject          obj in output                    )       { result +=               obj.ToString() + "\n"; }
                        foreach (ErrorRecord       obj in m_powershell.Streams.Error)       { result += "ERROR: "   + obj.ToString() + "\n"; }
                        foreach (WarningRecord     obj in m_powershell.Streams.Warning)     { result += "WARN: "    + obj.ToString() + "\n"; }
                        foreach (InformationRecord obj in m_powershell.Streams.Information) { result += "INFO: "    + obj.ToString() + "\n"; }
                        foreach (VerboseRecord     obj in m_powershell.Streams.Verbose)     { result += "VERBOSE: " + obj.ToString() + "\n"; }
                    }
                    catch (Exception ex)
                    {
                        result += ex.Message + "\n";
                    }

                    m_powershell.Streams.Error      .Clear();
                    m_powershell.Streams.Warning    .Clear();
                    m_powershell.Streams.Information.Clear();
                    m_powershell.Streams.Verbose    .Clear();
                }

                m_console.Text += result;
                m_console.CaretIndex = m_console.Text.Length;
                m_scrollViewer.ScrollToBottom();
            }
        }


        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }
    }
}

#else

using System;
using System.Windows;
using System.Windows.Controls;

namespace STak.WinTak
{
    public partial class ConsoleWindow : StickyTakWindow
    {
        public ConsoleWindow()
        {
            InitializeComponent();
        }


        public void InitializePowerShell()
        {
        }
    }
}

#endif
