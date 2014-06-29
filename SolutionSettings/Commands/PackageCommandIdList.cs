using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Enexure.SolutionSettings.Commands
{
    /// <summary> 
    /// This class is used to expose the list of the IDs of the commands implemented 
    /// by this package. This list of IDs must match the set of IDs defined inside the 
    /// Buttons section of the VSCT file. 
    /// </summary> 
    internal static class PkgCmdIDList
    {
        // Now define the list a set of public static members. 
        public const int cmdIdAddSolutionSettings = 0x1001;
        public const int cmdIdOpenGlobalSolutionSettings = 0x1002;
    } 
}
