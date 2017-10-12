using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.Xml.Linq;
using Microsoft.SqlServer.Dts.Runtime;
using PP.SSIS.ControlFlow.Logging.Properties;

namespace PP.SSIS.ControlFlow.Logging.UI
{
    public partial class VariablesToXmlForm : Form, IUIForm
    {
        private class TaskProperties
        {
            [Browsable(false)]
            protected VariablesToXmlTask TaskControl { get; private set; }

            public TaskProperties(VariablesToXmlTask taskControl)
            {
                TaskControl = taskControl;

                RootElementName = TaskControl.RootElementName;
                VariableElementName = TaskControl.VariableElementName;
                ExportDataType = TaskControl.ExportDataType;
                ExportValueDataType = TaskControl.ExportValueDataType;
                ExportVariablePath = TaskControl.ExportVariablePath;
                ExportDescription = TaskControl.ExportDescription;
                ExportBinaryData = TaskControl.ExportBinaryData;
                XmlSaveOptions = TaskControl.XmlSaveOptions;
            }

            public void PersistProperties()
            {
                TaskControl.RootElementName = RootElementName;
                TaskControl.VariableElementName = VariableElementName;
                TaskControl.ExportDataType = ExportDataType;
                TaskControl.ExportValueDataType = ExportValueDataType;
                TaskControl.ExportVariablePath = ExportVariablePath;
                TaskControl.ExportDescription = ExportDescription;
                TaskControl.ExportBinaryData = ExportBinaryData;
                TaskControl.XmlSaveOptions = XmlSaveOptions;
            }

            private string _rootElementName = "variables";
            [Category("Variables To Xml Settings")]
            [Description("Name of the Root XML Element")]
            [DefaultValue("variables")]
            public string RootElementName
            {
                get { return _rootElementName; }
                set { _rootElementName = value; }
            }

            private string _variableElementName = "variable";
            [Category("Variables To Xml Settings")]
            [Description("Name of the variable Element")]
            [DefaultValue("variable")]
            public string VariableElementName
            {
                get { return _variableElementName; }
                set { _variableElementName = value; }
            }


            private bool _exportDataType = true;
            [Category("Variables To Xml Settings")]
            [Description("Specifies whether attribute with variable data type should be exportred")]
            [DefaultValue(true)]
            public bool ExportDataType
            {
                get { return _exportDataType; }
                set { _exportDataType = value; }
            }

            private bool _exportValueDataType = true;
            [Category("Variables To Xml Settings")]
            [Description("Specifies whether attribute with variable value data type should be exportred")]
            [DefaultValue(true)]
            public bool ExportValueDataType
            {
                get { return _exportValueDataType; }
                set { _exportValueDataType = value; }
            }

            private bool _exportVariablePath = true;
            [Category("Variables To Xml Settings")]
            [Description("Specifies whether attribute with variable Package Path should be exported")]
            [DefaultValue(true)]
            public bool ExportVariablePath
            {
                get { return _exportVariablePath; }
                set { _exportVariablePath = value; }
            }

            private bool _exportDescription = true;
            [Category("Variables To Xml Settings")]
            [Description("Specifies whether attribute with variable Description should be exported")]
            [DefaultValue(true)]
            public bool ExportDescription
            {
                get { return _exportDescription; }
                set { _exportDescription = value; }
            }

            private bool _exportBinaryData = false;
            [Category("Variables To Xml Settings")]
            [Description("Specifies whether binary data of object variable should be exported")]
            [DefaultValue(false)]
            public bool ExportBinaryData
            {
                get { return _exportBinaryData; }
                set { _exportBinaryData = value; }
            }


            private SaveOptions _xmlSaveOption;
            [Category("Variables To Xml Settings")]
            [Description("Specifies the Xml SaveOptions to be used when exporting the xml")]
            [DefaultValue(SaveOptions.None)]
            public SaveOptions XmlSaveOptions
            {
                get { return _xmlSaveOption; }
                set { _xmlSaveOption = value; }
            }

        }

        TaskProperties _taskProperties;
        public bool Loading { get; private set; }

