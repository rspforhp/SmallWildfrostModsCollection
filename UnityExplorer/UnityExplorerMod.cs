using System;
using Deadpan.Enums.Engine.Components.Modding;

namespace UnityExplorer
{
    public class UnityExplorerMod : WildfrostMod
    {
        public UnityExplorerMod(string modDirectory) : base(modDirectory)
        {
        }

        protected override void Load()
        {
            base.Load();
            Instance=UnityExplorer.ExplorerStandalone.CreateInstance();
        }

        protected override void Unload()
        {
            base.Unload();
        }

        public ExplorerStandalone Instance;
        

        public override string GUID => "kopie.wildfrost.unityexplorer";
        public override string[] Depends => Array.Empty<string>();
        public override string Title => "Unity explorer";
        public override string Description => "Port of unity explorer for wildfrost.";
    }
}