namespace PP.SSIS.DataFlow.UI
{
    partial class HistoryLookupTransformationForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.ListViewItem listViewItem1 = new System.Windows.Forms.ListViewItem(new string[] {
            "Col1",
            "Col1_Alias"}, -1);
            System.Windows.Forms.ListViewItem listViewItem2 = new System.Windows.Forms.ListViewItem(new string[] {
            "Col2",
            "Col2"}, -1);
            System.Windows.Forms.ListViewItem listViewItem3 = new System.Windows.Forms.ListViewItem(new string[] {
            "Col3",
            ""}, -1);
            System.Windows.Forms.ListViewItem listViewItem4 = new System.Windows.Forms.ListViewItem(new string[] {
            "Col1",
            "LookupKey1"}, -1);
            System.Windows.Forms.ListViewItem listViewItem5 = new System.Windows.Forms.ListViewItem(new string[] {
            "Col2",
            "LookupKey2"}, -1);
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HistoryLookupTransformationForm));
            this.panel1 = new System.Windows.Forms.Panel();
            this.pnlError = new System.Windows.Forms.Panel();
            this.lblError = new System.Windows.Forms.Label();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            this.tabComponentProperties = new System.Windows.Forms.TabPage();
            this.propComponent = new System.Windows.Forms.PropertyGrid();
            this.tabInputColumns = new System.Windows.Forms.TabPage();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.panel5 = new System.Windows.Forms.Panel();
            this.lvOutputColumns = new PP.SSIS.DataFlow.UI.ListViewEx();
            this.outputColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.outputAlias = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.panel6 = new System.Windows.Forms.Panel();
            this.label2 = new System.Windows.Forms.Label();
            this.panel3 = new System.Windows.Forms.Panel();
            this.lvMappings = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.tableLayoutPanel5 = new System.Windows.Forms.TableLayoutPanel();
            this.panel10 = new System.Windows.Forms.Panel();
            this.btnAddMapping = new System.Windows.Forms.Button();
            this.panel11 = new System.Windows.Forms.Panel();
            this.btnRemoveMapping = new System.Windows.Forms.Button();
            this.panel7 = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.txtOutputAlias = new System.Windows.Forms.TextBox();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.errorProvider = new System.Windows.Forms.ErrorProvider(this.components);
            this.panel1.SuspendLayout();
            this.pnlError.SuspendLayout();
            this.tabComponentProperties.SuspendLayout();
            this.tabInputColumns.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.panel5.SuspendLayout();
            this.panel6.SuspendLayout();
            this.panel3.SuspendLayout();
            this.tableLayoutPanel5.SuspendLayout();
            this.panel10.SuspendLayout();
            this.panel11.SuspendLayout();
            this.panel7.SuspendLayout();
            this.tabControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider)).BeginInit();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.pnlError);
            this.panel1.Controls.Add(this.btnCancel);
            this.panel1.Controls.Add(this.btnOk);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 406);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(624, 35);
            this.panel1.TabIndex = 0;
            // 
            // pnlError
            // 
            this.pnlError.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlError.AutoScroll = true;
            this.pnlError.Controls.Add(this.lblError);
            this.errorProvider.SetIconAlignment(this.pnlError, System.Windows.Forms.ErrorIconAlignment.MiddleLeft);
            this.pnlError.Location = new System.Drawing.Point(28, 0);
            this.pnlError.Name = "pnlError";
            this.pnlError.Size = new System.Drawing.Size(424, 35);
            this.pnlError.TabIndex = 2;
            // 
            // lblError
            // 
            this.lblError.AutoSize = true;
            this.lblError.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblError.Location = new System.Drawing.Point(0, 0);
            this.lblError.Name = "lblError";
            this.lblError.Size = new System.Drawing.Size(13, 13);
            this.lblError.TabIndex = 0;
            this.lblError.Text = "  ";
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(539, 6);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 1;
            this.btnCancel.Text = "&Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // btnOk
            // 
            this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOk.Location = new System.Drawing.Point(458, 6);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(75, 23);
            this.btnOk.TabIndex = 0;
            this.btnOk.Text = "O&K";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // tabComponentProperties
            // 
            this.tabComponentProperties.Controls.Add(this.propComponent);
            this.tabComponentProperties.Location = new System.Drawing.Point(4, 22);
            this.tabComponentProperties.Name = "tabComponentProperties";
            this.tabComponentProperties.Padding = new System.Windows.Forms.Padding(3);
            this.tabComponentProperties.Size = new System.Drawing.Size(616, 380);
            this.tabComponentProperties.TabIndex = 2;
            this.tabComponentProperties.Text = "Component Properties";
            this.tabComponentProperties.UseVisualStyleBackColor = true;
            // 
            // propComponent
            // 
            this.propComponent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.propComponent.Location = new System.Drawing.Point(3, 3);
            this.propComponent.Name = "propComponent";
            this.propComponent.Size = new System.Drawing.Size(610, 374);
            this.propComponent.TabIndex = 1;
            // 
            // tabInputColumns
            // 
            this.tabInputColumns.Controls.Add(this.tableLayoutPanel2);
            this.tabInputColumns.Controls.Add(this.txtOutputAlias);
            this.tabInputColumns.Location = new System.Drawing.Point(4, 22);
            this.tabInputColumns.Name = "tabInputColumns";
            this.tabInputColumns.Padding = new System.Windows.Forms.Padding(3);
            this.tabInputColumns.Size = new System.Drawing.Size(616, 380);
            this.tabInputColumns.TabIndex = 0;
            this.tabInputColumns.Text = "Column Mappings && Output Columns";
            this.tabInputColumns.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 2;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.Controls.Add(this.panel5, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.panel3, 0, 0);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(3, 3);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 1;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(610, 374);
            this.tableLayoutPanel2.TabIndex = 2;
            // 
            // panel5
            // 
            this.panel5.Controls.Add(this.lvOutputColumns);
            this.panel5.Controls.Add(this.panel6);
            this.panel5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel5.Location = new System.Drawing.Point(308, 3);
            this.panel5.Name = "panel5";
            this.panel5.Size = new System.Drawing.Size(299, 368);
            this.panel5.TabIndex = 5;
            // 
            // lvOutputColumns
            // 
            this.lvOutputColumns.AllowColumnReorder = true;
            this.lvOutputColumns.CheckBoxes = true;
            this.lvOutputColumns.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.outputColumn,
            this.outputAlias});
            this.lvOutputColumns.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvOutputColumns.DoubleClickActivation = false;
            this.lvOutputColumns.FullRowSelect = true;
            listViewItem1.Checked = true;
            listViewItem1.StateImageIndex = 1;
            listViewItem2.Checked = true;
            listViewItem2.StateImageIndex = 1;
            listViewItem3.StateImageIndex = 0;
            this.lvOutputColumns.Items.AddRange(new System.Windows.Forms.ListViewItem[] {
            listViewItem1,
            listViewItem2,
            listViewItem3});
            this.lvOutputColumns.Location = new System.Drawing.Point(0, 25);
            this.lvOutputColumns.Name = "lvOutputColumns";
            this.lvOutputColumns.Size = new System.Drawing.Size(299, 343);
            this.lvOutputColumns.TabIndex = 6;
            this.lvOutputColumns.UseCompatibleStateImageBehavior = false;
            this.lvOutputColumns.View = System.Windows.Forms.View.Details;
            this.lvOutputColumns.SubItemClicked += new PP.SSIS.DataFlow.UI.SubItemEventHandler(this.lvOutputColumns_SubItemClicked);
            this.lvOutputColumns.SubItemEndEditing += new PP.SSIS.DataFlow.UI.SubItemEndEditingEventHandler(this.lvOutputColumns_SubItemEndEditing);
            this.lvOutputColumns.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler(this.lvOutputColumns_ItemChecked);
            // 
            // outputColumn
            // 
            this.outputColumn.Text = "Lookup Column";
            this.outputColumn.Width = 140;
            // 
            // outputAlias
            // 
            this.outputAlias.Text = "Output Alias";
            this.outputAlias.Width = 140;
            // 
            // panel6
            // 
            this.panel6.Controls.Add(this.label2);
            this.panel6.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel6.Location = new System.Drawing.Point(0, 0);
            this.panel6.Name = "panel6";
            this.panel6.Size = new System.Drawing.Size(299, 25);
            this.panel6.TabIndex = 5;
            // 
            // label2
            // 
            this.label2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.label2.Location = new System.Drawing.Point(0, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(299, 25);
            this.label2.TabIndex = 1;
            this.label2.Text = "Output Columns";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // panel3
            // 
            this.panel3.Controls.Add(this.lvMappings);
            this.panel3.Controls.Add(this.tableLayoutPanel5);
            this.panel3.Controls.Add(this.panel7);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel3.Location = new System.Drawing.Point(3, 3);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(299, 368);
            this.panel3.TabIndex = 6;
            // 
            // lvMappings
            // 
            this.lvMappings.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2});
            this.lvMappings.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvMappings.FullRowSelect = true;
            this.lvMappings.HideSelection = false;
            listViewItem4.Checked = true;
            listViewItem4.StateImageIndex = 1;
            listViewItem5.Checked = true;
            listViewItem5.StateImageIndex = 1;
            this.lvMappings.Items.AddRange(new System.Windows.Forms.ListViewItem[] {
            listViewItem4,
            listViewItem5});
            this.lvMappings.Location = new System.Drawing.Point(0, 25);
            this.lvMappings.MultiSelect = false;
            this.lvMappings.Name = "lvMappings";
            this.lvMappings.Size = new System.Drawing.Size(299, 315);
            this.lvMappings.TabIndex = 7;
            this.lvMappings.UseCompatibleStateImageBehavior = false;
            this.lvMappings.View = System.Windows.Forms.View.Details;
            this.lvMappings.SelectedIndexChanged += new System.EventHandler(this.lvMappings_SelectedIndexChanged);
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Data Input";
            this.columnHeader1.Width = 146;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Lookup Input";
            this.columnHeader2.Width = 146;
            // 
            // tableLayoutPanel5
            // 
            this.tableLayoutPanel5.ColumnCount = 2;
            this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel5.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel5.Controls.Add(this.panel10, 0, 0);
            this.tableLayoutPanel5.Controls.Add(this.panel11, 1, 0);
            this.tableLayoutPanel5.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.tableLayoutPanel5.Location = new System.Drawing.Point(0, 340);
            this.tableLayoutPanel5.Name = "tableLayoutPanel5";
            this.tableLayoutPanel5.RowCount = 1;
            this.tableLayoutPanel5.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel5.Size = new System.Drawing.Size(299, 28);
            this.tableLayoutPanel5.TabIndex = 8;
            // 
            // panel10
            // 
            this.panel10.Controls.Add(this.btnAddMapping);
            this.panel10.Location = new System.Drawing.Point(3, 3);
            this.panel10.Name = "panel10";
            this.panel10.Size = new System.Drawing.Size(142, 22);
            this.panel10.TabIndex = 0;
            // 
            // btnAddMapping
            // 
            this.btnAddMapping.Location = new System.Drawing.Point(20, 0);
            this.btnAddMapping.Name = "btnAddMapping";
            this.btnAddMapping.Size = new System.Drawing.Size(100, 22);
            this.btnAddMapping.TabIndex = 0;
            this.btnAddMapping.Text = "&Add Mapping";
            this.btnAddMapping.UseVisualStyleBackColor = true;
            this.btnAddMapping.Click += new System.EventHandler(this.btnAddMapping_Click);
            // 
            // panel11
            // 
            this.panel11.Controls.Add(this.btnRemoveMapping);
            this.panel11.Location = new System.Drawing.Point(152, 3);
            this.panel11.Name = "panel11";
            this.panel11.Size = new System.Drawing.Size(142, 22);
            this.panel11.TabIndex = 1;
            // 
            // btnRemoveMapping
            // 
            this.btnRemoveMapping.Location = new System.Drawing.Point(26, 0);
            this.btnRemoveMapping.Name = "btnRemoveMapping";
            this.btnRemoveMapping.Size = new System.Drawing.Size(101, 22);
            this.btnRemoveMapping.TabIndex = 1;
            this.btnRemoveMapping.Text = "&Remove Mapping";
            this.btnRemoveMapping.UseVisualStyleBackColor = true;
            this.btnRemoveMapping.Click += new System.EventHandler(this.btnRemoveMapping_Click);
            // 
            // panel7
            // 
            this.panel7.Controls.Add(this.label1);
            this.panel7.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel7.Location = new System.Drawing.Point(0, 0);
            this.panel7.Name = "panel7";
            this.panel7.Size = new System.Drawing.Size(299, 25);
            this.panel7.TabIndex = 5;
            // 
            // label1
            // 
            this.label1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.label1.Location = new System.Drawing.Point(0, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(299, 25);
            this.label1.TabIndex = 1;
            this.label1.Text = "Columns Mappings";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // txtOutputAlias
            // 
            this.txtOutputAlias.Location = new System.Drawing.Point(222, 369);
            this.txtOutputAlias.Name = "txtOutputAlias";
            this.txtOutputAlias.Size = new System.Drawing.Size(100, 20);
            this.txtOutputAlias.TabIndex = 1;
            this.txtOutputAlias.Visible = false;
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabInputColumns);
            this.tabControl1.Controls.Add(this.tabComponentProperties);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(624, 406);
            this.tabControl1.TabIndex = 1;
            // 
            // errorProvider
            // 
            this.errorProvider.ContainerControl = this;
            // 
            // HistoryLookupTransformationForm
            // 
            this.AcceptButton = this.btnOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(624, 441);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.panel1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(320, 240);
            this.Name = "HistoryLookupTransformationForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "History Lookup Transformation";
            this.Load += new System.EventHandler(this.ColumnsToXmlTransformationForm_Load);
            this.panel1.ResumeLayout(false);
            this.pnlError.ResumeLayout(false);
            this.pnlError.PerformLayout();
            this.tabComponentProperties.ResumeLayout(false);
            this.tabInputColumns.ResumeLayout(false);
            this.tabInputColumns.PerformLayout();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.panel5.ResumeLayout(false);
            this.panel6.ResumeLayout(false);
            this.panel3.ResumeLayout(false);
            this.tableLayoutPanel5.ResumeLayout(false);
            this.panel10.ResumeLayout(false);
            this.panel11.ResumeLayout(false);
            this.panel7.ResumeLayout(false);
            this.tabControl1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.TabPage tabComponentProperties;
        private System.Windows.Forms.PropertyGrid propComponent;
        private System.Windows.Forms.TabPage tabInputColumns;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TextBox txtOutputAlias;
        private System.Windows.Forms.ErrorProvider errorProvider;
        private System.Windows.Forms.Panel pnlError;
        private System.Windows.Forms.Label lblError;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.Panel panel5;
        private ListViewEx lvOutputColumns;
        private System.Windows.Forms.ColumnHeader outputColumn;
        private System.Windows.Forms.ColumnHeader outputAlias;
        private System.Windows.Forms.Panel panel6;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.ListView lvMappings;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel5;
        private System.Windows.Forms.Panel panel10;
        private System.Windows.Forms.Button btnAddMapping;
        private System.Windows.Forms.Panel panel11;
        private System.Windows.Forms.Button btnRemoveMapping;
        private System.Windows.Forms.Panel panel7;
        private System.Windows.Forms.Label label1;
    }
}