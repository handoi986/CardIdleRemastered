using System;
using System.Linq;

namespace CardIdleRemastered
{
    public class ReleaseInfo
    {
        public VersionInfo CurrentVersion { get; set; }

        private int[] _version;
        public int[] Version
        {
            get
            {
                if (_version == null)
                {
                    if (CurrentVersion != null)
                    {
                        _version = CurrentVersion.Number.Split('.').Take(4).Select(int.Parse).ToArray();
                    }
                    else
                    {
                        _version = new int[4] { 1, 0, 0, 0 };
                    }
                }

                return _version;
            }
        }

        /// <summary>
        /// Compares release version with app version
        /// </summary>
        public bool IsOlderThan(Version version)
        {
            var num = new[] { version.Major, version.Minor, version.Build, version.Revision };
            int delta = Enumerable.Range(0, num.Length)
                .Select(i => num[i] - Version[i])
                .SkipWhile(d => d == 0)
                .FirstOrDefault();
            return delta < 0;
        }
    }

    public class VersionInfo
    {
        public string Number { get; set; }
    }
}
