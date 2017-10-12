using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.SqlServer.Dts.Runtime;
using PP.SSIS.ControlFlow.Waiting.Properties;

namespace PP.SSIS.ControlFlow.Waiting.UI
{
    public partial class TaskPropertiesFrom<TTask, TTaskProperties> : Form, IUIForm
        where TTaskProperties : TaskPropertyClass<TTask>, new()
        where TTask : Task
    {
        public TaskPropertiesFrom()
        {
            InitializeComponent();
            foreach (Control c in propGrid.Controls)
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
            TaskProperties.InitializeProperties();
            propGrid.SelectedObject = TaskProperties;
        }


        public IUIHelper UIHelper { get; private set; }
        public TTask TaskControl
        {
            get { return UIHelper.TaskHost.InnerObject as TTask; }
        }

        protected TTaskProperties TaskProperties { get; private set; }

        public void Initialize(IUIHelper uiHelper)
        {
            UIHelper = uiHelper;
            TaskProperties = new TTaskProperties();
            TaskProperties.SetTaskControl(TaskControl);
            this.Text = string.Format(Resources.EditorFor, UIHelper.TaskHost.Name);
        }

        private void TaskPropertiesFrom_Load(object sender, EventArgs e)
        {
            TaskProperties.InitializeProperties();

            propGrid.SelectedObject = TaskProperties;

        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            TaskProperties.PersistProperties();
            this.Close();
        }
    }
}
