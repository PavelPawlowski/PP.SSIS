using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Globalization;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace PP.SSIS.DataFlow.UI
{
    /// <summary>
    /// Encapsulates the Input columns selected
    /// </summary>
    public class InputColumns : ICustomTypeDescriptor
    {
        private List<int> inputLineages;
        private string caption;
        List<string> captions;

        public InputColumns()
        {
            inputLineages = new List<int>();
            ValidateLineages();
        }

        public InputColumns(string cols) : this()
        {
            if (string.IsNullOrEmpty(cols))
            {
                inputLineages = new List<int>();
            }
            else
            {
                inputLineages = InputColumns.ParseInputLineages(cols);
            }
            ValidateLineages();
        }
        public InputColumns(InputColumns cth)
        {
            this.inputLineages = new List<int>(cth.inputLineages);
            caption = cth.caption;
            captions = new List<string>(cth.captions);
        }

        public int Count
        {
            get { return inputLineages.Count; }
        }

        /// <summary>
        /// Returns Input Lineages
        /// </summary>
        /// <returns></returns>
        public List<int> GetInputLineages()
        {
            return inputLineages;
        }

        /// <summary>
        /// Returngs string representing input lineages
        /// </summary>
        /// <returns></returns>
        public string GetInputLineagesString()
        {
            return InputColumns.BuildInputLineagesString(inputLineages);
        }

        /// <summary>
        /// Sets the list of input lineages
        /// </summary>
        /// <param name="lineages"></param>
        public void SetInputLineages(IEnumerable<int> lineages)
        {

            inputLineages.Clear();
            foreach (var lineage in lineages)
            {
                inputLineages.Add(lineage);
            }
            ValidateLineages();
        }

        /// <summary>
        /// Set the caption for dsiplaying the column names in the Property Grid editor
        /// </summary>
        private void ValidateLineages()
        {
            caption = "<< No Columns Selected >>";

            captions = new List<string>(inputLineages.Count);
            if (inputLineages.Count > 0)
            {
                List<int> lineagesToRemove = new List<int>(inputLineages.Count);

                if (InputColumnsUIEditor.UIHelper != null)
                {
                    var inputCols = InputColumnsUIEditor.UIHelper.GetFormInputColumns();
                    foreach (int lineage in inputLineages)
                    {
                        FormInputColumn fic = inputCols.Find(ic => ic.LineageID == lineage);
                        if (fic != null)
                            captions.Add(fic.DisplayName);
                        else
                            lineagesToRemove.Add(lineage);
                    }
                }
                else
                    captions.AddRange(inputLineages.ConvertAll<string>(il => il.ToString(CultureInfo.InvariantCulture)));

                if (lineagesToRemove.Count > 0)
                    lineagesToRemove.ForEach(l => inputLineages.Remove(l));


                if (captions.Count > 0)
                    caption = string.Join(" | ", captions.ToArray());
                
            }
        }

        public override string ToString()
        {
            return caption;
        }

        /// <summary>
        /// parses the input lineages list and returns the list of lineages
        /// </summary>
        /// <param name="colsStr">string representing the input lineages list</param>
        /// <returns></returns>
        public static List<int> ParseInputLineages(string colsStr)
        {
            var colLineages = colsStr.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            List<int> inputLineagesList = new List<int>(colLineages.Length);

            foreach (string col in colLineages)
            {
                int lineageID;
                string colLin;

                if (col.StartsWith("#"))
                    colLin = col.Remove(0, 1);
                else
                    colLin = col;

                if (int.TryParse(colLin, out lineageID))
                    inputLineagesList.Add(lineageID);
                else
                    throw new Exception("Invalid format of the HashColumns property. Proper format is comma-delimited list of input column Lineages prefixed with #> Eg. #23,#24");
            }

            return inputLineagesList;
        }

        /// <summary>
        /// Build the string representing input lineages
        /// </summary>
        /// <param name="lineages"></param>
        /// <returns></returns>
        public static string BuildInputLineagesString(IEnumerable<int> lineages)
        {
            List<int> inputLineages = new List<int>();

            foreach (int lineageID in lineages)
            {
                inputLineages.Add(lineageID);
            }

            if (inputLineages.Count > 0)
                return string.Join(",", inputLineages.ConvertAll<string>(l => "#" + l.ToString()).ToArray());
            else
                return string.Empty;

        }

        #region Custom Type Descriptor
        public AttributeCollection GetAttributes()
        {
            return TypeDescriptor.GetAttributes(this, true);
        }

        public string GetClassName()
        {
            return TypeDescriptor.GetClassName(this, true);
        }

        public string GetComponentName()
        {
            return TypeDescriptor.GetComponentName(this, true);
        }

        public TypeConverter GetConverter()
        {
            return TypeDescriptor.GetConverter(this, true);
        }

        public EventDescriptor GetDefaultEvent()
        {
            return TypeDescriptor.GetDefaultEvent(this, true);
        }

        public PropertyDescriptor GetDefaultProperty()
        {
            return TypeDescriptor.GetDefaultProperty(this, true);
        }

        public object GetEditor(Type editorBaseType)
        {
            return TypeDescriptor.GetEditor(this, editorBaseType, true);
        }

        public EventDescriptorCollection GetEvents()
        {
            return TypeDescriptor.GetEvents(this, true);
        }

        public EventDescriptorCollection GetEvents(Attribute[] attributes)
        {
            return TypeDescriptor.GetEvents(this, true);
        }

        public PropertyDescriptorCollection GetProperties()
        {
            // Create a new collection object PropertyDescriptorCollection
            PropertyDescriptorCollection pds = new PropertyDescriptorCollection(null);


            for (int i = 0; i < captions.Count; i++)
            {
                InputColumnsPropertyDescriptor pd = new InputColumnsPropertyDescriptor(captions[i], i, inputLineages[i]);

                pds.Add(pd);
            }
            return pds;
        }

        public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            return GetProperties();
        }

        public object GetPropertyOwner(PropertyDescriptor pd)
        {
            return this;
        }

        #endregion

        #region Custom Property Descriptor
        public class InputColumnsPropertyDescriptor : PropertyDescriptor
        {
            string caption;
            int idx;
            int lineageID;
            string name;

            public InputColumnsPropertyDescriptor(string caption, int idx, int lineageID) : base(caption, null)
            {
                this.caption = caption;
                this.idx = idx;
                this.lineageID = lineageID;
                this.name = string.Format("[{0}] ({1})", idx, lineageID);
            }

            public override string DisplayName
            {
                get
                {
                    return name;
                }
            }


            public override string Name
            {
                get
                {
                    return name;
                }
            }

            public override string Description
            {
                get
                {
                    return caption;
                }
            }

            public override Type ComponentType
            {
                get
                {
                    return typeof(string);
                }
            }

            public override bool IsReadOnly
            {
                get
                {
                    return true;
                }
            }

            public override Type PropertyType
            {
                get
                {
                    return typeof(string);
                }
            }

            public override bool CanResetValue(object component)
            {
                return false;
            }

            public override object GetValue(object component)
            {
                return caption;
            }

            public override void ResetValue(object component)
            {

            }

            public override void SetValue(object component, object value)
            {

            }

            public override bool ShouldSerializeValue(object component)
            {
                return false;
            }
        }

        #endregion
    }

    /// <summary>
    /// UIEditor for InputColumns
    /// </summary>
    public class InputColumnsUIEditor : UITypeEditor
    {
        public static IUIHelper UIHelper { get; set; }
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.Modal;
        }

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            IWindowsFormsEditorService svc = provider.GetService(typeof(IWindowsFormsEditorService)) as IWindowsFormsEditorService;

            InputColumns cth = value as InputColumns;
            if (cth != null)
            {
                using (InputColumnsUIEditorForm hcue = new UI.InputColumnsUIEditorForm())
                {
                    if (UIHelper != null)
                        hcue.InitializeForm(UIHelper, cth.GetInputLineages());

                    if (hcue.ShowDialog() == DialogResult.OK)
                    {
                        cth.SetInputLineages(hcue.GetInputColumnsLineages());
                    }

                }
            }
            return new InputColumns(cth);
        }
    }
}