        public IUIHelper UIHelper { get; private set; }
        public VariablesToXmlTask TaskControl
        {
            get
            {
                return UIHelper != null && UIHelper.TaskHost != null ? UIHelper.TaskHost.InnerObject as VariablesToXmlTask : null;
            }
        }



        public void Initialize(IUIHelper uiHelper)
        {
            UIHelper = uiHelper;
        }

        public VariablesToXmlForm()
        {
            try
            {
                Loading = true;
                InitializeComponent();
            }
            finally
            {
                Loading = false;
            }
        }

        private void WaitForFileForm_Load(object sender, EventArgs e)
        {
            try
            {
                Loading = true;

                this.Text = string.Format(Resources.EditorFor, UIHelper.TaskHost.Name);

                _taskProperties = new TaskProperties(UIHelper.TaskHost.InnerObject as VariablesToXmlTask);

                propControlProperties.SelectedObject = _taskProperties;

                string controlPath = UIHelper.TaskHost.GetPackagePath();

                List<string> selected = new List<string>(TaskControl.ExportVariables.Count);
                foreach (string var in TaskControl.ExportVariables)
                {
                    if (UIHelper.TaskHost.Variables.Contains(var))
                    {
                        Variable v = UIHelper.TaskHost.Variables[var];
                        selected.Add(v.QualifiedName);
                    }
                }


                lvVariables.SuspendLayout();
                lvVariables.Items.Clear();
                lbSelectedVariables.SuspendLayout();
                lbSelectedVariables.Items.Clear();
                cmbXmlVariable.SuspendLayout();
                cmbXmlVariable.Items.Clear();

                foreach (Variable var in UIHelper.TaskHost.Variables)
                {
                    if (var.GetPackagePath() == string.Format("{0}.Variables[{1}]", controlPath, var.QualifiedName))
                        continue;

                    ListViewGroup lvg = lvVariables.Groups[var.Namespace];

                    if (lvg == null)
                        lvg = lvVariables.Groups.Add(var.Namespace, var.Namespace);

                    ListViewItem lvi = new ListViewItem(var.Name, lvg);
                    lvi.Tag = var.QualifiedName;
                    lvi.Name = var.QualifiedName;

                    lvi.SubItems.Add(var.DataType.ToString());
                    lvi.SubItems.Add(var.Description);
                    lvi.SubItems.Add(var.QualifiedName);
                    lvi.SubItems.Add(var.GetPackagePath());

                    if (selected.Contains(var.QualifiedName))
                        lvi.Checked = true;

                    if (var.Namespace == "User" && (var.DataType == TypeCode.String || var.DataType == TypeCode.Object) && var.EvaluateAsExpression == false)
                    {
                        cmbXmlVariable.Items.Add(var.QualifiedName);
                    }

                    lvVariables.Items.Add(lvi);
                }

                cmbXmlVariable.SelectedItem = -1;
                if (!string.IsNullOrEmpty(TaskControl.XmlVariable) && UIHelper.TaskHost.Variables.Contains(TaskControl.XmlVariable) && cmbXmlVariable.Items.Count > 0)
                {
                    Variable xv = UIHelper.TaskHost.Variables[TaskControl.XmlVariable];
                    if ((xv.DataType == TypeCode.Object || xv.DataType == TypeCode.String) && xv.Namespace == "User" && xv.EvaluateAsExpression == false)
                        cmbXmlVariable.SelectedItem = xv.QualifiedName;
                }

                lbSelectedVariables.Items.AddRange(selected.ToArray());
            }
            finally
            {
                lbSelectedVariables.ResumeLayout(true);
                lvVariables.ResumeLayout(true);
                cmbXmlVariable.ResumeLayout(true);
                Loading = false;
            }

        }

        private void lvVariables_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (Loading || e.Item == null || e.Item.Tag == null)
                return;

            string qualifiedName = e.Item.Tag.ToString();

