using System;
using System.Collections.Generic;
using System.Text;

using System.Diagnostics;

/* Todo:
 * add constands for states
 * 
 * */

namespace the3DSLoadForWin
{
    class Settings
    {
        public static bool initSettings()
        {
            String[] keys = { "port", "ip", "mode", "target", "folderToken" };
            String[] DefaultValues = { "80", "127.0.0.1", "Gateway", "", "" };
	        int SettingsLength = keys.Length;
	        bool success = true;
	        Windows.Foundation.Collections.IPropertySet sValues = Windows.Storage.ApplicationData.Current.RoamingSettings.Values;
	        for (int i = 0; i < SettingsLength; i++)
	        {
		        String key = keys[i];
		        if (!sValues.ContainsKey(key))
		        {
			        String dValue = DefaultValues[i];
                    sValues.Add(new KeyValuePair<string, object>(key, dValue));
                    Debug.WriteLine("Created settings");
		        }
	        }
	        return success;
        }

        public static String getValue(String key)
        {
	        String Value = null;
	        Windows.Foundation.Collections.IPropertySet sValues = Windows.Storage.ApplicationData.Current.RoamingSettings.Values;
	        if (sValues.ContainsKey(key))
		        Value = (String)sValues[key].ToString();
	        return Value;
        }

        public static int setValue(String key, String Value)
        {
	        int state = 1;
	        Windows.Foundation.Collections.IPropertySet sValues = Windows.Storage.ApplicationData.Current.RoamingSettings.Values;
	        if (sValues.ContainsKey(key))
	        {
		        Windows.Storage.ApplicationData.Current.RoamingSettings.Values.Remove(key);
		        Windows.Storage.ApplicationData.Current.RoamingSettings.Values.Add(key, Value);
		        state = 0;
	        }
	        return state;
        }

        public static bool reset()
        {
	        Windows.Storage.ApplicationData.Current.RoamingSettings.Values.Clear();
	        return initSettings();
        }
    }
}
