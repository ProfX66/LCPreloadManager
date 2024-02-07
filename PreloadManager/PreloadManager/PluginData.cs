using System;
using System.Collections.Generic;
using System.Reflection;

namespace BepInEx.PreloadManager
{
    public class PluginData
    {
        public string name;
        public Version version;
        public Type type;
        public List<MethodInfo> methods;
    }
}
