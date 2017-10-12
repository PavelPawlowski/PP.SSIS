using System.Windows.Forms;
using System;
using System.Collections.Generic;

#if SQL2008 || SQL2008R2 || SQL2012 || SQL2014 || SQL2017
using IDTSOutput = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSOutput100;
using IDTSCustomProperty = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSCustomProperty100;
using IDTSOutputColumn = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSOutputColumn100;
using IDTSInput = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSInput100;
using IDTSInputColumn = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSInputColumn100;
using IDTSVirtualInput = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSVirtualInput100;
using IDTSComponentMetaData = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSComponentMetaData100;
using IDTSDesigntimeComponent = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSDesigntimeComponent100;
using IDTSVirtualInputColumn = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSVirtualInputColumn100;
#endif

namespace PP.SSIS.DataFlow.UI
{
    public partial class InputColumnsUIEditorForm : Form
    {
        private bool initializing = false;
        public InputColumnsUIEditorForm()
        {
            InitializeComponent();
        }

        public List<int> GetInputColumnsLineages()
        {
            List<int> colLineages = new List<int>(lbSelectedItems.Items.Count);

            foreach (var item in lbSelectedItems.Items)
            {
                FormInputColumn fic = item as FormInputColumn;
                if (fic != null)
                    colLineages.Add(fic.LineageID);
            }

            return colLineages;                
        }

        internal void InitializeForm(IUIHelper uiHelper, List<int> inputColumnsLineages)
        {
            var inputCols = uiHelper.GetFormInputColumns();

            try
            {
                initializing = true;
                clbInputColumns.SuspendLayout();
                clbInputColumns.Items.Clear();

                lbSelectedItems.SuspendLayout();
                lbSelectedItems.Items.Clear();

                if (clbInputColumns != null)
                    inputCols.ForEach(ic => clbInputColumns.Items.Add(ic, inputColumnsLineages.Contains(ic.LineageID)));


                foreach (int lineage in inputColumnsLineages)
                {
                    var inputCol = inputCols.Find(fic => fic.LineageID == lineage);
                    if (inputCol != null)
                        lbSelectedItems.Items.Add(inputCol);
                }
            }
            finally
            {
                initializing = false;
                clbInputColumns.ResumeLayout();
                lbSelectedItems.ResumeLayout();
            }
        }

        private void clbInputColumns_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (initializing)
                return;
            FormInputColumn fic = clbInputColumns.Items[e.Index] as FormInputColumn;
            if (fic != null && e.CurrentValue != e.NewValue)
            {
                if (e.NewValue == CheckState.Checked)
                {
                    if (!lbSelectedItems.Items.Contains(fic))
                        lbSelectedItems.Items.Add(fic);
                }
                else
                {
                    if (lbSelectedItems.Items.Contains(fic))
                        lbSelectedItems.Items.Remove(fic);
                }
            }
        }

        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < clbInputColumns.Items.Count; i++)
            {
                clbInputColumns.SetItemChecked(i, true);
            }
        }

        private void deselectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < clbInputColumns.Items.Count; i++)
            {
                clbInputColumns.SetItemChecked(i, false);
            }
        }

        private void btnUp_Click(object sender, EventArgs e)
        {
            int idx = lbSelectedItems.SelectedIndex;
            if (idx > 0)
            {
                try
                {
                    lbSelectedItems.SuspendLayout();

                    FormInputColumn icol = lbSelectedItems.SelectedItem as FormInputColumn;
                    FormInputColumn prevCol = lbSelectedItems.Items[idx - 1] as FormInputColumn;

                    lbSelectedItems.Items.RemoveAt(idx);
                    lbSelectedItems.Items.Insert(idx - 1, icol);
                    lbSelectedItems.SelectedIndex = idx - 1;
                }
                finally
                {
                    lbSelectedItems.ResumeLayout(true);
                }
            }
        }

        private void btnDown_Click(object sender, EventArgs e)
        {
            int idx = lbSelectedItems.SelectedIndex;
            if (idx >= 0 && idx < lbSelectedItems.Items.Count - 1)
            {
                try
                {
                    lbSelectedItems.SuspendLayout();
                    FormInputColumn icol = lbSelectedItems.SelectedItem as FormInputColumn;
                    FormInputColumn nextCol = lbSelectedItems.Items[idx + 1] as FormInputColumn;

                    lbSelectedItems.Items.RemoveAt(idx);
                    lbSelectedItems.Items.Insert(idx + 1, icol);
                    lbSelectedItems.SelectedIndex = idx + 1;
                }
                finally
                {
                    lbSelectedItems.ResumeLayout(true);
                }
            }

        }

        private void lbSelectedItems_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Modifiers == Keys.Control)
            {
                if (e.KeyCode == Keys.Up)
                {
                    btnUp.PerformClick();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Down)
                {
                    btnDown.PerformClick();
                    e.Handled = true;
                }
            }

        }

        private void cmsSelected_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = lbSelectedItems.Items.Count == 0 || lbSelectedItems.SelectedIndex < 0;

            bool enableMove = true;
            if (lbSelectedItems.Items.Count < 2)
                enableMove = false;

            moveUpToolStripMenuItem.Enabled = enableMove;
            moveDownToolStripMenuItem.Enabled = enableMove;

        }

        private void moveUpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btnUp.PerformClick();
        }

        private void moveDownToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btnDown.PerformClick();
        }

        private void removeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormInputColumn icol = lbSelectedItems.SelectedItem as FormInputColumn;

            if (icol != null)
            {

                for (int i = 0; i < clbInputColumns.Items.Count; i++)
                {
                    FormInputColumn fic = clbInputColumns.Items[i] as FormInputColumn;
                    if (fic != null && fic.LineageID == icol.LineageID)
                    {
                        clbInputColumns.SetItemChecked(i, false);
                        break;
                    }
                }
            }
        }
    }
}
