using OuiGui.Lib.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OuiGui.WPF.Util
{
    public class InstallAction
    {
        public InstallAction(PackageVersion package, InstallActionType action)
        {
            this.Package = package;
            this.Action = action;
        }

        public PackageVersion Package { get; private set; }

        public InstallActionType Action { get; private set; }
    }
}
