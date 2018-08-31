using System;
using System.IO;

namespace Common
{
    public static class GameDetector
    {
        public enum Game
        {
            MostWanted,
            Unknown
        }

        /// <summary>
        /// Attempt to determine the NFS game installed in the given directory.
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        public static Game DetectGame(string directory)
        {
            var tracksPath = Path.Combine(directory, "TRACKS");
            if (!Directory.Exists(tracksPath))
            {
                throw new ArgumentException("TRACKS folder does not exist! Cannot determine game.");
            }

            if (File.Exists(Path.Combine(directory, "speed.exe")))
            {
                if (File.Exists(Path.Combine(tracksPath, "L2RA.BUN"))
                    && File.Exists(Path.Combine(tracksPath, "STREAML2RA.BUN")))
                {
                    return Game.MostWanted;
                }
            }

            return Game.Unknown;
        }
    }
}
