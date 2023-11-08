using UnityEngine;

namespace Ogxd.ProjectCurator
{
    public static class ProjectIcons
    {
        private static Texture2D _linkBlack;
        public static Texture2D LinkBlack => _linkBlack ??= Resources.Load<Texture2D>("link_black");

        private static Texture2D _linkWhite;
        public static Texture2D LinkWhite => _linkWhite ??= Resources.Load<Texture2D>("link_white");

        private static Texture2D _linkBlue;
        public static Texture2D LinkBlue => _linkBlue ??= Resources.Load<Texture2D>("link_blue");
    }
}