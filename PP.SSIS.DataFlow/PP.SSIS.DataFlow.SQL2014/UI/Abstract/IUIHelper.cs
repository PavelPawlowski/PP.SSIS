// <copyright file="UIHelper.cs" company="Pavel Pawlowski">
// Copyright (c) 2014, 2015 All Right Reserved
//
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY 
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// </copyright>
// <author>Pavel Pawlowski</author>
// <summary>Contains definition of the IUIHelperInterface/summary>
using System;
using System.Collections.Generic;
using Microsoft.SqlServer.Dts.Runtime;
using System.ComponentModel;
using System.Windows.Forms;


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
    /// Interface defining Helper methods for the UI Form
    /// </summary>
    public interface IUIHelper
    {
        /// <summary>
        /// Gets Component metadata for associated Transformation
        /// </summary>
        IDTSComponentMetaData ComponentMetadata { get; }
        /// <summary>
        /// Provides connections of the associated Transformation
        /// </summary>
        Connections Connections { get; }
        /// <summary>
        /// Gets the DesignTime componend of associated Transformation
        /// </summary>
        IDTSDesigntimeComponent DesignTimeComponent { get; }
        /// <summary>
        /// Gets the primary Input of associated Transformation
        /// </summary>
        IDTSInput Input { get; }
        /// <summary>
        /// Gets Variables of associated Transformation
        /// </summary>
        Variables Variables { get; }
        /// <summary>
        /// Gets the primary virtual input for associated Transformation
        /// </summary>
        IDTSVirtualInput VirtualInput { get; }
        /// <summary>
        /// Gets the Second Input of associated transformation
        /// </summary>
        IDTSInput Input2 { get; }
        /// <summary>
        /// Gets the second Virtual input for associated transformation
        /// </summary>
        IDTSVirtualInput VirtualInput2 { get; }
        /// <summary>
        /// Gets Default output for associated transformation
        /// </summary>
        IDTSOutput Output { get; }
        /// <summary>
        /// Gets the output marked as Error Output
        /// </summary>
        IDTSOutput ErrorOutput { get; }
        /// <summary>
        /// Gets Input at particular Index of the Inputs Collection
        /// </summary>
        /// <param name="index">Index of the Input to be retrieved</param>
        /// <returns>IDTSInput</returns>
        IDTSInput GetInput(int index);
        /// <summary>
        /// Gets IDTSVirtualInpu associated with IDTSInput at particular index in the Input Collection
        /// </summary>
        /// <param name="index">Index of the Input to retrieve VirtualInput</param>
        /// <returns>IDTSVirtualInput</returns>
        IDTSVirtualInput GetVirtualInput(int index);

        bool Loading { get; }

        /// <summary>
        /// Gets List of primary input (Index = 0) columns encapsulated in the FormInputColumn class
        /// </summary>
        /// <returns>List of FormInputColumns</returns>
        List<FormInputColumn> GetFormInputColumns();

        /// <summary>
        /// Gets List of IDTSInputColumns from Input at particular Index int he Input collection encapsulated in the FormInputColumn class
        /// </summary>
        /// <param name="index">Index of the Input to retrieve InputColumns</param>
        /// <returns>List of FormInputColumns</returns>
        List<FormInputColumn> GetFormInputColumns(int index);

        /// <summary>
        /// Gets Output columns encapsulated in FormOutputColumn clas
        /// </summary>
        /// <param name="outputID">ID of the output to get the columns</param>
        /// <param name="creator">Delegate for OutputColumnCreator - Method providing new instances of FormOutputColumn for existing IDTSOutputColumns</param>
        /// <param name="propertyChangedHandler">Handler to hook to PropertyChanged event of FormOutputColumn</param>
        /// <returns>List of FormOutputColumns</returns>
        List<FormOutputColumn> GetFormOutputColumns(int outputID, OutputColumnCreator creator, PropertyChangedEventHandler propertyChangedHandler);

        /// <summary>
        /// Gets list of Outputs encapsulated int he FormOutput class
        /// </summary>
        /// <param name="changeHandler">Handler to hook to PropertyChanged event of FormOutput</param>
        /// <returns>List of FormOutputs</returns>
        List<FormOutput> GetFormOutputs(PropertyChangedEventHandler changeHandler);

        /// <summary>
        /// Initializes objects on the Edit From default Inout and Output (Index = 0)
        /// </summary>
        /// <param name="clbInputColumns">CheckedListBox with InputColumns</param>
        /// <param name="lbSelectedItems">ListBox with selected InputColumns</param>
        /// <param name="trvOutputColumns">TreeView with OutputColumns</param>
        /// <param name="outputColumnCreator">Delegate for OutputColumnCreator - Method providing new instance of FormOutputColumn for exiswting IDTSOutputColumns</param>
        /// <param name="propertyChangedHandler">Handler to hook to ProperrtyChanged event of FormInputColumns, FormOutput and FormOutputColum objects</param>
        void FormInitialize(CheckedListBox clbInputColumns, ListBox lbSelectedItems, TreeView trvOutputColumns, OutputColumnCreator outputColumnCreator,
            PropertyChangedEventHandler propertyChangedHandler);

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
        void FormInitialize(CheckedListBox clbInputColumns, ListBox lbSelectedItems, TreeView trvOutputColumns, OutputColumnCreator outputColumnCreator,
            PropertyChangedEventHandler propertyChangedHandler, int inputIndex, int outputIndex);

        /// <summary>
        /// Handles update of Name of the TreeNode in the TreeView after update of the name of the object implementing INameProvider.
        /// Generally after Name update of FormOutpt or FormOutputCoolumn this method is called by the PropertyChangedEventHandler to reflect the change
        /// in the TreeView on the Form
        /// </summary>
        /// <param name="sender">INameProvider with changedName</param>
        /// <param name="trvOutputColumns">TreeView which TreeNodes name should be updated</param>
        void UpdateTreeViewNodeName(object sender, TreeView trvOutputColumns);

        /// <summary>
        /// Selects an InputColumn (to become available in the input buffer)
        /// </summary>
        /// <param name="clbInputColumns">CheckedListbox with InputColumns</param>
        /// <param name="lbSelectedItems">ListBox with Selected InputColumns</param>
        /// <param name="checkedIndex">Index of the Item being checked</param>
        /// <param name="state">CheckState of the item being checkd</param>
        void SelectInputColumn(CheckedListBox clbInputColumns, ListBox lbSelectedItems, int checkedIndex, CheckState state);

        /// <summary>
        /// Selexts an InputColumn (to become available in the input buffer)
        /// </summary>
        /// <param name="lineageID">LineageID of the InputColumn</param>
        /// <param name="isSelected">Selection Status of the Column</param>
        IDTSInputColumn SelectInputColumn(int lineageID, bool isSelected);

        /// <summary>
        /// Moves the selected item in the selected item ListBox
        /// </summary>
        /// <param name="lbSelectedItems">Moves selected InputColumn in provide ListBox by one position Up or Down</param>
        /// <param name="moveUp">If true, movement is Up otherwise Down</param>
        void MoveSelectedItem(ListBox lbSelectedItems, bool moveUp);

        /// <summary>
        /// Adds new output column to TreeView
        /// </summary>
        /// <param name="trvOutputColumns">TreeView to which the OutputColumn should be created</param>
        /// <param name="creator">OutputColumnCreator to be used for new OutputColumn creaton</param>
        /// <param name="propertyChangedHandler">PropertyChange EventHandler to be associated with the newly created FormInputColumn</param>
        FormOutputColumn AddOutputColumn(TreeView trvOutputColumns, NewOutputColumnCreator creator, PropertyChangedEventHandler propertyChangedHandler);

        /// <summary>
        /// Adds new output column to TreeView
        /// </summary>
        /// <param name="trvOutputColumns">TreeView to which the OutputColumn should be created</param>
        /// <param name="creator">OutputColumnCreator to be used for new OutputColumn creaton</param>
        /// <param name="propertyChangedHandler">PropertyChange EventHandler to be associated with the newly created FormInputColumn</param>
        TFormOutputColumn AddOutputColumnT<TFormOutputColumn>(TreeView trvOutputColumns, NewOutputColumnCreator<TFormOutputColumn> creator, PropertyChangedEventHandler propertyChangedHandler) where TFormOutputColumn : FormOutputColumn;
        /// <summary>
        /// CheckColumnFoInputvalidity
        /// </summary>
        /// <param name="vInput">VirtualInput</param>
        /// <param name="input">Input</param>
        /// <param name="icol">FormInputColumn</param>
        /// <returns></returns>
        bool CheckColumnForInputValidity(IDTSVirtualInput vInput, IDTSInput input, FormInputColumn icol);

        bool CheckColumnForInputValidity(IDTSVirtualInputColumn vInputColumn);

        /// <summary>
        /// Removes selected OutputColumn
        /// </summary>
        /// <param name="trvOutputColumns">TreeView from which the OutputColumn shoudl be removed</param>
        void RemoveOutputColumn(TreeView trvOutputColumns);
    }   
}