            if (e.Item.Checked)
            {
                if (!lbSelectedVariables.Items.Contains(qualifiedName))
                    lbSelectedVariables.Items.Add(qualifiedName);

                if (cmbXmlVariable.SelectedIndex >= 0 && cmbXmlVariable.SelectedItem != null && cmbXmlVariable.SelectedItem.ToString() == qualifiedName)
                    cmbXmlVariable.SelectedIndex = -1;
            }
            else
            {
                if (lbSelectedVariables.Items.Contains(qualifiedName))
                    lbSelectedVariables.Items.Remove(qualifiedName);
            }
        }

        private void cmbXmlVariable_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Loading)
                return;

            if (!Loading && cmbXmlVariable.SelectedIndex >= 0 && cmbXmlVariable.SelectedItem != null)
            {
                string xmlVar = cmbXmlVariable.SelectedItem.ToString();

                var items = lvVariables.Items.Find(xmlVar, false);
                if (items != null)
                {
                    foreach (ListViewItem item in items)
                    {
                        item.Checked = false;
                    }
                }

            }
        }

        private void MoveSelectedItem(ListBox lbSelectedItems, bool moveUp)
        {
            if (lbSelectedItems == null)
                return;

            int idx = lbSelectedItems.SelectedIndex;
            if (moveUp && idx > 0)
            {
                lbSelectedItems.SuspendLayout();

                string var = lbSelectedItems.SelectedItem as string;
                lbSelectedItems.Items.RemoveAt(idx);
                lbSelectedItems.Items.Insert(idx - 1, var);
                lbSelectedItems.SelectedIndex = idx - 1;


                lbSelectedItems.ResumeLayout(true);
            }
            else if (moveUp == false && idx >= 0 && idx < lbSelectedItems.Items.Count - 1)
            {
                lbSelectedItems.SuspendLayout();
                string var = lbSelectedItems.SelectedItem as string;
                lbSelectedItems.Items.RemoveAt(idx);
                lbSelectedItems.Items.Insert(idx + 1, var);
                lbSelectedItems.SelectedIndex = idx + 1;

                lbSelectedItems.ResumeLayout(true);
            }
        }


        private void btnOk_Click(object sender, EventArgs e)
        {
            TaskControl.XmlVariable = cmbXmlVariable.SelectedIndex >= 0 ? cmbXmlVariable.SelectedItem.ToString() : string.Empty;
            _taskProperties.PersistProperties();
            List<string> vars = new List<string>(lbSelectedVariables.Items.Count);
            foreach (var v in lbSelectedVariables.Items)
            {
                vars.Add(v.ToString());
            }

            TaskControl.VariablesToExport = vars.Count > 0 ? string.Join(";", vars.ToArray()) : string.Empty;
        }

        private void btnMoveUp_Click(object sender, EventArgs e)
        {
            MoveSelectedItem(lbSelectedVariables, true);
        }

        private void btnMoveDown_Click(object sender, EventArgs e)
        {
            MoveSelectedItem(lbSelectedVariables, false);
        }

        private void removeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (lbSelectedVariables.SelectedIndex >= 0 && lbSelectedVariables.SelectedItem != null)
            {
                string var = lbSelectedVariables.SelectedItem.ToString();
                var lvis = lvVariables.Items.Find(var, false);
                if (lvis != null && lvis.Length > 0)
                {
                    foreach (ListViewItem lvi in lvis)
                    {
                        lvi.Checked = false;
                    }
                }
                    
            }
        }

        private void cmsVariables_Opening(object sender, CancelEventArgs e)
        {
            e.Cancel = lbSelectedVariables.Items.Count == 0;

            removeToolStripMenuItem.Enabled = lbSelectedVariables.Items.Count > 0;
        }

        private void cmsAvailableVariables_Opening(object sender, CancelEventArgs e)
        {
            e.Cancel = lvVariables.CheckedItems.Count == 0;
        }

        private void deselectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (lvVariables.Items.Count > 0 && lvVariables.CheckedItems.Count > 0)
            {
                while (lvVariables.CheckedItems.Count > 0)
                {
                    lvVariables.CheckedItems[0].Checked = false;
                }
            }
        }

    }
}
