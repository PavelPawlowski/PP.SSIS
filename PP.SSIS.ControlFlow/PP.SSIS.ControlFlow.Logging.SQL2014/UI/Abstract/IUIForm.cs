using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PP.SSIS.ControlFlow.Logging.UI
{
    /// <summary>
    /// Provides methos for initialization of UIForm
    /// </summary>
    public interface IUIForm
    {
        /// <summary>
        /// Initializes the UI Form by providing the UIHelper
        /// </summary>
        /// <param name="uiHelper"></param>
        void Initialize(IUIHelper uiHelper);
    }
}
