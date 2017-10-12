using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using Microsoft.SqlServer.Dts.Runtime;
using PP.SSIS.ControlFlow.Waiting.Properties;

namespace PP.SSIS.ControlFlow.Waiting.UI
{
    public partial class WaitForFileForm : Form, IUIForm
    {
        /// <summary>
        /// Class for storing the WaitForFileProperties for the purpose of editing in the EditForm
        /// </summary>
        private class WaitForFileProperties
        {
            [Browsable(false)]
            protected WaitForFile TaskControl { get; private set; }

            public WaitForFileProperties(WaitForFile taskControl)
            {
                TaskControl = taskControl;
                InitProperties();
            }

            /// <summary>
            /// Initializes the properties from the associated WaitForFile Task
            /// </summary>
            public void InitProperties()
            {
                CheckInterval = TaskControl.CheckInterval;
                CheckTimeoutInterval = TaskControl.CheckTimeoutInterval;
                CheckTimeoutTime = TaskControl.CheckTimeoutTime;
                TimeoutNextDayIfTimePassed = TaskControl.TimeoutNextDayIfTimePassed;
                CheckType = TaskControl.CheckType;
                ExistenceType = TaskControl.ExistenceType;
            }

            private TimeSpan _day = new TimeSpan(1, 0, 0, 0);

            private int checkInterval = 2000;
            [Category("Wait For File")]
            [Description("Specifies interval in millisecionds in which the file(s) existence/non-existence is being checked")]
            [DefaultValue(2000)]
            public int CheckInterval
            {
                get { return checkInterval; }
                set { checkInterval = value; }
            }

            private TimeSpan checkTimeoutInterval = TimeSpan.Zero;
            [Category("Wait For File")]
            [Description("Specifies the time interval in hh:mm:ss after which TieMout occurs. TimeoutInterval of 00:00:00 means there is no timeout. Task times out either on CheckTimeoutInterval or CheckTimeOutTime depending what occurs earlier.")]
            [DefaultValue("00:00:00")]
            public TimeSpan CheckTimeoutInterval
            {
                get { return checkTimeoutInterval; }
                set
                {
                    if (value < TimeSpan.Zero)
                        throw new System.InvalidCastException("CheckTimeoutInterval has to be greater or equal to 00:00:00");
                    checkTimeoutInterval = value;
                }
            }

            private TimeSpan checkTimeoutTime = TimeSpan.Zero;
            [Category("Wait For File")]
            [Description("Specifies the time of a day hh:mm:ss at which TimeOut occurs. CheckTimeOutTime of 00:00:00 means there is no timeout. Task times out either on CheckTimeoutInterval or CheckTimeOutTime depending what occurs earlier.")]
            [DefaultValue("00:00:00")]
            public TimeSpan CheckTimeoutTime
            {
                get { return checkTimeoutTime; }
                set
                {
                    if (value < TimeSpan.Zero || value > _day)
                    {
                        throw new System.InvalidCastException("CheckTimeoutTime has to be in interval 00:00:00 - 23:50:59.999");
                    }
                    checkTimeoutTime = value;
                }
            }

            private bool timeoutNextDayIfTimePassed = false;
            [Category("Wait For File")]
            [Description("Specifies whether CheckTimeOutTime has already passed at the time of execution, whether to set the Timeout on the same time next day.")]
            [DefaultValue(false)]
            public bool TimeoutNextDayIfTimePassed
            {
                get { return timeoutNextDayIfTimePassed; }
                set { timeoutNextDayIfTimePassed = value; }
            }


            private PP.SSIS.ControlFlow.Waiting.WaitForFile.ChekFileType checkType;
            [Category("Wait For File")]
            [Description("Specifies the CheckType. Existence means files are checked for their existence. NonExistence means that files are checked for Non Existence. This means then condition is true when the file does not exists.")]
            [DefaultValue(PP.SSIS.ControlFlow.Waiting.WaitForFile.ChekFileType.Existence)]
            public PP.SSIS.ControlFlow.Waiting.WaitForFile.ChekFileType CheckType
            {
                get { return checkType; }
                set { checkType = value; }
            }

