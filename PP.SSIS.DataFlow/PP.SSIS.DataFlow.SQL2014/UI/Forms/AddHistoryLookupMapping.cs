using PP.SSIS.DataFlow.UI.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PP.SSIS.DataFlow.UI
{
    public partial class AddHistoryLookupMapping : Form
    {
        public AddHistoryLookupMapping()
        {
            InitializeComponent();
        }

        public AddHistoryLookupMapping(List<ConvertMetadataInputColumn> inputColumns, List<ConvertMetadataInputColumn> lookupColumns)
            : this()
        {
            InputColumns = inputColumns;
            LookupColumns = lookupColumns;
        }

        public List<ConvertMetadataInputColumn> InputColumns { get; set; }
        public List<ConvertMetadataInputColumn> LookupColumns { get; set; }

        public ConvertMetadataInputColumn SelectedInputColumn { get; private set; }

        public ConvertMetadataInputColumn SelectedLookupColumn { get; private set; }

        private void AddHistoryLookupMapping_Load(object sender, EventArgs e)
        {
            cmbDataColumn.DisplayMember = "DisplayName";
            cmbLookupColumn.DisplayMember = "DisplayName";
            cmbDataColumn.DataSource = InputColumns;

        }

        private void cmbDataColumn_SelectedIndexChanged(object sender, EventArgs e)
        {
            ConvertMetadataInputColumn ic = cmbDataColumn.SelectedItem as ConvertMetadataInputColumn;
            if (ic != null)
            {
                var matchedItems =
                    LookupColumns.FindAll(c =>
                        c.VirtualColumn.DataType == ic.VirtualColumn.DataType &&
                        c.VirtualColumn.Length == ic.VirtualColumn.Length &&
                        c.VirtualColumn.Precision == ic.VirtualColumn.Precision &&
                        c.VirtualColumn.Scale == ic.VirtualColumn.Scale
                    );
                cmbLookupColumn.DataSource = matchedItems;
            }

            btnOk.Enabled = cmbDataColumn.SelectedIndex >= 0 && cmbLookupColumn.SelectedIndex >= 0;
        }

        private void cmbLookupColumn_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnOk.Enabled = cmbDataColumn.SelectedIndex >= 0 && cmbLookupColumn.SelectedIndex >= 0;
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            SelectedInputColumn = cmbDataColumn.SelectedIndex >= 0 ? cmbDataColumn.Items[cmbDataColumn.SelectedIndex] as ConvertMetadataInputColumn : null;
            SelectedLookupColumn = cmbLookupColumn.SelectedIndex >= 0 ? cmbLookupColumn.Items[cmbLookupColumn.SelectedIndex] as ConvertMetadataInputColumn : null;
        }
    }
}
