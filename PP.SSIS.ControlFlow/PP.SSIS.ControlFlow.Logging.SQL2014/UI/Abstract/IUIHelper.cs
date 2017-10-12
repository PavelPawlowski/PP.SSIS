using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Dts.Runtime;
using Microsoft.SqlServer.Dts.Runtime.Design;

namespace PP.SSIS.ControlFlow.Logging.UI
{
    /// <summary>
    /// UIHelper interface which provieds access to teh Task services
    /// </summary>
    public interface IUIHelper
    {
        IServiceProvider ServiceProvider { get; }
        IDtsConnectionService ConnectionService { get; }
        TaskHost TaskHost { get; }
        Connections Connections { get; }
    }
}
