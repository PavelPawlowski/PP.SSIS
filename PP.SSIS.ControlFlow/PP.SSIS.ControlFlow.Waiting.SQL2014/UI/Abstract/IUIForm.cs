using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Dts.Runtime;

namespace PP.SSIS.ControlFlow.Waiting.UI
{
    public interface IUIForm
    {
        void Initialize(IUIHelper uiHelper);
    }
}