            private PP.SSIS.ControlFlow.Waiting.WaitForFile.FileExistenceType existenceType;
            [Category("Wait For File")]
            [Description("Specifies Whether Existence is being checked for All files or for any file. In case of All, all files has to exists or not exists. In case of Any, if any of the file exists or not exists depending on the Check type, the check is successfull.")]
            [DefaultValue(PP.SSIS.ControlFlow.Waiting.WaitForFile.FileExistenceType.All)]
            public PP.SSIS.ControlFlow.Waiting.WaitForFile.FileExistenceType ExistenceType
            {
                get { return existenceType; }
                set { existenceType = value; }
            }

            /// <summary>
            /// Persists current properties values to the associated WaitForFileTask
            /// </summary>
            public void PersistProperties()
            {
                TaskControl.CheckInterval = CheckInterval;
                TaskControl.CheckTimeoutInterval = CheckTimeoutInterval;
                TaskControl.CheckTimeoutTime = CheckTimeoutTime;
                TaskControl.TimeoutNextDayIfTimePassed = TimeoutNextDayIfTimePassed;
                TaskControl.CheckType = CheckType;
                TaskControl.ExistenceType = ExistenceType;
            }
        }

        /// <summary>
        /// WaitForFileProperties
        /// </summary>
        WaitForFileProperties TaskProperties { get; set; }

        /// <summary>
        /// Gets the UIHelper
        /// </summary>
        public IUIHelper UIHelper { get; private set; }
        /// <summary>
        /// Gets the Wait ForForFile TaskControl 
        /// </summary>
        public WaitForFile TaskControl
        {
            get { return UIHelper.TaskHost.InnerObject as WaitForFile; }
        }

        public void Initialize(IUIHelper uiHelper)
        {
            UIHelper = uiHelper;
        }

        public WaitForFileForm()
        {
            InitializeComponent();

            foreach (Control c in propControlProperties.Controls)
            {
                ToolStrip ts = c as ToolStrip;
                if (ts != null)
                {
                    ts.Items.Add(new ToolStripSeparator());
                    ToolStripButton btnReset = new ToolStripButton("Reset");
                    btnReset.Click += btnReset_Click; 
                    ts.Items.Add(btnReset);
                }
            }

        }

        void btnReset_Click(object sender, EventArgs e)
        {
            TaskProperties.InitProperties();
            propControlProperties.SelectedObject = TaskProperties;
        }

        private void WaitForFileForm_Load(object sender, EventArgs e)
        {
            this.Text = string.Format(Resources.EditorFor, UIHelper.TaskHost.Name);

            TaskProperties = new WaitForFileProperties(UIHelper.TaskHost.InnerObject as WaitForFile);
            propControlProperties.SelectedObject = TaskProperties;

            clbFiles.SuspendLayout();
            foreach (string file in TaskControl.Files)
            {
                clbFiles.Items.Add(file, false);
            }
            clbFiles.ResumeLayout();

        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            TaskProperties.PersistProperties();

            if (clbFiles.Items.Count > 0)
            {
                List<string> files = new List<string>(clbFiles.Items.Count);
                foreach (string file in clbFiles.Items)
                {
                    files.Add(file);
                }
                TaskControl.FilesToCheck = string.Join("|", files);
            }
            else
                TaskControl.FilesToCheck = string.Empty;

        }

        private void btnAddFile_Click(object sender, EventArgs e)
        {
            if (ofdSelectFile.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var files = ofdSelectFile.FileNames;
                clbFiles.Items.AddRange(files);
            }
        }

        private void btnRemoveFile_Click(object sender, EventArgs e)
        {
            if (clbFiles.CheckedIndices.Count > 0)
            {
                while (clbFiles.CheckedIndices.Count > 0)
                {
                    clbFiles.Items.RemoveAt(clbFiles.CheckedIndices[0]);
                }
            }
            else if (clbFiles.SelectedIndex >= 0)
            {
                clbFiles.Items.RemoveAt(clbFiles.SelectedIndex);
            }
        }

        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < clbFiles.Items.Count; i++)
			{
                clbFiles.SetItemChecked(i, true);
			}            
        }

        private void deselectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < clbFiles.Items.Count; i++)
            {
                clbFiles.SetItemChecked(i, false);
            }
        }

        private void cmsFiles_Opening(object sender, CancelEventArgs e)
        {
            e.Cancel = clbFiles.Items.Count == 0;
        }


    }
}
