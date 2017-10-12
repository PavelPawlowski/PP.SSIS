using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Dts.Runtime;
using Microsoft.SqlServer.Dts.Runtime.Design;

namespace PP.SSIS.ControlFlow.Waiting.UI
{
    public interface IUIHelper
    {
        IServiceProvider ServiceProvider { get; }
        IDtsConnectionService ConnectionService { get; }
        TaskHost TaskHost { get; }
        Connections Connections { get; }

    }
}
