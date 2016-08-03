using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Research.Science.FetchClimate2.Properties
{
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase
    {
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.SettingsSerializeAs(global::System.Configuration.SettingsSerializeAs.Binary)]
        public global::System.Collections.Specialized.StringDictionary Accounts
        {
            get
            {
                return ((global::System.Collections.Specialized.StringDictionary)(this["Accounts"]));
            }
            set
            {
                this["Accounts"] = value;
            }
        }
    }
}
