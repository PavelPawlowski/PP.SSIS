using System;
using System.Linq;
using System.Windows.Forms;
using Microsoft.SqlServer.Dts.Runtime;
using Microsoft.SqlServer.Dts.Runtime.Design;

namespace PP.SSIS.ControlFlow.Waiting.UI
{

    public class TaskUI<TUIForm> : IDtsTaskUI, IUIHelper where TUIForm : Form, IUIForm, new()
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

        public ContainerControl GetView()
        {
            TUIForm form = new TUIForm();
            form.Initialize(this);
            return form;
        }

        public void Initialize(Microsoft.SqlServer.Dts.Runtime.TaskHost taskHost, IServiceProvider serviceProvider)
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
    }
}
