// <copyright file="DataFlowUI.cs" company="Pavel Pawlowski">
// Copyright (c) 2014, 2015 All Right Reserved
//
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY 
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// </copyright>
// <author>Pavel Pawlowski</author>
// <summary>Contains abstract implementaion of the DataFlowUI Helper class</summary>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Microsoft.SqlServer.Dts.Pipeline;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;
using Microsoft.SqlServer.Dts.Runtime.Wrapper;
using System.Xml.Linq;
using System.Globalization;
using System.Security.Cryptography;
using System.IO;
using PP.SSIS.DataFlow.Properties;
using System.Windows.Forms;
using Microsoft.SqlServer.Dts.Runtime;

    using IDTSOutput = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSOutput100;
    using IDTSCustomProperty = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSCustomProperty100;
    using IDTSOutputColumn = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSOutputColumn100;
    using IDTSInput = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSInput100;
    using IDTSInputColumn = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSInputColumn100;
    using IDTSVirtualInput = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSVirtualInput100;
    using IDTSComponentMetaData = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSComponentMetaData100;
    using IDTSDesigntimeComponent = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSDesigntimeComponent100;
    using IDTSVirtualInputColumn = Microsoft.SqlServer.Dts.Pipeline.Wrapper.IDTSVirtualInputColumn100;

namespace PP.SSIS.DataFlow.UI
{
    /// <summary>
    /// Abstract helpre class for UI request handling for particular DataFlowComponent
    /// </summary>
    /// <typeparam name="TUIForm">Form to be used to Edit the DataFlow Components. Form must implemnt IUIForm interface</typeparam>
    public abstract class DataFlowUI<TUIForm> : Microsoft.SqlServer.Dts.Pipeline.Design.IDtsComponentUI, IUIHelper where TUIForm : Form, IUIForm, new() 
    {

        IDTSComponentMetaData componentMetadata;
        IServiceProvider serviceProvider;
        IDTSDesigntimeComponent designTimeComponent;
        List<IDTSVirtualInput> virtualInputs = new List<IDTSVirtualInput>();
        Variables variables;
        IDTSInput input;
        IDTSInput input2;
        IDTSOutput output;
        Connections connections;

        /// <summary>
        /// Gets ComponentMetadata of the DataFlow Component
        /// </summary>
        public IDTSComponentMetaData ComponentMetadata
        {
            get { return componentMetadata; }
        }

        /// <summary>
        /// Gets access to the Variables of the DataFlow Component
        /// </summary>
        public Variables Variables
        {
            get { return variables; }
        }

        /// <summary>
        /// Gets access to the Connections of the DataFlow Component
        /// </summary>
        public Connections Connections
        {
            get { return connections; }
        }

        /// <summary>
        /// Gets access to the DesignTimeComponent of the DataFlow Component
        /// </summary>
        public IDTSDesigntimeComponent DesignTimeComponent
        {
            get { return designTimeComponent; }
        }

        public IDTSOutput Output
        {
            get
            {
                return ComponentMetadata != null && ComponentMetadata.OutputCollection.Count > 0 ? ComponentMetadata.OutputCollection[0] : null;
                //if (output == null && ComponentMetadata != null && ComponentMetadata.OutputCollection.Count > 0)
                //{
                //    output = ComponentMetadata.OutputCollection[0];
                //}
                //return output;
            }
        }

        /// <summary>
        /// Gets the Default Input of the DataFlow Component (Index=0)
        /// </summary>
        public IDTSInput Input
        {
            get
            {
                return ComponentMetadata != null && componentMetadata.InputCollection.Count > 0 ? ComponentMetadata.InputCollection[0] : null;
                //if (input == null && ComponentMetadata != null)
                //{
                //    input = ComponentMetadata.InputCollection[0];
                //}
                //return input;
            }
        }

        /// <summary>
        /// Gets the Secont Input of the DataFlow Component (Index=1)
        /// </summary>
        public IDTSInput Input2
        {
            get
            {
                return ComponentMetadata != null && componentMetadata.InputCollection.Count > 1 ? ComponentMetadata.InputCollection[1] : null;
                //if (input2 == null && ComponentMetadata != null)
                //{
                //    if (ComponentMetadata.InputCollection.Count > 1)
                //        input = ComponentMetadata.InputCollection[1];
                //}
                //return input;
            }
        }

