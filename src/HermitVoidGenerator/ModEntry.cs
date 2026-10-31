using UnityEngine;

namespace HermitVoidGenerator
{
    /// <summary>Hermit's Void Generator entry class. ModLoader takes the first IMod implementation and constructs it parameterlessly.</summary>
    public sealed class PawnshopMod : IMod
    {
        internal const string ModId = "yg.hermit_void_generator";

        private static GameObject host;

        public void Init(ModManifest manifest)
        {
            Log.Info("Init 完成 — v" + (manifest != null ? manifest.ModVersion : "?"));
        }

        public void OnEnable()
        {
            if (host != null)
            {
                return;
            }

            host = new GameObject("HermitVoidGeneratorHost");
            Object.DontDestroyOnLoad(host);
            host.AddComponent<PawnshopDriver>();
            Log.Info("已启用：按 " + PawnshopDriver.HotkeyLabel + " 打开「Hermit的虚空生成器」");
        }

        public void OnDisable()
        {
            PawnshopWindow.Destroy();
            if (host != null)
            {
                Object.Destroy(host);
                host = null;
            }
        }
    }
}
