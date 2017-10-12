using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.SqlServer.Dts.Runtime;
using Microsoft.SqlServer.Dts.Runtime.Design;

namespace PP.SSIS.ControlFlow.Waiting.UI
{

    public abstract class TaskUIGeneric<TTask, TTaskProperties> : IDtsTaskUI, IUIHelper
        where TTaskProperties : TaskPropertyClass<TTask>, new()
        where TTask : Task 
    {

        private IServiceProvider serviceProvider;
        public IServiceProvider ServiceProvider
        {
            get { return serviceProvider; }
            private set
            {
                serviceProvider = value;
                if (serviceProvider != null)
                {
                    //IDtsConnectionBaseService cs = ServiceProvider.GetService(typeof(IDtsConnectionService)) as IDtsConnectionService;
                    //this.Connections = ConnectionService.GetConnections();
                }
                else
                {
                    Connections = null;
                }

            }
        }


        public IDtsConnectionService ConnectionService { get; private set; }
        public TaskHost TaskHost { get; private set; }
        public Connections Connections { get; private set; }
        public TTask TaskControl
        {
            get
            {
                return TaskHost != null ? TaskHost.InnerObject as TTask : null;
            }
        }


        public ContainerControl GetView()
        {
            TaskPropertiesFrom<TTask, TTaskProperties> form = new TaskPropertiesFrom<TTask, TTaskProperties>();
            form.Initialize(this);
            form.Icon = GetFormIcon();
            return form;
        }

        public virtual void Initialize(Microsoft.SqlServer.Dts.Runtime.TaskHost taskHost, IServiceProvider serviceProvider)
        {
            this.TaskHost = taskHost;
            this.ServiceProvider = serviceProvider;
        }


        public void Delete(IWin32Window parentWindow)
        {
        }

        public void New(IWin32Window parentWindow)
        {
        }

        protected virtual System.Drawing.Icon GetFormIcon()
        {
            Type t = typeof(TTask);
            string resourceName = null;

            var attribs = t.GetCustomAttributes(typeof(DtsTaskAttribute), false);
            if (attribs != null && attribs.Length > 0)
            {
                DtsTaskAttribute attr = attribs[0] as DtsTaskAttribute;
                if (attr != null)
                {
                    resourceName = attr.IconResource;
                }

            }

            System.IO.Stream st;
            System.Reflection.Assembly a = System.Reflection.Assembly.GetExecutingAssembly();

            if (!string.IsNullOrEmpty(resourceName))
            {
                st = a.GetManifestResourceStream(resourceName);
                return new System.Drawing.Icon(st);
            }

            return null;
        }

    }
}