        public IDTSOutput ErrorOutput
        {
            get
            {
                if (ComponentMetadata != null)
                {
                    foreach (IDTSOutput output in ComponentMetadata.OutputCollection)
                    {
                        if (output.IsErrorOut)
                            return output;
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Gets VirtualInput of the Default Input (Index=0)
        /// </summary>
        public IDTSVirtualInput VirtualInput
        {
            get
            {
                while (virtualInputs.Count < ComponentMetadata.InputCollection.Count)
                    virtualInputs.Add(null);

                if (virtualInputs[0] == null && Input != null)
                {
                    virtualInputs[0] = input.GetVirtualInput();
                }
                return virtualInputs[0];
            }
        }

        /// <summary>
        /// Gets VirtualInput of the Default Input (Index=0)
        /// </summary>
        public IDTSVirtualInput VirtualInput2
        {
            get
            {
                while (virtualInputs.Count < ComponentMetadata.InputCollection.Count)
                    virtualInputs.Add(null);

                if (virtualInputs[1] == null && Input2 != null)
                {
                    virtualInputs[1] = Input2.GetVirtualInput();
                }
                return virtualInputs[1];
            }
        }

        /// <summary>
        /// Gets Input of the DataFlow Component at the Index position int he Inputs Collection
        /// </summary>
        /// <param name="index">Index of the Input to retrieve</param>
        /// <returns>IDTSInput</returns>
        public IDTSInput GetInput(int index)
        {
            if (index < ComponentMetadata.InputCollection.Count)
                return ComponentMetadata.InputCollection[index];
            else
                return null;
        }

        /// <summary>
        /// Gets VirtulaInput of the Input of the DataFlow Component at the Index position in the Inputs Collection.
        /// It ensures that the same IDTSVirtualInput object is retured during each call and ensures proper GUI functionality
        /// </summary>
        /// <param name="index">Index of the Input to retrive VistualInput</param>
        /// <returns>IDTSVirtualINput of Input at sprovided Index position</returns>
        public IDTSVirtualInput GetVirtualInput(int index)
        {
            while (virtualInputs.Count < ComponentMetadata.InputCollection.Count)
                virtualInputs.Add(null);

            IDTSInput input = GetInput(index);
            if (virtualInputs[index] == null && input != null)
            {
                virtualInputs[index] = input.GetVirtualInput();
            }
            return virtualInputs[index];
        }

        /// <summary>
        /// Indiates whether the GUI form is being loaded (initalized)
        /// </summary>
        public bool Loading { get; private set; }

        /// <summary>
        /// Gets InputColumns of the default Input (Index=0) encapsulated in the FormInputColumn class
        /// </summary>
        /// <returns>KList of FomrInputColumn</returns>
        public List<FormInputColumn> GetFormInputColumns()
        {
            return GetFormInputColumns(0);
        }

        /// <summary>
        /// Gets InputColumns of Input at selected inde of InputCollection encapsulated in the FormInputColumn class
        /// </summary>
        /// <param name="index">Index of the Input to get InputColumns</param>
        /// <returns>List of FormInputColumn</returns>
        public virtual List<FormInputColumn> GetFormInputColumns(int index)
        {
            List<FormInputColumn> inputCols = new List<FormInputColumn>();

            IDTSInput input = GetInput(index);
            IDTSVirtualInput vInput = GetVirtualInput(index);


            if (input != null && VirtualInput != null)
            {
                for (int i = 0; i < VirtualInput.VirtualInputColumnCollection.Count; i++)
                //foreach (IDTSVirtualInputColumn vcol in VirtualInput.VirtualInputColumnCollection)
                {
                    IDTSVirtualInputColumn vcol = VirtualInput.VirtualInputColumnCollection[i];

                    FormInputColumn icol = new FormInputColumn(vcol, i);

                    if (vcol.UsageType != DTSUsageType.UT_IGNORED)
                    {
                        IDTSInputColumn inputCol = input.InputColumnCollection.GetInputColumnByLineageID(vcol.LineageID);
                        icol.DTSInputColumn = inputCol;
                    }

                    bool isValidForInput = CheckColumnForInputValidity(vInput, input, icol);

                    if (isValidForInput)
                        inputCols.Add(icol);
                }

                inputCols.Sort((a, b) => a.Name.CompareTo(b.Name));
                

                for (int i = 0; i < inputCols.Count; i++)
                {
                    if (i >= 0 && i < inputCols.Count - 1 && inputCols[i].Name == inputCols[i + 1].Name)
                        inputCols[i].DisplayName = string.Format("{0}.{1}", inputCols[i].SourceComponent, inputCols[i].Name);
                    else if (i > 0 && i < inputCols.Count && inputCols[i].Name == inputCols[i - 1].Name)
                        inputCols[i].DisplayName = string.Format("{0}.{1}", inputCols[i].SourceComponent, inputCols[i].Name);
                }

                inputCols.Sort((a, b) => a.Index.CompareTo(b.Index));
            }

            return inputCols;
        }

        public virtual bool CheckColumnForInputValidity(IDTSVirtualInput vInput, IDTSInput input, FormInputColumn icol)
        {
            return CheckColumnForInputValidity(icol != null ? icol.DTSVirtualColumn : null);
        }
        public virtual bool CheckColumnForInputValidity(IDTSVirtualInputColumn vInputColumn)
        {
            return true;
        }


        /// <summary>
        /// Gets List of OutputColumns of Particular OuputID encapsulated in the FormOutputColumn class
        /// </summary>
        /// <param name="outputID">ID of the Output to get OutputColumns</param>
        /// <param name="creator">OutputColumnCreator to be used for creation of the FormInputColumn instances</param>
        /// <param name="changedHandler">PropertyChangedEventHanled to be associated with the newly created FormInputColumn objects</param>
        /// <returns>List of FomrInputColumn</returns>
        public virtual List<FormOutputColumn> GetFormOutputColumns(int outputID, OutputColumnCreator creator, PropertyChangedEventHandler changedHandler)
        {
            List<FormOutputColumn> outputCols = new List<FormOutputColumn>();
            IDTSOutput output = ComponentMetadata.OutputCollection.GetObjectByID(outputID);
            if (output != null && creator != null)
            {
                for (int i = 0; i < output.OutputColumnCollection.Count; i++)
                {
                    IDTSOutputColumn col = output.OutputColumnCollection[i];

                    FormOutputColumn oCol = creator(col);
                    oCol.UIHelper = this;
                    oCol.OutputID = outputID;

                    oCol.Index = i;

                    if (changedHandler != null)
                        oCol.PropertyChanged += changedHandler;

                    outputCols.Add(oCol);
                }
            }

            return outputCols;
        }

        /// <summary>
        /// Gets List of DataFlow component Outputs encapsulated in the FormOutputClass
        /// </summary>
        /// <param name="changeHandler">PropertyChangedEventHandler to be associated with the newly created FormOutput class</param>
        /// <returns>List of FormOutput</returns>
        public List<FormOutput> GetFormOutputs(PropertyChangedEventHandler changeHandler)
        {
            List<FormOutput> outputs = new List<FormOutput>();
            if (ComponentMetadata != null)
            {
                for (int i = 0; i < ComponentMetadata.OutputCollection.Count; i++)
                {
                    IDTSOutput o = ComponentMetadata.OutputCollection[i];
                    FormOutput fo = new FormOutput(o);
                    fo.Index = i;
                    fo.PropertyChanged += changeHandler;
                    outputs.Add(fo);
                }
            }
            return outputs;
        }

        /// <summary>
        /// Initializes objects on the Edit From default Inout and Output (Index = 0)
        /// </summary>
        /// <param name="clbInputColumns">CheckedListBox with InputColumns</param>
        /// <param name="lbSelectedItems">ListBox with selected InputColumns</param>
        /// <param name="trvOutputColumns">TreeView with OutputColumns</param>
        /// <param name="outputColumnCreator">Delegate for OutputColumnCreator - Method providing new instance of FormOutputColumn for exiswting IDTSOutputColumns</param>
        /// <param name="propertyChangedHandler">Handler to hook to ProperrtyChanged event of FormInputColumns, FormOutput and FormOutputColum objects</param>
        public virtual void FormInitialize(CheckedListBox clbInputColumns, ListBox lbSelectedItems, TreeView trvOutputColumns, OutputColumnCreator outputColumnCreator,
            PropertyChangedEventHandler o_PropertyChanged)
        {
            FormInitialize(clbInputColumns, lbSelectedItems, trvOutputColumns, outputColumnCreator, o_PropertyChanged, 0, 0);
        }

        /// <summary>
        /// Initializes objects on the Edit From with data from selected Input and Output defined by Index
        /// </summary>
        /// <param name="clbInputColumns">CheckedListBox with InputColumns</param>
        /// <param name="lbSelectedItems">ListBox with selected InputColumns</param>
        /// <param name="trvOutputColumns">TreeView with OutputColumns</param>
        /// <param name="outputColumnCreator">Delegate for OutputColumnCreator - Method providing new instance of FormOutputColumn for exiswting IDTSOutputColumns</param>
        /// <param name="propertyChangedHandler">Handler to hook to ProperrtyChanged event of FormInputColumns, FormOutput and FormOutputColum objects</param>
        /// <param name="inputIndex">Index of the input to used to populate InputColumns</param>
        /// <param name="outputIndex">Index of the output to be used to pouplate OutputColumns</param>
        public virtual void FormInitialize(CheckedListBox clbInputColumns, ListBox lbSelectedItems, TreeView trvOutputColumns, OutputColumnCreator outputColumnCreator,
            PropertyChangedEventHandler o_PropertyChanged, int inputIndex, int outputIndex)
        {
            try
            {
                Loading = true;
                if (clbInputColumns != null)
                    clbInputColumns.SuspendLayout();
                if (lbSelectedItems != null)
                    lbSelectedItems.SuspendLayout();
                if (trvOutputColumns != null)
                    trvOutputColumns.SuspendLayout();

                if (clbInputColumns != null)
                {
                    List<FormInputColumn> inputCols = GetFormInputColumns(inputIndex);

                    inputCols.ForEach(ic => clbInputColumns.Items.Add(ic, ic.IsSelected));

                    if (lbSelectedItems != null)
                    {
                        List<FormInputColumn> selectedColumns = inputCols.FindAll(ic => ic.IsSelected);
                        selectedColumns.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));

                        lbSelectedItems.Items.AddRange(selectedColumns.ToArray());
                    }
                }

                if (trvOutputColumns != null)
                {
                    trvOutputColumns.Nodes.Clear();

                    List<FormOutput> outputs = GetFormOutputs(o_PropertyChanged);

                    foreach (FormOutput o in outputs)
                    {
                        TreeNode outputNode = new TreeNode(o.Name);
                        outputNode.Name = o.Guid.ToString();
                        outputNode.Tag = o;

                        List<FormOutputColumn> cols = GetFormOutputColumns(o.ID, outputColumnCreator, o_PropertyChanged);

                        foreach (FormOutputColumn col in cols)
                        {
                            TreeNode outputColumnNode = new TreeNode(col.Name);
                            outputColumnNode.Name = col.Guid.ToString();

                            outputNode.Nodes.Add(outputColumnNode);
                            outputColumnNode.Tag = col;
                            col.AssociatedTreeNode = outputColumnNode;
                        }

                        trvOutputColumns.Nodes.Add(outputNode);
                    }

                    trvOutputColumns.ExpandAll();

                    if (trvOutputColumns.Nodes.Count > 0)
                    {
                        TreeNode nd = trvOutputColumns.Nodes[0];
                        if (nd.Nodes.Count > 0)
                            trvOutputColumns.SelectedNode = nd.FirstNode;
                        else
                            trvOutputColumns.SelectedNode = nd;
                        trvOutputColumns.SelectedNode.EnsureVisible();
                    }
                }
            }
            finally
            {
                Loading = false;
                if (clbInputColumns != null)
                    clbInputColumns.ResumeLayout();
                if (lbSelectedItems != null)
                    lbSelectedItems.ResumeLayout();
                if (trvOutputColumns != null)
                    trvOutputColumns.ResumeLayout();
            }

        }

        /// <summary>
        /// Handles update of Name of the TreeNode in the TreeView after update of the name of the object implementing INameProvider.
        /// Generally after Name update of FormOutpt or FormOutputCoolumn this method is called by the PropertyChangedEventHandler to reflect the change
        /// in the TreeView on the Form
        /// </summary>
        /// <param name="sender">INameProvider with changedName</param>
        /// <param name="trvOutputColumns">TreeView which TreeNodes name should be updated</param>
        public virtual void UpdateTreeViewNodeName(object sender, TreeView trvOutputColumns)
        {
            if (sender is INameProvider)
            {
                string key = ((INameProvider)sender).Guid.ToString();

                if (key != null)
                {
                    var nodes = trvOutputColumns.Nodes.Find(key, true);
                    if (nodes == null)
                        return;

                    trvOutputColumns.SuspendLayout();
                    foreach (TreeNode node in nodes)
                    {
                        node.Text = ((INameProvider)sender).Name;
                    }
                    trvOutputColumns.ResumeLayout();
                }
            }
        }

        /// <summary>
        /// Selects an InputColumn (to become available in the input buffer)
        /// </summary>
        /// <param name="clbInputColumns">CheckedListbox with InputColumns</param>
        /// <param name="lbSelectedItems">ListBox with Selected InputColumns</param>
        /// <param name="checkedIndex">Index of the Item being checked</param>
        /// <param name="state">CheckState of the item being checkd</param>
        public virtual void SelectInputColumn(CheckedListBox clbInputColumns, ListBox lbSelectedItems, int checkedIndex, CheckState state)
        {
            FormInputColumn icol = clbInputColumns.Items[checkedIndex] as FormInputColumn;

            IDTSInput input = ComponentMetadata.InputCollection[0];

            if (icol != null)
            {
                if (state == CheckState.Checked)
                {
                    DesignTimeComponent.SetUsageType(input.ID, VirtualInput, icol.LineageID, DTSUsageType.UT_READONLY);

                    IDTSInputColumn inputCol = input.InputColumnCollection.GetInputColumnByLineageID(icol.LineageID);

                    icol.DTSInputColumn = inputCol;


                    if (lbSelectedItems != null && !lbSelectedItems.Items.Contains(icol))
                    {
                        int sortOrder = 0;
                        if (lbSelectedItems.Items.Count > 0)
                        {
                            FormInputColumn lastCol = lbSelectedItems.Items[lbSelectedItems.Items.Count - 1] as FormInputColumn;
                            sortOrder = lastCol.SortOrder;
                        }
                        icol.SortOrder = sortOrder + 1;

                        lbSelectedItems.Items.Add(icol);
                    }
                }
                else
                {
                    DesignTimeComponent.SetUsageType(input.ID, VirtualInput, icol.LineageID, DTSUsageType.UT_IGNORED);
                    icol.DTSInputColumn = null;

                    if (lbSelectedItems != null && lbSelectedItems.Items.Contains(icol))
                        lbSelectedItems.Items.Remove(icol);
                }

            }

        }

        /// <summary>
        /// Selexts an InputColumn (to become available in the input buffer)
        /// </summary>
        /// <param name="lineageID">LineageID of the InputColumn</param>
        /// <param name="isSelected">Selection Status of the Column</param>
        public IDTSInputColumn SelectInputColumn(int lineageID, bool selected)
        {
            IDTSInputColumn icol = null;
            if (Input != null)
            {
                DTSUsageType usageType = selected ? DTSUsageType.UT_READONLY : DTSUsageType.UT_IGNORED;

                icol = DesignTimeComponent.SetUsageType(Input.ID, VirtualInput, lineageID, usageType);
            }
            return icol;
        }

        /// <summary>
        /// Moves the selected item in the selected item ListBox
        /// </summary>
        /// <param name="lbSelectedItems">Moves selected InputColumn in provide ListBox by one position Up or Down</param>
        /// <param name="moveUp">If true, movement is Up otherwise Down</param>
        public virtual void MoveSelectedItem(ListBox lbSelectedItems, bool moveUp)
        {
            if (lbSelectedItems == null)
                return;

            int idx = lbSelectedItems.SelectedIndex;
            if (moveUp && idx > 0)
            {
                lbSelectedItems.SuspendLayout();

                FormInputColumn icol = lbSelectedItems.SelectedItem as FormInputColumn;
                FormInputColumn prevCol = lbSelectedItems.Items[idx - 1] as FormInputColumn;
                int ord = icol.SortOrder;
                icol.SortOrder = prevCol.SortOrder;
                prevCol.SortOrder = ord;

                lbSelectedItems.Items.RemoveAt(idx);
                lbSelectedItems.Items.Insert(idx - 1, icol);
                lbSelectedItems.SelectedIndex = idx - 1;


                lbSelectedItems.ResumeLayout(true);
            }
            else if (moveUp == false && idx >= 0 && idx < lbSelectedItems.Items.Count - 1)
            {
                lbSelectedItems.SuspendLayout();
                FormInputColumn icol = lbSelectedItems.SelectedItem as FormInputColumn;
                FormInputColumn nextCol = lbSelectedItems.Items[idx + 1] as FormInputColumn;
                int ord = icol.SortOrder;
                icol.SortOrder = nextCol.SortOrder;
                nextCol.SortOrder = ord;
                lbSelectedItems.Items.RemoveAt(idx);
                lbSelectedItems.Items.Insert(idx + 1, icol);
                lbSelectedItems.SelectedIndex = idx + 1;

                lbSelectedItems.ResumeLayout(true);
            }

        }

        /// <summary>
        /// Adds new output column to TreeView
        /// </summary>
        /// <param name="trvOutputColumns">TreeView to which the OutputColumn should be created</param>
        /// <param name="creator">OutputColumnCreator to be used for new OutputColumn creaton</param>
        /// <param name="propertyChangedHandler">PropertyChange EventHandler to be associated with the newly created FormInputColumn</param>
        public virtual FormOutputColumn AddOutputColumn(TreeView trvOutputColumns, NewOutputColumnCreator creator, PropertyChangedEventHandler o_PropertyChanged)
        {
            TreeNode node = trvOutputColumns.SelectedNode;
            if (node != null)
            {
                if (node.Tag is FormOutputColumn)
                    node = node.Parent;

                FormOutput o = node.Tag as FormOutput;

                FormOutputColumn col = creator(o.DTSOutput);
                col.UIHelper = this;
                col.PropertyChanged += o_PropertyChanged;

                TreeNode cn = new TreeNode(col.Name);
                cn.Name = col.Guid.ToString();
                cn.Tag = col;
                col.AssociatedTreeNode = cn;

                node.Nodes.Add(cn);
                trvOutputColumns.SelectedNode = cn;
                cn.EnsureVisible();
                return col;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Adds new output column to TreeView
        /// </summary>
        /// <param name="trvOutputColumns">TreeView to which the OutputColumn should be created</param>
        /// <param name="creator">OutputColumnCreator to be used for new OutputColumn creaton</param>
        /// <param name="propertyChangedHandler">PropertyChange EventHandler to be associated with the newly created FormInputColumn</param>
        public virtual TFormOutputColumn AddOutputColumnT<TFormOutputColumn>(TreeView trvOutputColumns, NewOutputColumnCreator<TFormOutputColumn> creator, PropertyChangedEventHandler o_PropertyChanged) where TFormOutputColumn:FormOutputColumn
        {
            TreeNode node = trvOutputColumns.SelectedNode;
            if (node != null)
            {
                if (node.Tag is FormOutputColumn)
                    node = node.Parent;

                FormOutput o = node.Tag as FormOutput;

                TFormOutputColumn col = creator(o.DTSOutput);
                col.UIHelper = this;
                col.PropertyChanged += o_PropertyChanged;

                TreeNode cn = new TreeNode(col.Name);
                cn.Name = col.Guid.ToString();
                cn.Tag = col;
                col.AssociatedTreeNode = cn;

                node.Nodes.Add(cn);
                trvOutputColumns.SelectedNode = cn;
                cn.EnsureVisible();
                return col;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Adds new output column to TreeView
        /// </summary>
        /// <param name="trvOutputColumns">TreeView to which the OutputColumn should be created</param>
        /// <param name="creator">UiOutputColumnCreator to be used for new OutputColumn creaton</param>
        /// <param name="propertyChangedHandler">PropertyChange EventHandler to be associated with the newly created FormInputColumn</param>
        public virtual void AddUIOutputColumn(TreeView trvOutputColumns, NewUiOutputColumnCreator creator, IUIHelper uiHelper, PropertyChangedEventHandler o_PropertyChanged)
        {
            TreeNode node = trvOutputColumns.SelectedNode;
            if (node != null)
            {
                if (node.Tag is FormOutputColumn)
                    node = node.Parent;

                FormOutput o = node.Tag as FormOutput;

                FormOutputColumn col = creator(o.DTSOutput, uiHelper);
                col.UIHelper = this;
                col.PropertyChanged += o_PropertyChanged;

                TreeNode cn = new TreeNode(col.Name);
                cn.Name = col.Guid.ToString();
                cn.Tag = col;
                col.AssociatedTreeNode = cn;

                node.Nodes.Add(cn);
                trvOutputColumns.SelectedNode = cn;
                cn.EnsureVisible();
            }
        }

        /// <summary>
        /// Removes selected OutputColumn
        /// </summary>
        /// <param name="trvOutputColumns">TreeView from which the OutputColumn shoudl be removed</param>
        public virtual void RemoveOutputColumn(TreeView trvOutputColumns)
        {
            TreeNode node = trvOutputColumns.SelectedNode;
            if (node != null && node.Tag is FormOutputColumn)
            {
                TreeNode on = node.Parent;

                FormOutput o = on.Tag as FormOutput;
                FormOutputColumn oc = node.Tag as FormOutputColumn;

                o.DTSOutput.OutputColumnCollection.RemoveObjectByID(oc.ID);
                //propOutputColumn.SelectedObject = null;
                on.Nodes.Remove(node);
            }
        }


        public void Delete(System.Windows.Forms.IWin32Window parentWindow)
        {
            
        }

        /// <summary>
        /// Launches the Edit form for the DataFlow component
        /// </summary>
        /// <param name="parentWindow"></param>
        /// <param name="variables"></param>
        /// <param name="connections"></param>
        /// <returns></returns>
        public bool Edit(IWin32Window parentWindow, Variables variables, Connections connections)
        {
            this.connections = connections;
            this.variables = variables;

            TUIForm frm = new TUIForm();
            if (designTimeComponent == null)
                designTimeComponent = componentMetadata.Instantiate();

            InputColumnsUIEditor.UIHelper = this;

            frm.InitializeUIForm(this);

            DialogResult result = frm.ShowDialog(parentWindow);

            return result == DialogResult.OK;
        }

        public void Help(System.Windows.Forms.IWin32Window parentWindow)
        {
            
        }

        /// <summary>
        /// Initializes the DataFlowUI Instance
        /// </summary>
        /// <param name="dtsComponentMetadata">IDTSComponentMetaData of the DataFlow component</param>
        /// <param name="serviceProvider">IService provider tobe asosciated with the DataFlowUI</param>
        public virtual void Initialize(IDTSComponentMetaData dtsComponentMetadata, IServiceProvider serviceProvider)
        {
            this.componentMetadata = dtsComponentMetadata;
            this.serviceProvider = serviceProvider;
            if (this.designTimeComponent == null)
                this.designTimeComponent = this.componentMetadata.Instantiate();

        }

        public void New(System.Windows.Forms.IWin32Window parentWindow)
        {

        }
    }
}
